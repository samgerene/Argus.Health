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

    using Polly;
    using Polly.Timeout;

    
    /// <summary>
    /// Manages per-endpoint HTTP monitor loops with Polly retry and timeout
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly HttpClient httpClient = new();
        private readonly ConcurrentDictionary<Guid, (Task task, CancellationTokenSource cts)> monitors = new();
        private readonly Subject<HealthEndPointCheckResult> resultsSubject = new();

        public IObservable<HealthEndPointCheckResult> ResultsObservable => resultsSubject.AsObservable();

        public IObservable<HealthEndPointCheckResult> FailureObservable =>
            resultsSubject.Where(r => r.StatusCode < 200 || r.StatusCode >= 300);

        public void UpdateEndpoints(IList<HealthEndPoint> endpoints)
        {
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

        private void StartMonitor(HealthEndPoint endpoint)
        {
            var cts = new CancellationTokenSource();
            var task = MonitorEndpointAsync(endpoint, cts.Token);
            monitors[endpoint.Identifier] = (task, cts);
        }

        private void StopMonitor(Guid id)
        {
            if (monitors.TryRemove(id, out var monitor))
            {
                monitor.cts.Cancel();
                monitor.cts.Dispose();
            }
        }

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
                }
                catch (HttpRequestException ex)
                {
                    result.StatusCode = 0;
                    result.ErrorMessage = ex.Message;
                }
                catch (Exception ex)
                {
                    result.StatusCode = 0;
                    result.ErrorMessage = ex.Message;
                }

                resultsSubject.OnNext(result);

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

        public void Dispose()
        {
            foreach (var id in monitors.Keys.ToList())
            {
                StopMonitor(id);
            }

            resultsSubject.Dispose();
            httpClient.Dispose();
        }
    }
}
