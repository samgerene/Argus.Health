// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointRepository.cs"  >
// 
//     Copyright (c) 2025-2026 Sam Gerené
// 
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
// 
//         http://www.apache.org/licenses/LICENSE-2.0
// 
//     Unless required by applicable law or agreed to in writing, softwareUseCases
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.
// 
//   </copyright>
//   ------------------------------------------------------------------------------------------------

namespace Argus.Health.Service.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Collections.Immutable;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;

    using FluentResults;

    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The purpose of the <see cref="IHealthEndPointRepository"/> is to perform
    /// CRUD on the <see cref="HealthEndPoint"/> repository
    /// </summary>
    public class HealthEndPointRepository : IHealthEndPointRepository
    {
        /// <summary>
        /// The name of the database file
        /// </summary>
        public static string DatabaseFileName = "ArgusHealth.sqlite";

        /// <summary>
        /// The (injected) logger
        /// </summary>
        private readonly ILogger<HealthEndPointRepository> logger;

        /// <summary>
        /// The connection string used to connect to the SQLite dbase
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// The path to the database folder on disk
        /// </summary>
        private readonly string databaseFolderPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndPointRepository"/>
        /// </summary>
        /// <param name="logger">
        /// The (injected) logger
        /// </param>
        public HealthEndPointRepository(ILogger<HealthEndPointRepository> logger)
        {
            this.logger = logger;

            this.databaseFolderPath = Program.ApplicationDataFolder;

            var dbPath = Path.Combine(databaseFolderPath, DatabaseFileName);
            this.connectionString = $"Data Source={dbPath};";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndPointRepository"/>
        /// </summary>
        /// <param name="logger">
        /// The (injected) logger
        /// </param>
        /// <param name="databaseFolderPath">
        /// path to the folder where the Argus Health database is stored
        /// </param>
        internal HealthEndPointRepository(ILogger<HealthEndPointRepository> logger, string databaseFolderPath)
        {
            this.logger = logger;

            this.databaseFolderPath = databaseFolderPath;

            var dbPath = Path.Combine(databaseFolderPath, DatabaseFileName);
            this.connectionString = $"Data Source={dbPath};";
        }

        /// <summary>
        /// The event that is raised when a <see cref="HealthEndPoint"/>
        /// has been added
        /// </summary>
        public event EventHandler<HealthEndPoint>? EndpointAdded;

        /// <summary>
        /// The event that is raised when a <see cref="HealthEndPoint"/>
        /// has been updated
        /// </summary>
        public event EventHandler<HealthEndPoint>? EndpointUpdated;

        /// <summary>
        /// The event that is raised when a <see cref="HealthEndPoint"/>
        /// has been deleted
        /// </summary>
        public event EventHandler<HealthEndPoint>? EndpointRemoved;

        /// <summary>
        /// Initializes a new database file if it does not yet exist
        /// </summary>
        public void InitializeDatabase(bool force = false)
        {
            Directory.CreateDirectory(this.databaseFolderPath);

            this.InitializeDatabaseFile(force);

            this.logger.LogInformation("Database initialized successfully at {DatabaseFolder}", this.databaseFolderPath);
        }

        /// <summary>
        /// Asynchronously reads all the <see cref="HealthEndPoint"/> instances
        /// from the database
        /// </summary>
        /// <returns>
        /// a <see cref="ImmutableList{HealthEndPoint}"/>
        /// </returns>
        public async Task<ImmutableList<HealthEndPoint>> ReadAsync(Guid[]? identifiers = null)
        {
            this.logger.LogDebug("Reading HealthEndPoints with {FilterCount} identifier filter(s)", identifiers?.Length ?? 0);

            var list = new List<HealthEndPoint>();

            await using var connection = new SqliteConnection(this.connectionString);

            try
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();

                if (identifiers != null && identifiers.Any())
                {
                    var paramNames = identifiers
                        .Select((id, index) => $"@id{index}")
                        .ToArray();

                    command.CommandText = $"""
                                               SELECT
                                                   Identifier,
                                                   Name,
                                                   Urls,
                                                   Frequency,
                                                   Timeout,
                                                   RetryCount,
                                                   IsActive
                                               FROM HealthEndpoints
                                               WHERE Identifier IN ({string.Join(", ", paramNames)});
                                           """;

                    for (int i = 0; i < paramNames.Length; i++)
                    {
                        command.Parameters.AddWithValue(paramNames[i], identifiers.ElementAt(i).ToString());
                    }
                }
                else
                {
                    command.CommandText = """
                                              SELECT
                                                  Identifier,
                                                  Name,
                                                  Urls,
                                                  Frequency,
                                                  Timeout,
                                                  RetryCount,
                                                  IsActive
                                              FROM HealthEndpoints;
                                          """;
                }

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var healthEndPoint = new HealthEndPoint
                    {
                        Identifier = Guid.Parse(reader.GetString(0)),
                        Name = reader.GetString(1),
                        Url = reader.GetString(2),
                        Frequency = reader.GetInt32(3),
                        Timeout = reader.GetInt32(4),
                        RetryCount = reader.GetInt32(5),
                        IsActive = reader.GetBoolean(6)
                    };

                    list.Add(healthEndPoint);
                }

                this.logger.LogDebug("Successfully read {Count} HealthEndPoint(s) from the database", list.Count);

                return list.ToImmutableList();

            }
            catch (Exception ex)
            {
                var message = "The HealthEndPoint instances could not be READ from the SQLite database";

                this.logger.LogError(ex, message);

                throw new DataException(message, ex);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously creates a <see cref="HealthEndPoint"/> in the database
        /// </summary>
        /// <param name="healthEndPoint">
        /// The <see cref="HealthEndPoint"/> that is to be added
        /// </param>
        /// <returns>
        /// an awaitable <see cref="Task"/>
        /// </returns>
        public async Task<Result> CreateAsync(HealthEndPoint healthEndPoint)
        {
            ArgumentNullException.ThrowIfNull(healthEndPoint);

            this.logger.LogDebug("Creating HealthEndPoint with name {Name}", healthEndPoint.Name);

            if (healthEndPoint.Identifier == Guid.Empty)
            {
                healthEndPoint.Identifier = Guid.NewGuid();
            }

            await using var connection = new SqliteConnection(this.connectionString);

            try
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = """
                                      INSERT INTO HealthEndpoints (Identifier, Name, Urls, Frequency, Timeout, RetryCount, IsActive)
                                      VALUES ($identifier, $name, $urls, $frequency, $timeout, $retryCount, $isActive);
                                      """;

                command.Parameters.AddWithValue("$identifier", healthEndPoint.Identifier.ToString());
                command.Parameters.AddWithValue("$name", healthEndPoint.Name);
                command.Parameters.AddWithValue("$urls", string.Join(';', healthEndPoint.Url));
                command.Parameters.AddWithValue("$frequency", healthEndPoint.Frequency);
                command.Parameters.AddWithValue("$timeout", healthEndPoint.Timeout);
                command.Parameters.AddWithValue("$retryCount", healthEndPoint.RetryCount);
                command.Parameters.AddWithValue("$isActive", healthEndPoint.IsActive ? 1 : 0);

                var affected = await command.ExecuteNonQueryAsync();

                if (affected == 1)
                {
                    this.logger.LogInformation("HealthEndPoint {Name} created with identifier {Identifier}", healthEndPoint.Name, healthEndPoint.Identifier);
                    this.EndpointAdded?.Invoke(this, healthEndPoint);
                }
                else
                {
                    this.logger.LogWarning("The HealthEndPoint with identifier {identifier} was not CREATED", healthEndPoint.Identifier);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var message = "The HealthEndPoint instances could not be CREATED in the SQLite database";

                this.logger.LogError(ex, message);

                return Result.Fail(ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously updates a <see cref="HealthEndPoint"/> in the database
        /// </summary>
        /// <param name="healthEndPoint">
        /// The <see cref="HealthEndPoint"/> that is to be updated
        /// </param>
        /// <returns>
        /// an awaitable <see cref="Task"/>
        /// </returns>
        public async Task<Result> UpdateAsync(HealthEndPoint healthEndPoint)
        {
            ArgumentNullException.ThrowIfNull(healthEndPoint);

            this.logger.LogDebug("Updating HealthEndPoint with identifier {Identifier}", healthEndPoint.Identifier);

            await using var connection = new SqliteConnection(this.connectionString);

            try
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = """
                    UPDATE HealthEndpoints
                    SET Name = $name, Urls = $urls, Frequency = $frequency, Timeout = $timeout, RetryCount = $retryCount, IsActive = $isActive
                    WHERE Identifier = $identifier;
                """;

                command.Parameters.AddWithValue("$identifier", healthEndPoint.Identifier.ToString());
                command.Parameters.AddWithValue("$name", healthEndPoint.Name);
                command.Parameters.AddWithValue("$urls", string.Join(';', healthEndPoint.Url));
                command.Parameters.AddWithValue("$frequency", healthEndPoint.Frequency);
                command.Parameters.AddWithValue("$timeout", healthEndPoint.Timeout);
                command.Parameters.AddWithValue("$retryCount", healthEndPoint.RetryCount);
                command.Parameters.AddWithValue("$isActive", healthEndPoint.IsActive ? 1 : 0);

                var affected = await command.ExecuteNonQueryAsync();

                if (affected == 1)
                {
                    this.logger.LogInformation("HealthEndPoint with identifier {Identifier} updated successfully", healthEndPoint.Identifier);
                    EndpointUpdated?.Invoke(this, healthEndPoint);
                }
                else
                {
                    this.logger.LogWarning("The HealthEndPoint with identifier {identifier} was not UPDATED, it probably does not exist in the database", healthEndPoint.Identifier);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var message = "The HealthEndPoint instances could not be UPDATED in the SQLite database";

                this.logger.LogError(ex, message);

                return Result.Fail(ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously deletes a <see cref="HealthEndPoint"/> from the database
        /// </summary>
        /// <param name="healthEndPoint">
        /// The <see cref="HealthEndPoint"/> that is to be deleted
        /// </param>
        /// <returns>
        /// an awaitable <see cref="Task"/>
        /// </returns>
        public async Task<Result> DeleteAsync(HealthEndPoint healthEndPoint)
        {
            ArgumentNullException.ThrowIfNull(healthEndPoint);

            this.logger.LogDebug("Deleting HealthEndPoint with identifier {Identifier}", healthEndPoint.Identifier);

            await using var connection = new SqliteConnection(this.connectionString);

            try
            {
                var d = healthEndPoint.Identifier.ToString().ToLower(CultureInfo.InvariantCulture);

                await connection.OpenAsync();

                var deleteResultsCommand = connection.CreateCommand();
                deleteResultsCommand.CommandText = "DELETE FROM HealthEndPointCheckResults WHERE HealthEndPoint = $identifier;";
                deleteResultsCommand.Parameters.AddWithValue("$identifier", d);
                await deleteResultsCommand.ExecuteNonQueryAsync();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM HealthEndpoints WHERE Identifier = $identifier;";
                command.Parameters.AddWithValue("$identifier", d);

                var affected = await command.ExecuteNonQueryAsync();

                if (affected == 1)
                {
                    this.logger.LogInformation("HealthEndPoint with identifier {Identifier} deleted successfully", healthEndPoint.Identifier);
                    EndpointRemoved?.Invoke(this, healthEndPoint);
                }
                else
                {
                    this.logger.LogWarning("The HealthEndPoint with identifier {identifier} was not DELETED, it probably does not exist in the database", healthEndPoint.Identifier);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var message = "The HealthEndPoint instances could not be DELETED from the SQLite database";

                this.logger.LogError(ex, message);

                return Result.Fail(ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Initializes the database with the required tables
        /// </summary>
        private void InitializeDatabaseFile(bool force)
        {
            this.logger.LogDebug("Initializing database tables, force: {Force}", force);

            using var connection = new SqliteConnection(this.connectionString);

            try
            {
                connection.Open();

                if (force)
                {
                    using var dropCheckResultsCommand = connection.CreateCommand();
                    dropCheckResultsCommand.CommandText = "DROP TABLE IF EXISTS HealthEndPointCheckResults;";
                    dropCheckResultsCommand.ExecuteNonQuery();

                    using var dropCommand = connection.CreateCommand();
                    dropCommand.CommandText = "DROP TABLE IF EXISTS HealthEndpoints;";
                    dropCommand.ExecuteNonQuery();
                }

                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS HealthEndpoints (
                        Identifier TEXT PRIMARY KEY,
                        Name TEXT NOT NULL UNIQUE,
                        Urls TEXT NOT NULL,
                        Frequency INTEGER NOT NULL,
                        Timeout INTEGER NOT NULL,
                        RetryCount INTEGER NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                ";
                createCommand.ExecuteNonQuery();

                using var createCheckResultsCommand = connection.CreateCommand();
                createCheckResultsCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS HealthEndPointCheckResults (
                        Identifier TEXT PRIMARY KEY,
                        Timestamp TEXT NOT NULL,
                        StatusCode INTEGER NOT NULL,
                        ErrorMessage TEXT,
                        ResponseTimeMs INTEGER NOT NULL DEFAULT 0,
                        HealthEndPoint TEXT NOT NULL,
                        FOREIGN KEY (HealthEndPoint) REFERENCES HealthEndpoints(Identifier)
                    );
                ";
                createCheckResultsCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var message = "The HealthEndPoint SQLite database could not be initialized";

                this.logger.LogError(ex, message);

                throw new DataException(message, ex);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }
}
