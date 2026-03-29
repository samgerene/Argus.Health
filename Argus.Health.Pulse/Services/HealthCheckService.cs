// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthCheckService.cs">
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

namespace Argus.Health.Pulse.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.Client;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Polls the Argus Health service via IPC for recent health check results per endpoint
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        /// <summary>
        /// The polling interval for fetching new check results
        /// </summary>
        public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The <see cref="ILogger{HealthCheckService}"/> used for logging
        /// </summary>
        private readonly ILogger<HealthCheckService> logger;

        /// <summary>
        /// The <see cref="HealthEndPointClient"/> used to fetch check results via IPC
        /// </summary>
        private readonly HealthEndPointClient client;

        /// <summary>
        /// Subject that publishes health check results
        /// </summary>
        private readonly Subject<HealthEndPointCheckResult> resultsSubject = new();

        /// <summary>
        /// Tracks the newest known result timestamp per endpoint to only emit new results
        /// </summary>
        private readonly Dictionary<Guid, DateTime> lastKnownTimestamp = new();

        /// <summary>
        /// The current set of endpoint identifiers to poll
        /// </summary>
        private readonly List<Guid> endpointIds = new();

        /// <summary>
        /// Lock for thread-safe access to <see cref="endpointIds"/>
        /// </summary>
        private readonly object endpointLock = new();

        /// <summary>
        /// The current polling subscription
        /// </summary>
        private IDisposable? pollSubscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthCheckService"/> class
        /// </summary>
        /// <param name="client">
        /// The <see cref="HealthEndPointClient"/> used to fetch check results via IPC
        /// </param>
        /// <param name="logger">
        /// The <see cref="ILogger{HealthCheckService}"/> used for logging
        /// </param>
        public HealthCheckService(HealthEndPointClient client, ILogger<HealthCheckService> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        /// <summary>
        /// Gets an observable of all health check results
        /// </summary>
        public IObservable<HealthEndPointCheckResult> ResultsObservable => this.resultsSubject.AsObservable();

        /// <summary>
        /// Gets an observable filtered to non-2xx results
        /// </summary>
        public IObservable<HealthEndPointCheckResult> FailureObservable =>
            this.resultsSubject.Where(r => r.StatusCode < 200 || r.StatusCode >= 300);

        /// <summary>
        /// Starts polling the service for recent check results
        /// </summary>
        public void Start()
        {
            this.Stop();

            this.pollSubscription = Observable.Timer(TimeSpan.FromSeconds(1), PollInterval)
                .SelectMany(_ => Observable.FromAsync(this.PollAsync))
                .Subscribe();

            this.logger.LogInformation("Health check result polling started");
        }

        /// <summary>
        /// Stops polling
        /// </summary>
        public void Stop()
        {
            this.pollSubscription?.Dispose();
            this.pollSubscription = null;
            this.logger.LogInformation("Health check result polling stopped");
        }

        /// <summary>
        /// Updates the set of endpoint identifiers to poll for check results
        /// </summary>
        /// <param name="endpointIds">
        /// The current set of endpoint identifiers
        /// </param>
        public void SetEndpointIds(IEnumerable<Guid> endpointIds)
        {
            lock (this.endpointLock)
            {
                this.endpointIds.Clear();
                this.endpointIds.AddRange(endpointIds);
            }
        }

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            this.pollSubscription?.Dispose();
            this.resultsSubject.Dispose();
        }

        /// <summary>
        /// Fetches check results for each known endpoint and emits only new results
        /// </summary>
        private async Task PollAsync()
        {
            List<Guid> ids;

            lock (this.endpointLock)
            {
                ids = this.endpointIds.ToList();
            }

            foreach (var endpointId in ids)
            {
                try
                {
                    var results = await this.client.GetCheckResultsAsync(endpointId);

                    this.lastKnownTimestamp.TryGetValue(endpointId, out var lastTimestamp);

                    var newResults = results.Where(r => r.Timestamp > lastTimestamp).ToList();

                    foreach (var result in newResults)
                    {
                        this.resultsSubject.OnNext(result);

                        if (result.Timestamp > lastTimestamp)
                        {
                            lastTimestamp = result.Timestamp;
                        }
                    }

                    if (newResults.Count > 0)
                    {
                        this.lastKnownTimestamp[endpointId] = lastTimestamp;
                        this.logger.LogDebug("Polled {Count} new check result(s) for endpoint {EndpointId}", newResults.Count, endpointId);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to poll check results for endpoint {EndpointId}", endpointId);
                }
            }
        }
    }
}
