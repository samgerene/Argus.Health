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
    using System.Threading;
    using System.Threading.Tasks;

    using ArgusTransfer.Extensions;
    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;

    using ArgusTransfer.Protocol;
    using ArgusTransfer.Client;

    using Microsoft.Extensions.Logging;
    
    /// <summary>
    /// A typed client for <see cref="HealthEndPoint"/> CRUD operations over the Argus named-pipe IPC protocol
    /// </summary>
    public class HealthEndPointClient
    {
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
            this.argusClient = argusClient;
            this.logger = logger;
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

            var response = await this.argusClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw new InvalidOperationException($"Server returned {response.StatusCode}");
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

            var response = await this.argusClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw new InvalidOperationException($"Server returned {response.StatusCode}");
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
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/healthendpoint",
                Body = HealthEndPointWriter.Write(healthEndPoint)
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Created)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw new InvalidOperationException($"Server returned {response.StatusCode}");
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
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.PUT,
                Route = $"/healthendpoint/{healthEndPoint.Identifier.ToShortGuid()}",
                Body = HealthEndPointWriter.Write(healthEndPoint)
            };

            this.logger.LogDebug("Sending {Verb} {Route}", request.Verb, request.Route);

            var response = await this.argusClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw new InvalidOperationException($"Server returned {response.StatusCode}");
            }

            return HealthEndPointReader.Read(response.Body!);
        }

        /// <summary>
        /// Deletes a <see cref="HealthEndPoint"/> from the server
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

            var response = await this.argusClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != ArgusStatusCode.Ok)
            {
                this.logger.LogWarning("Server returned {StatusCode} for {Verb} {Route}", response.StatusCode, request.Verb, request.Route);
                throw new InvalidOperationException($"Server returned {response.StatusCode}");
            }
        }
    }
}
