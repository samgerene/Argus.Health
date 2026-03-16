// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointCheckResultRepository.cs">
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
    using System.Collections.Immutable;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;

    using FluentResults;

    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The purpose of the <see cref="HealthEndPointCheckResultRepository"/> is to perform
    /// Create and Read operations on <see cref="HealthEndPointCheckResult"/> records
    /// </summary>
    public class HealthEndPointCheckResultRepository : IHealthEndPointCheckResultRepository
    {
        /// <summary>
        /// The (injected) logger
        /// </summary>
        private readonly ILogger<HealthEndPointCheckResultRepository> logger;

        /// <summary>
        /// The connection string used to connect to the SQLite database
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndPointCheckResultRepository"/> class
        /// </summary>
        /// <param name="logger">
        /// The (injected) logger
        /// </param>
        public HealthEndPointCheckResultRepository(ILogger<HealthEndPointCheckResultRepository> logger)
        {
            this.logger = logger;

            var databaseFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArgusHealth");

            var dbPath = Path.Combine(databaseFolderPath, HealthEndPointRepository.DatabaseFileName);
            this.connectionString = $"Data Source={dbPath};";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndPointCheckResultRepository"/> class
        /// </summary>
        /// <param name="logger">
        /// The (injected) logger
        /// </param>
        /// <param name="databaseFolderPath">
        /// Path to the folder where the Argus Health database is stored
        /// </param>
        internal HealthEndPointCheckResultRepository(ILogger<HealthEndPointCheckResultRepository> logger, string databaseFolderPath)
        {
            this.logger = logger;

            var dbPath = Path.Combine(databaseFolderPath, HealthEndPointRepository.DatabaseFileName);
            this.connectionString = $"Data Source={dbPath};";
        }

        /// <summary>
        /// Asynchronously creates a <see cref="HealthEndPointCheckResult"/> in the database
        /// </summary>
        /// <param name="checkResult">
        /// The <see cref="HealthEndPointCheckResult"/> that is to be added
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure
        /// </returns>
        public async Task<Result> CreateAsync(HealthEndPointCheckResult checkResult)
        {
            if (checkResult == null)
            {
                throw new ArgumentNullException(nameof(checkResult));
            }

            if (checkResult.Identifier == Guid.Empty)
            {
                checkResult.Identifier = Guid.NewGuid();
            }

            await using var connection = new SqliteConnection(this.connectionString);

            try
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = """
                                      INSERT INTO HealthEndPointCheckResults (Identifier, Timestamp, StatusCode, ErrorMessage, HealthEndPoint)
                                      VALUES ($identifier, $timestamp, $statusCode, $errorMessage, $healthEndPoint);
                                      """;

                command.Parameters.AddWithValue("$identifier", checkResult.Identifier.ToString());
                command.Parameters.AddWithValue("$timestamp", checkResult.Timestamp.ToString("o", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$statusCode", checkResult.StatusCode);
                command.Parameters.AddWithValue("$errorMessage", (object?)checkResult.ErrorMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("$healthEndPoint", checkResult.HealthEndPoint.ToString());

                await command.ExecuteNonQueryAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var message = "The HealthEndPointCheckResult could not be CREATED in the SQLite database";

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
        /// Asynchronously reads <see cref="HealthEndPointCheckResult"/> records from the database
        /// </summary>
        /// <param name="healthEndPointIdentifier">
        /// Optional filter to return only results for the specified <see cref="HealthEndPoint"/>
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableList{HealthEndPointCheckResult}"/>
        /// </returns>
        public async Task<ImmutableList<HealthEndPointCheckResult>> ReadAsync(Guid? healthEndPointIdentifier = null)
        {
            var list = new List<HealthEndPointCheckResult>();

            await using var connection = new SqliteConnection(this.connectionString);

            try
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();

                if (healthEndPointIdentifier.HasValue)
                {
                    command.CommandText = """
                                              SELECT
                                                  Identifier,
                                                  Timestamp,
                                                  StatusCode,
                                                  ErrorMessage,
                                                  HealthEndPoint
                                              FROM HealthEndPointCheckResults
                                              WHERE HealthEndPoint = $healthEndPoint;
                                          """;

                    command.Parameters.AddWithValue("$healthEndPoint", healthEndPointIdentifier.Value.ToString());
                }
                else
                {
                    command.CommandText = """
                                              SELECT
                                                  Identifier,
                                                  Timestamp,
                                                  StatusCode,
                                                  ErrorMessage,
                                                  HealthEndPoint
                                              FROM HealthEndPointCheckResults;
                                          """;
                }

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var checkResult = new HealthEndPointCheckResult
                    {
                        Identifier = Guid.Parse(reader.GetString(0)),
                        Timestamp = DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind),
                        StatusCode = reader.GetInt32(2),
                        ErrorMessage = reader.IsDBNull(3) ? null : reader.GetString(3),
                        HealthEndPoint = Guid.Parse(reader.GetString(4))
                    };

                    list.Add(checkResult);
                }

                return list.ToImmutableList();
            }
            catch (Exception ex)
            {
                var message = "The HealthEndPointCheckResult instances could not be READ from the SQLite database";

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
    }
}
