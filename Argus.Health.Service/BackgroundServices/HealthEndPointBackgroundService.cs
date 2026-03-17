// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthEndPointBackgroundService.cs">
// 
//    Copyright (c) 2025-2026 Sam Gerené
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, softwareUseCases
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// 
//  </copyright>
//  ------------------------------------------------------------------------------------------------

namespace Argus.Health.Service.BackgroundServices
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Service.Repository;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Polly;
    using Polly.Timeout;
    using Polly.CircuitBreaker;

    /// <summary>
    /// The purpose of the <see cref="HealthEndPointBackgroundService"/> class is to perform
    /// Health endpoint checking
    /// </summary>
    public class HealthEndPointBackgroundService : BackgroundService
    {
        /// <summary>
        /// The (injected) logger
        /// </summary>
        private readonly ILogger<HealthEndPointBackgroundService> logger;

        /// <summary>
        /// The (injected) <see cref="IHttpClientFactory"/>
        /// </summary>
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// The (injected) <see cref="IHealthEndPointRepository"/>
        /// </summary>
        private readonly IHealthEndPointRepository healthEndPointRepository;

        /// <summary>
        /// The (injected) <see cref="IHealthEndPointCheckResultRepository"/>
        /// </summary>
        private readonly IHealthEndPointCheckResultRepository healthEndPointCheckResultRepository;

        /// <summary>
        /// Task store: endpoint identifier → (monitor task, cancellation)
        /// </summary>
        private readonly ConcurrentDictionary<Guid, (Task task, CancellationTokenSource cts)> monitors = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndPointBackgroundService"/> class.
        /// </summary>
        /// <param name="logger">
        /// The (injected) logger
        /// </param>
        /// <param name="httpClientFactory">
        /// The (injected) <see cref="IHttpClientFactory"/>
        /// </param>
        /// <param name="healthEndPointRepository">
        /// The (injected) <see cref="IHealthEndPointRepository"/>
        /// </param>
        /// <param name="healthEndPointCheckResultRepository">
        /// The (injected) <see cref="IHealthEndPointCheckResultRepository"/>
        /// </param>
        public HealthEndPointBackgroundService(ILogger<HealthEndPointBackgroundService> logger,
            IHttpClientFactory httpClientFactory,
            IHealthEndPointRepository healthEndPointRepository,
            IHealthEndPointCheckResultRepository healthEndPointCheckResultRepository)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.healthEndPointRepository = healthEndPointRepository;
            this.healthEndPointCheckResultRepository = healthEndPointCheckResultRepository;
        }

        /// <summary>
        /// Executes <see cref="HealthEndPointBackgroundService"/>> Task
        /// </summary>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/> to cancel execution of
        /// the <see cref="HealthEndPointBackgroundService"/>
        /// </param>
        /// <returns>
        /// an awaitable <see cref="Task"/>
        /// </returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Argus Health Worker started.");

            this.healthEndPointRepository.EndpointAdded += async (sender, healthEndPoint) =>
            {
                StartHealthEndPointMonitor(healthEndPoint);
            };

            this.healthEndPointRepository.EndpointUpdated += async (sender, healthEndPoint) =>
            {
                logger.LogInformation("Endpoint updated: {Name}", healthEndPoint.Name);
                StopHealthEndPointMonitor(healthEndPoint);

                if (healthEndPoint.IsActive)
                {
                    StartHealthEndPointMonitor(healthEndPoint);
                }
            };

            this.healthEndPointRepository.EndpointRemoved += async (sender, healthEndPoint) =>
            {
                logger.LogInformation("Endpoint removed: {Name}", healthEndPoint.Name);
                StopHealthEndPointMonitor(healthEndPoint);
            };

            foreach (var healthEndPoint in await healthEndPointRepository.ReadAsync())
            {
                this.StartHealthEndPointMonitor(healthEndPoint);
            }

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogInformation("Argus Health Worker stopping.");
            }
        }

        /// <summary>
        /// Start Monitoring the <see cref="HealthEndPoint"/>
        /// </summary>
        /// <param name="healthEndPoint">
        /// The subject <see cref="HealthEndPoint"/>
        /// </param>
        private void StartHealthEndPointMonitor(HealthEndPoint healthEndPoint)
        {
            if (!healthEndPoint.IsActive)
            {
                this.logger.LogInformation("Skipping inactive endpoint {Name}", healthEndPoint.Name);
                return;
            }

            if (this.monitors.ContainsKey(healthEndPoint.Identifier))
            {
                return;
            }

            var cts = new CancellationTokenSource();
            var task = this.MonitorEndpointAsync(healthEndPoint, cts.Token);
            this.monitors[healthEndPoint.Identifier] = (task, cts);
        }

        /// <summary>
        /// Stop Monitoring the <see cref="HealthEndPoint"/>
        /// </summary>
        /// <param name="healthEndPoint">
        /// The subject <see cref="HealthEndPoint"/>
        /// </param>
        private void StopHealthEndPointMonitor(HealthEndPoint healthEndPoint)
        {
            if (this.monitors.TryRemove(healthEndPoint.Identifier, out var monitor))
            {
                monitor.cts.Cancel();
                logger.LogInformation("Stopped monitor for endpoint {Id}", healthEndPoint.Identifier);
            }
        }

        /// <summary>
        /// Asynchronously check the provided <see cref="HealthEndPoint"/>
        /// </summary>
        /// <param name="healthEndPoint">
        /// The <see cref="HealthEndPoint"/> to monitor
        /// </param>
        /// <param name="ct">
        /// The <see cref="CancellationToken"/> used to cancel monitoring
        /// </param>
        /// <returns>
        /// an awaitable <see cref="Task"/>
        /// </returns>
        private async Task MonitorEndpointAsync(HealthEndPoint healthEndPoint, CancellationToken ct)
        {
            this.logger.LogDebug("Starting to check {Name}:{Url}", healthEndPoint.Name, healthEndPoint.Url);
            
            var client = httpClientFactory.CreateClient("ArgusHealth");

            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: healthEndPoint.RetryCount,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetry: (outcome, delay, attempt, context) =>
                    {
                        this.logger.LogWarning("[{Name}:{Url}:{ExceptionMessage}] Retry {Attempt} after {Delay}ms", outcome.Exception.Message, healthEndPoint.Name, healthEndPoint.Url, attempt, delay.TotalMilliseconds);
                    });

            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(healthEndPoint.Timeout),
                TimeoutStrategy.Optimistic);

            var breakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, breakDelay, context) =>
                                {
                                    this.logger.LogWarning("[{Name}:{ExceptionMessage}] Circuit breaker OPEN for {Delay}s", outcome.Exception.Message, healthEndPoint.Name, breakDelay.TotalSeconds);
                               },
                               onReset: (context) =>
                               {
                                   this.logger.LogInformation("[{Name}] Circuit breaker CLOSED", healthEndPoint.Name);
                               });
            
            var wrappedPolicies = Policy.WrapAsync(retryPolicy, timeoutPolicy, breakerPolicy);

            while (!ct.IsCancellationRequested)
            {
                var checkResult = new HealthEndPointCheckResult
                {
                    Timestamp = DateTime.UtcNow,
                    HealthEndPoint = healthEndPoint.Identifier
                };

                try
                {
                    var response = await wrappedPolicies.ExecuteAsync(() =>
                        client.GetAsync(healthEndPoint.Url, ct));

                    checkResult.StatusCode = (int)response.StatusCode;

                    var status = response.IsSuccessStatusCode
                        ? "✅ Healthy"
                        : $"⚠️ Unhealthy ({(int)response.StatusCode})";

                    this.logger.LogInformation("[{Name}] {Url} -> {Status}", healthEndPoint.Name, healthEndPoint.Url, status);
                }
                catch (BrokenCircuitException)
                {
                    checkResult.StatusCode = 0;
                    checkResult.ErrorMessage = "Circuit breaker is OPEN — request skipped";

                    this.logger.LogWarning("[{Name}] Circuit is OPEN — skipping {Url}", healthEndPoint.Name, healthEndPoint.Url);
                }
                catch (TimeoutRejectedException)
                {
                    checkResult.StatusCode = 0;
                    checkResult.ErrorMessage = $"Request timed out after {healthEndPoint.Timeout}s";

                    this.logger.LogWarning("[{Name}] {Url} timed out after {Timeout}s", healthEndPoint.Name, healthEndPoint.Url, healthEndPoint.Timeout);
                }
                catch (Exception ex)
                {
                    checkResult.StatusCode = 0;
                    checkResult.ErrorMessage = ex.Message;

                    this.logger.LogWarning(ex, "[{Name}] {Url} failed after retries", healthEndPoint.Name, healthEndPoint.Url);
                }

                await this.healthEndPointCheckResultRepository.CreateAsync(checkResult);

                await Task.Delay(TimeSpan.FromSeconds(healthEndPoint.Frequency), ct);
            }

            logger.LogInformation("Stopped Health Check monitor for [{Name}]", healthEndPoint.Name);
        }
    }
}
