// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthEndPointClient.cs">
//
//    Copyright (c) 2025-2026 Sam Gerené
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
//  </copyright>
//  ------------------------------------------------------------------------------------------------

namespace Argus.Health.Pulse.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using ArgusTransfer.Extensions;
    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;

    using ArgusTransfer.Protocol;
    using ArgusTransfer.Client;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A typed client for <see cref="HealthEndPoint"/> CRUD operations over the Argus named-pipe IPC protocol.
    /// The timeout and the retries of a call are those of the <see cref="ArgusClient"/> it is given; see
    /// <see cref="CreateArgusClient"/>
    /// </summary>
    public class HealthEndPointClient
    {
        /// <summary>
        /// The time a call may take in total, including its retries and the delays between them
        /// </summary>
        public static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The <see cref="ILogger{HealthEndPointClient}"/> used for logging
        /// </summary>
        private readonly ILogger<HealthEndPointClient> logger;

        /// <summary>
        /// The <see cref="ArgusClient"/> used to send requests
        /// </summary>
        private readonly ArgusClient argusClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndPointClient"/> class
        /// </summary>
        /// <param name="argusClient">
        /// The <see cref="ArgusClient"/> used to send requests
        /// </param>
        /// <param name="logger">
        /// The <see cref="ILogger{HealthEndPointModule}"/> used for logging
        /// </param>
        public HealthEndPointClient(ArgusClient argusClient, ILogger<HealthEndPointClient> logger)
        {
            ArgumentNullException.ThrowIfNull(argusClient);

            this.argusClient = argusClient;
            this.logger = logger;
        }

        /// <summary>
        /// Creates the <see cref="ArgusClient"/> for a <see cref="HealthEndPointClient"/>: a call takes at most
        /// <see cref="CallTimeout"/> and a transient pipe failure is retried up to three times with an exponential
        /// backoff. A request that may already have reached the service is only retried when it is idempotent, so
        /// a create is never sent twice. Compression of large bodies is enabled
        /// </summary>
        /// <param name="pipeName">
        /// The name of the named pipe the Argus Health Service listens on
        /// </param>
        /// <param name="logger">
        /// The optional <see cref="ILogger"/> the <see cref="ArgusClient"/> logs its retries to
        /// </param>
        /// <returns>
        /// The configured <see cref="ArgusClient"/>
        /// </returns>
        public static ArgusClient CreateArgusClient(string pipeName, ILogger? logger = null)
        {
            var client = new ArgusClient(pipeName)
            {
                DefaultTimeout = CallTimeout,
                RetryPolicy = new ArgusRetryPolicy
                {
                    MaxRetries = 3,
                    InitialDelay = TimeSpan.FromMilliseconds(500),
                    Strategy = RetryBackoffStrategy.Exponential
                }
            };

            client.Compression.Enabled = true;

            if (logger != null)
            {
                client.Logger = logger;
            }

            return client;
        }

        /// <summary>
        /// Retrieves all <see cref="HealthEndPoint"/> instances from the server
        /// </summary>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> used to signal cancellation
        /// </param>
        /// <returns>
        /// An <see cref="IList{HealthEndPoint}"/> containing all endpoints
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a non-success status code
        /// </exception>
        public async Task<IList<HealthEndPoint>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = "/healthendpoint"
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, null, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw CreateServerException(response);
            }

            return HealthEndPointReader.ReadArray(response.Body!);
        }

        /// <summary>
        /// Retrieves a single <see cref="HealthEndPoint"/> by its identifier
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier of the endpoint to retrieve
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> used to signal cancellation
        /// </param>
        /// <returns>
        /// The <see cref="HealthEndPoint"/> matching the specified identifier
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a non-success status code
        /// </exception>
        public async Task<HealthEndPoint> GetByIdAsync(Guid identifier, CancellationToken cancellationToken = default)
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, null, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw CreateServerException(response);
            }

            return HealthEndPointReader.Read(response.Body!);
        }

        /// <summary>
        /// Creates a new <see cref="HealthEndPoint"/> on the server
        /// </summary>
        /// <param name="healthEndPoint">
        /// The <see cref="HealthEndPoint"/> to create
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> used to signal cancellation
        /// </param>
        /// <returns>
        /// The created <see cref="HealthEndPoint"/> as returned by the server
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a non-success status code
        /// </exception>
        public async Task<HealthEndPoint> CreateAsync(HealthEndPoint healthEndPoint, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(healthEndPoint);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/healthendpoint",
                Body = HealthEndPointWriter.Write(healthEndPoint)
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, null, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Created)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw CreateServerException(response);
            }

            return HealthEndPointReader.Read(response.Body!);
        }

        /// <summary>
        /// Updates an existing <see cref="HealthEndPoint"/> on the server
        /// </summary>
        /// <param name="healthEndPoint">
        /// The <see cref="HealthEndPoint"/> to update. The <see cref="HealthEndPoint.Identifier"/> must be set
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> used to signal cancellation
        /// </param>
        /// <returns>
        /// The updated <see cref="HealthEndPoint"/> as returned by the server
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a non-success status code
        /// </exception>
        public async Task<HealthEndPoint> UpdateAsync(HealthEndPoint healthEndPoint, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(healthEndPoint);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.PUT,
                Route = $"/healthendpoint/{healthEndPoint.Identifier.ToShortGuid()}",
                Body = HealthEndPointWriter.Write(healthEndPoint)
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, null, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw CreateServerException(response);
            }

            return HealthEndPointReader.Read(response.Body!);
        }

        /// <summary>
        /// Deletes a <see cref="HealthEndPoint"/> from the server. An endpoint that does not exist counts as
        /// deleted, so a delete that is retried after the server already processed it does not fail
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier of the endpoint to delete
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> used to signal cancellation
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a non-success status code
        /// </exception>
        public async Task DeleteAsync(Guid identifier, CancellationToken cancellationToken = default)
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.DELETE,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, null, cancellationToken);

            if (response.StatusCode == ArgusStatusCode.NotFound)
            {
                this.logger.LogInformation("The HealthEndPoint of {Verb} {Route} does not exist, it counts as deleted", request.Verb, request.Route);
                return;
            }

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw CreateServerException(response);
            }
        }

        /// <summary>
        /// Retrieves historical health check results for a specific endpoint from the server
        /// </summary>
        /// <param name="endpointIdentifier">
        /// The unique identifier of the endpoint to retrieve results for
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> used to signal cancellation
        /// </param>
        /// <returns>
        /// An <see cref="IList{HealthEndPointCheckResult}"/> containing the check results
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a non-success status code
        /// </exception>
        public async Task<IList<HealthEndPointCheckResult>> GetCheckResultsAsync(Guid endpointIdentifier, CancellationToken cancellationToken = default)
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = $"/healthendpoint/{endpointIdentifier.ToShortGuid()}/results"
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, null, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw CreateServerException(response);
            }

            return HealthEndPointCheckResultReader.ReadArray(response.Body!);
        }

        /// <summary>
        /// Retrieves aggregated uptime summaries for a specific endpoint from the server
        /// </summary>
        /// <param name="endpointIdentifier">
        /// The unique identifier of the endpoint to retrieve uptime summaries for
        /// </param>
        /// <param name="days">
        /// The number of days to look back (default 90)
        /// </param>
        /// <param name="resolution">
        /// The <see cref="UptimeResolution"/> for aggregation (default <see cref="UptimeResolution.Hour"/>)
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> used to signal cancellation
        /// </param>
        /// <returns>
        /// An <see cref="IList{UptimeSummary}"/> containing the aggregated uptime summaries
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a non-success status code
        /// </exception>
        public async Task<IList<UptimeSummary>> GetUptimeSummaryAsync(
            Guid endpointIdentifier,
            int days = 90,
            UptimeResolution resolution = UptimeResolution.Hour,
            CancellationToken cancellationToken = default)
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = $"/healthendpoint/{endpointIdentifier.ToShortGuid()}/uptime"
            };

            request.QueryParameters["days"] = days.ToString(CultureInfo.InvariantCulture);
            request.QueryParameters["resolution"] = resolution.ToString().ToLowerInvariant();

            this.logger.LogDebug("Sending {Verb} {Route}?days={Days}&resolution={Resolution}", request.Verb, request.Route, days, resolution);

            var response = await this.argusClient.SendAsync(request, null, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw CreateServerException(response);
            }

            return UptimeSummaryReader.ReadArray(response.Body!);
        }

        /// <summary>
        /// Creates the exception for a response with an unexpected status code. When the server answered
        /// with <see cref="ArgusProblemDetails"/>, its detail is included in the message
        /// </summary>
        /// <param name="response">
        /// The <see cref="ArgusResponse"/> returned by the server
        /// </param>
        /// <returns>
        /// The <see cref="InvalidOperationException"/> to throw
        /// </returns>
        private static InvalidOperationException CreateServerException(ArgusResponse response)
        {
            if (ArgusProblemDetails.TryRead(response, out var problemDetails) && !string.IsNullOrWhiteSpace(problemDetails.Detail))
            {
                return new InvalidOperationException($"Server returned {response.StatusCode}: {problemDetails.Detail}");
            }

            return new InvalidOperationException($"Server returned {response.StatusCode}");
        }
    }
}
