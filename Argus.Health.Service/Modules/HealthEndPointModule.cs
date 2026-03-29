// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointModule.cs"  >
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

namespace Argus.Health.Service.Modules
{
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;
    using Argus.Health.Service.Repository;

    using ArgusTransfer.Extensions;
    using ArgusTransfer.Protocol;
    using ArgusTransfer.Routing;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Module that registers route handlers for <see cref="HealthEndPoint"/> CRUD operations
    /// </summary>
    public class HealthEndPointModule : IArgusModule
    {
        /// <summary>
        /// The <see cref="ILogger{HealthEndPointModule}"/> used for logging
        /// </summary>
        private readonly ILogger<HealthEndPointModule> logger;

        /// <summary>
        /// The <see cref="IHealthEndPointRepository"/> used for CRUD operations on health endpoints
        /// </summary>
        private readonly IHealthEndPointRepository healthEndPointRepository;

        /// <summary>
        /// The <see cref="IHealthEndPointCheckResultRepository"/> used for reading check results
        /// </summary>
        private readonly IHealthEndPointCheckResultRepository healthEndPointCheckResultRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndPointModule"/> class
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{HealthEndPointModule}"/> used for logging
        /// </param>
        /// <param name="healthEndPointRepository">
        /// The <see cref="IHealthEndPointRepository"/> used for CRUD operations
        /// </param>
        /// <param name="healthEndPointCheckResultRepository">
        /// The <see cref="IHealthEndPointCheckResultRepository"/> used for reading check results
        /// </param>
        public HealthEndPointModule(ILogger<HealthEndPointModule> logger, IHealthEndPointRepository healthEndPointRepository, IHealthEndPointCheckResultRepository healthEndPointCheckResultRepository)
        {
            this.logger = logger;
            this.healthEndPointRepository = healthEndPointRepository;
            this.healthEndPointCheckResultRepository = healthEndPointCheckResultRepository;
        }

        /// <summary>
        /// Registers route handlers for health endpoint operations
        /// </summary>
        /// <param name="app">
        /// The <see cref="IArgusRouteBuilder"/> to register routes on
        /// </param>
        public void AddRoutes(IArgusRouteBuilder app)
        {
            this.logger.LogDebug("register HealthEndPoint routes");

            app.MapGet("/healthendpoint", this.HandleGetAllAsync);
            app.MapGet("/healthendpoint/{identifier:ShortGuid}", this.HandleGetByIdAsync);
            app.MapPost("/healthendpoint", this.HandleCreateAsync);
            app.MapPut("/healthendpoint/{identifier:ShortGuid}", this.HandleUpdateAsync);
            app.MapDelete("/healthendpoint/{identifier:ShortGuid}", this.HandleDeleteAsync);
            app.MapGet("/healthendpoint/{identifier:ShortGuid}/results", this.HandleGetResultsAsync);

            this.logger.LogDebug("HealthEndPoint routes registered");
        }

