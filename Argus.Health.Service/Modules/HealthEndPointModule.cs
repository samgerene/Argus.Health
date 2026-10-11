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
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;
    using Argus.Health.Service.Repository;

    using ArgusTransfer.Extensions;
    using ArgusTransfer.Protocol;
    using ArgusTransfer.Routing;

    using FluentResults;

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
            app.MapGet("/healthendpoint/{identifier:ShortGuid}/uptime", this.HandleGetUptimeAsync);

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

                    SetProblem(context, ArgusStatusCode.BadRequest, "The request body is empty; a HealthEndPoint is expected.");
                    return;
                }

                var healthEndPoint = HealthEndPointReader.Read(bodyJson);

                if (!this.TryValidate(context, healthEndPoint))
                {
                    return;
                }

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

                SetFailure(context, result, "created");
            }
            catch (JsonException ex)
            {
                this.logger.LogError(ex, "Failed to deserialize HealthEndPoint from request body.");

                SetProblem(context, ArgusStatusCode.BadRequest, $"The request body is not a valid HealthEndPoint: {ex.Message}");
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

                SetProblem(context, ArgusStatusCode.InternalServerError, "The HealthEndPoints could not be read.");
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

                    SetProblem(context, ArgusStatusCode.NotFound, $"No HealthEndPoint with identifier {identifier} exists.");
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

                SetProblem(context, ArgusStatusCode.InternalServerError, "The HealthEndPoint could not be read.");
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

                    SetProblem(context, ArgusStatusCode.BadRequest, "The request body is empty; a HealthEndPoint is expected.");
                    return;
                }

                var identifier = routeValues["identifier"].FromShortGuid();
                var healthEndPoint = HealthEndPointReader.Read(bodyJson);
                healthEndPoint.Identifier = identifier;

                if (!this.TryValidate(context, healthEndPoint))
                {
                    return;
                }

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

                SetFailure(context, result, "updated");
            }
            catch (JsonException ex)
            {
                this.logger.LogError(ex, "Failed to deserialize HealthEndPoint from request body.");

                SetProblem(context, ArgusStatusCode.BadRequest, $"The request body is not a valid HealthEndPoint: {ex.Message}");
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

            SetFailure(context, result, "deleted");
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

                SetProblem(context, ArgusStatusCode.InternalServerError, $"The check results of HealthEndPoint {identifier} could not be read.");
            }
        }

        /// <summary>
        /// Handles a GET request to retrieve aggregated uptime summaries for a specific endpoint
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> containing the request and route values
        /// </param>
        internal async Task HandleGetUptimeAsync(ArgusContext context)
        {
            var sw = Stopwatch.StartNew();

            this.logger.LogDebug("Starting to read uptime summary for a specific endpoint");

            var request = context.Request;
            var routeValues = context.RouteValues;

            var identifier = routeValues["identifier"].FromShortGuid();

            var days = 90;

            if (context.QueryValues.TryGetValue("days", out var daysValue)
                && int.TryParse(daysValue, CultureInfo.InvariantCulture, out var parsedDays)
                && parsedDays is > 0 and <= 365)
            {
                days = parsedDays;
            }

            var resolution = UptimeResolution.Hour;

            if (context.QueryValues.TryGetValue("resolution", out var resolutionValue)
                && string.Equals(resolutionValue, "day", StringComparison.OrdinalIgnoreCase))
            {
                resolution = UptimeResolution.Day;
            }

            try
            {
                var summaries = await this.healthEndPointCheckResultRepository.ReadUptimeSummaryAsync(identifier, days, resolution);

                sw.Stop();

                this.logger.LogInformation("Retrieved {Count} uptime summary period(s) for endpoint {Identifier} in {ElapsedMs}ms",
                    summaries.Count, identifier, sw.ElapsedMilliseconds);

                context.Response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = ArgusStatusCode.Ok,
                    Body = UptimeSummaryWriter.WriteArray(summaries)
                };
            }
            catch (DataException ex)
            {
                this.logger.LogError(ex, "Failed to read uptime summary for endpoint {Identifier}", identifier);

                SetProblem(context, ArgusStatusCode.InternalServerError, $"The uptime summary of HealthEndPoint {identifier} could not be read.");
            }
        }

        /// <summary>
        /// Answers the request with an <see cref="ArgusProblemDetails"/> response that carries the
        /// correlation token of the request
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        /// <param name="statusCode">
        /// The <see cref="ArgusStatusCode"/> of the problem
        /// </param>
        /// <param name="detail">
        /// A human-readable explanation of the problem
        /// </param>
        private static void SetProblem(ArgusContext context, ArgusStatusCode statusCode, string detail)
        {
            var response = context.Problem(statusCode, detail);

            response.CorrelationToken = context.Request.CorrelationToken;
        }

        /// <summary>
        /// Answers the request for a failed repository operation: 404 when the <see cref="HealthEndPoint"/>
        /// does not exist, 409 when it conflicts with an existing one, otherwise 500
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        /// <param name="result">
        /// The failed <see cref="ResultBase"/>
        /// </param>
        /// <param name="action">
        /// The past participle of the operation that failed, such as <c>created</c>
        /// </param>
        private static void SetFailure(ArgusContext context, ResultBase result, string action)
        {
            var statusCode = ArgusStatusCode.InternalServerError;

            if (result.HasError<NotFoundError>())
            {
                statusCode = ArgusStatusCode.NotFound;
            }
            else if (result.HasError<ConflictError>())
            {
                statusCode = ArgusStatusCode.Conflict;
            }

            SetProblem(context, statusCode, $"The HealthEndPoint could not be {action}: {DescribeErrors(result)}");
        }

        /// <summary>
        /// Validates the values of a <see cref="HealthEndPoint"/> sent by a client and answers the request
        /// with a 422 problem details response when they are not valid
        /// </summary>
        /// <param name="context">
        /// The <see cref="ArgusContext"/> for the current request
        /// </param>
        /// <param name="healthEndPoint">
        /// The <see cref="HealthEndPoint"/> to validate
        /// </param>
        /// <returns>
        /// <c>true</c> when the <see cref="HealthEndPoint"/> is valid; otherwise <c>false</c>
        /// </returns>
        private bool TryValidate(ArgusContext context, HealthEndPoint healthEndPoint)
        {
            string? problem = null;

            if (string.IsNullOrWhiteSpace(healthEndPoint.Name))
            {
                problem = "The name of a HealthEndPoint must not be empty.";
            }
            else if (!Uri.TryCreate(healthEndPoint.Url, UriKind.Absolute, out _))
            {
                problem = "The url of a HealthEndPoint must be an absolute URL.";
            }
            else if (healthEndPoint.Frequency <= 0)
            {
                problem = "The frequency of a HealthEndPoint must be greater than zero.";
            }
            else if (healthEndPoint.Timeout <= 0)
            {
                problem = "The timeout of a HealthEndPoint must be greater than zero.";
            }
            else if (healthEndPoint.RetryCount < 0)
            {
                problem = "The retry count of a HealthEndPoint must not be negative.";
            }

            if (problem == null)
            {
                return true;
            }

            this.logger.LogWarning("HealthEndPoint request rejected: {Problem}", problem);

            SetProblem(context, ArgusStatusCode.UnprocessableEntity, problem);

            return false;
        }

        /// <summary>
        /// Joins the error messages of a failed <see cref="ResultBase"/> into one text
        /// </summary>
        /// <param name="result">
        /// The failed <see cref="ResultBase"/>
        /// </param>
        /// <returns>
        /// The error messages, separated by a semicolon
        /// </returns>
        private static string DescribeErrors(ResultBase result)
        {
            return string.Join("; ", result.Errors.Select(error => error.Message));
        }
    }
}
