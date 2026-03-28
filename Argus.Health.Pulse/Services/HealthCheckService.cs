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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;

    using Microsoft.Extensions.Logging;

    using Polly;
    using Polly.Timeout;

    /// <summary>
    /// Manages per-endpoint HTTP monitor loops with Polly retry and timeout
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        /// <summary>
        /// The <see cref="ILogger{HealthCheckService}"/> used for logging
        /// </summary>
        private readonly ILogger<HealthCheckService> logger;

        /// <summary>
        /// The <see cref="HttpClient"/> used to probe endpoints
        /// </summary>
        private readonly HttpClient httpClient = new();

        /// <summary>
        /// Running monitor tasks keyed by endpoint identifier
        /// </summary>
        private readonly ConcurrentDictionary<Guid, (Task task, CancellationTokenSource cts)> monitors = new();

        /// <summary>
        /// Subject that publishes health check results
        /// </summary>
        private readonly Subject<HealthEndPointCheckResult> resultsSubject = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthCheckService"/> class
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{HealthCheckService}"/> used for logging
        /// </param>
        public HealthCheckService(ILogger<HealthCheckService> logger)
        {
            this.logger = logger;
        }

        /// <summary>Gets an observable of all health check results</summary>
        public IObservable<HealthEndPointCheckResult> ResultsObservable => resultsSubject.AsObservable();

        /// <summary>Gets an observable filtered to non-2xx results</summary>
        public IObservable<HealthEndPointCheckResult> FailureObservable =>
            resultsSubject.Where(r => r.StatusCode < 200 || r.StatusCode >= 300);

        /// <summary>Diffs the provided endpoints against currently running monitors and starts/stops as needed</summary>
        public void UpdateEndpoints(IList<HealthEndPoint> endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var desiredIds = new HashSet<Guid>(endpoints.Select(e => e.Identifier));

            // stop monitors for removed endpoints
            foreach (var id in monitors.Keys)
            {
                if (!desiredIds.Contains(id))
                {
                    StopMonitor(id);
                }
            }

            // start monitors for new endpoints
            foreach (var endpoint in endpoints)
            {
                if (!monitors.ContainsKey(endpoint.Identifier))
                {
                    StartMonitor(endpoint);
                }
            }
        }

        /// <summary>
        /// Creates a cancellation source and starts a monitor loop for the specified endpoint
        /// </summary>
        /// <param name="endpoint">
        /// The <see cref="HealthEndPoint"/> to monitor
        /// </param>
        private void StartMonitor(HealthEndPoint endpoint)
        {
            this.logger.LogInformation("Starting monitor for endpoint {EndpointName} ({EndpointId})", endpoint.Name, endpoint.Identifier);
            var cts = new CancellationTokenSource();
            var task = MonitorEndpointAsync(endpoint, cts.Token);
            this.monitors[endpoint.Identifier] = (task, cts);
        }

        /// <summary>
        /// Cancels and removes the monitor for the specified endpoint identifier
        /// </summary>
        /// <param name="id">
        /// The endpoint identifier
        /// </param>
        private void StopMonitor(Guid id)
        {
            if (this.monitors.TryRemove(id, out var monitor))
            {
                this.logger.LogInformation("Stopping monitor for endpoint {EndpointId}", id);
                monitor.cts.Cancel();
                monitor.cts.Dispose();
            }
        }

        /// <summary>
        /// Runs the polling loop for a single endpoint with Polly retry and timeout
        /// </summary>
        /// <param name="endpoint">
        /// The <see cref="HealthEndPoint"/> to poll
        /// </param>
        /// <param name="ct">
        /// The <see cref="CancellationToken"/> used to stop the loop
        /// </param>
        private async Task MonitorEndpointAsync(HealthEndPoint endpoint, CancellationToken ct)
        {
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: endpoint.RetryCount,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt));

            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(endpoint.Timeout),
                TimeoutStrategy.Optimistic);

            var wrappedPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);

            while (!ct.IsCancellationRequested)
            {
                var result = new HealthEndPointCheckResult
                {
                    Identifier = Guid.NewGuid(),
                    HealthEndPoint = endpoint.Identifier,
                    Timestamp = DateTime.UtcNow
                };

                try
                {
                    var response = await wrappedPolicy.ExecuteAsync(
                        async token => await httpClient.GetAsync(endpoint.Url, token), ct);

                    result.StatusCode = (int)response.StatusCode;

                    if (!response.IsSuccessStatusCode)
                    {
                        result.ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                        this.logger.LogWarning("Health check failed for {EndpointName}: {ErrorMessage}", endpoint.Name, result.ErrorMessage);
                    }
                    else
                    {
                        this.logger.LogDebug("Health check OK for {EndpointName}: HTTP {StatusCode}", endpoint.Name, result.StatusCode);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (TimeoutRejectedException)
                {
                    result.StatusCode = 408;
                    result.ErrorMessage = $"Timed out after {endpoint.Timeout}s";
                    this.logger.LogWarning("Health check timed out for {EndpointName} after {Timeout}s", endpoint.Name, endpoint.Timeout);
                }
                catch (HttpRequestException ex)
                {
                    result.StatusCode = 0;
                    result.ErrorMessage = ex.Message;
                    this.logger.LogWarning(ex, "Health check HTTP error for {EndpointName}", endpoint.Name);
                }
                catch (Exception ex)
                {
                    result.StatusCode = 0;
                    result.ErrorMessage = ex.Message;
                    this.logger.LogError(ex, "Health check unexpected error for {EndpointName}", endpoint.Name);
                }

                this.resultsSubject.OnNext(result);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(endpoint.Frequency), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>Stops all monitors and disposes managed resources</summary>
        public void Dispose()
        {
            foreach (var id in this.monitors.Keys.ToList())
            {
                this.StopMonitor(id);
            }

            this.resultsSubject.Dispose();
            this.httpClient.Dispose();
        }
    }
}