        /// <summary>
        /// Handles a POST request to create a new <see cref="HealthEndPoint"/>
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        internal async Task HandleCreateAsync(ArgusContext context)
        {
            var sw = Stopwatch.StartNew();

            this.logger.LogDebug("Starting to create a new HealthEndPoint");

            var request = context.Request;

            try
            {
                var bodyJson = request.Body;

                if (string.IsNullOrWhiteSpace(bodyJson))
                {
                    this.logger.LogWarning("Create HealthEndPoint request received with empty body");

                    context.Response = new ArgusResponse
                    {
                        CorrelationToken = request.CorrelationToken,
                        StatusCode = ArgusStatusCode.BadRequest
                    };
                    return;
                }

                var healthEndPoint = HealthEndPointReader.Read(bodyJson);

                var result = await this.healthEndPointRepository.CreateAsync(healthEndPoint);

                if (result.IsSuccess)
                {
                    this.logger.LogInformation("HealthEndPoint {Name} created successfully via IPC",
                        healthEndPoint.Name);

                    context.Response = new ArgusResponse
                    {
                        CorrelationToken = request.CorrelationToken,
                        StatusCode = ArgusStatusCode.Created,
                        Body = HealthEndPointWriter.Write(healthEndPoint)
                    };
                    return;
                }

                this.logger.LogError("Failed to create HealthEndPoint: {Errors}", string.Join("; ", result.Errors));

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.InternalServerError
                };
            }
            catch (JsonException ex)
            {
                this.logger.LogError(ex, "Failed to deserialize HealthEndPoint from request body.");

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.BadRequest
                };
            }
            finally
            {
                this.logger.LogInformation("Create HealthEndPoint request completed in {ElapsedMilliseconds} [ms]", sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Handles a GET request to retrieve all <see cref="HealthEndPoint"/> instances
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        internal async Task HandleGetAllAsync(ArgusContext context)
        {
            var sw = Stopwatch.StartNew();

            this.logger.LogDebug("Starting to read all HealthEndPoints");

            var request = context.Request;

            try
            {
                var endpoints = await this.healthEndPointRepository.ReadAsync();

                this.logger.LogDebug("Retrieved {Count} HealthEndPoint(s) via IPC", endpoints.Count);

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.Ok,
                    Body = HealthEndPointWriter.Write(endpoints)
                };
            }
            catch (DataException ex)
            {
                this.logger.LogError(ex, "Failed to read HealthEndPoints.");

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.InternalServerError
                };
            }
            finally
            {
                this.logger.LogInformation("Read all HealthEndPoints request completed in {ElapsedMilliseconds} [ms]", sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Handles a GET request to retrieve a single <see cref="HealthEndPoint"/> by identifier
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        internal async Task HandleGetByIdAsync(ArgusContext context)
        {
            var sw = Stopwatch.StartNew();

            this.logger.LogDebug("Starting to read a specific HealthEndPoint");

            var request = context.Request;
            var routeValues = context.RouteValues;

            try
            {
                var identifier = routeValues["identifier"].FromShortGuid();
                var endpoints = await this.healthEndPointRepository.ReadAsync(new[] { identifier });

                if (!endpoints.Any())
                {
                    this.logger.LogDebug("HealthEndPoint with identifier {Identifier} not found", identifier);

                    context.Response = new ArgusResponse
                    {
                        CorrelationToken = request.CorrelationToken,
                        StatusCode = ArgusStatusCode.NotFound
                    };
                    return;
                }

                this.logger.LogDebug("Retrieved HealthEndPoint with identifier {Identifier} via IPC", identifier);

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.Ok,
                    Body = HealthEndPointWriter.Write(endpoints.First())
                };
            }
            catch (DataException ex)
            {
                this.logger.LogError(ex, "Failed to read HealthEndPoint.");

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.InternalServerError
                };
            }
            finally
            {
                this.logger.LogInformation("Read specific HealthEndPoint request completed in {ElapsedMilliseconds} [ms]", sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Handles a PUT request to update an existing <see cref="HealthEndPoint"/>
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        internal async Task HandleUpdateAsync(ArgusContext context)
        {
            var sw = Stopwatch.StartNew();

            this.logger.LogDebug("Starting to update a specific HealthEndPoint");

            var request = context.Request;
            var routeValues = context.RouteValues;

            try
            {
                var bodyJson = request.Body;

                if (string.IsNullOrWhiteSpace(bodyJson))
                {
                    this.logger.LogWarning("Update HealthEndPoint request received with empty body");

                    context.Response = new ArgusResponse
                    {
                        CorrelationToken = request.CorrelationToken,
                        StatusCode = ArgusStatusCode.BadRequest
                    };
                    return;
                }

                var identifier = routeValues["identifier"].FromShortGuid();
                var healthEndPoint = HealthEndPointReader.Read(bodyJson);
                healthEndPoint.Identifier = identifier;

                var result = await this.healthEndPointRepository.UpdateAsync(healthEndPoint);

                if (result.IsSuccess)
                {
                    this.logger.LogInformation("HealthEndPoint with identifier {Identifier} updated successfully via IPC", identifier);

                    context.Response = new ArgusResponse
                    {
                        CorrelationToken = request.CorrelationToken,
                        StatusCode = ArgusStatusCode.Ok,
                        Body = HealthEndPointWriter.Write(healthEndPoint)
                    };
                    return;
                }

                this.logger.LogError("Failed to update HealthEndPoint: {Errors}", string.Join("; ", result.Errors));

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.InternalServerError
                };
            }
            catch (JsonException ex)
            {
                this.logger.LogError(ex, "Failed to deserialize HealthEndPoint from request body.");

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.BadRequest
                };
            }
            finally
            {
                this.logger.LogInformation("Update specific HealthEndPoint request completed in {ElapsedMilliseconds} [ms]", sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Handles a DELETE request to remove a <see cref="HealthEndPoint"/>
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        internal async Task HandleDeleteAsync(ArgusContext context)
        {
            var sw = Stopwatch.StartNew();

            this.logger.LogDebug("Starting to delete a specific HealthEndPoint");

            var request = context.Request;
            var routeValues = context.RouteValues;

            var identifier = routeValues["identifier"].FromShortGuid();

            var healthEndPoint = new HealthEndPoint { Identifier = identifier };

            var result = await this.healthEndPointRepository.DeleteAsync(healthEndPoint);

            if (result.IsSuccess)
            {
                this.logger.LogInformation("HealthEndPoint with identifier {Identifier} deleted successfully via IPC", identifier);

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.Ok
                };
                return;
            }

            this.logger.LogError("Failed to delete HealthEndPoint: {Errors}", string.Join("; ", result.Errors));

            context.Response = new ArgusResponse
            {
                CorrelationToken = request.CorrelationToken,
                StatusCode = ArgusStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Handles a GET request to retrieve health check results for a specific endpoint
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> containing the request and route values
        /// </param>
        internal async Task HandleGetResultsAsync(ArgusContext context)
        {
            var sw = Stopwatch.StartNew();

            this.logger.LogDebug("Starting to read HealthEndPointCheckResults for a specific endpoint");

            var request = context.Request;
            var routeValues = context.RouteValues;

            var identifier = routeValues["identifier"].FromShortGuid();

            try
            {
                var results = await this.healthEndPointCheckResultRepository.ReadAsync(identifier);

                sw.Stop();

                this.logger.LogInformation("Retrieved {Count} check result(s) for endpoint {Identifier} in {ElapsedMs}ms",
                    results.Count, identifier, sw.ElapsedMilliseconds);

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.Ok,
                    Body = HealthEndPointCheckResultWriter.WriteArray(results)
                };
            }
            catch (System.Data.DataException ex)
            {
                this.logger.LogError(ex, "Failed to read check results for endpoint {Identifier}", identifier);

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.InternalServerError
                };
            }
        }
    }
}
