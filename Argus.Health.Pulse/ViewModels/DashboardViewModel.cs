// -------------------------------------------------------------------------------------------------
//  <copyright file="DashboardViewModel.cs">
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

namespace Argus.Health.Pulse.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Argus.Health.Pulse.Services;

    using DynamicData;

    using Microsoft.Extensions.Logging;

    using ReactiveUI.Avalonia;

    using ReactiveUI;

    /// <summary>
    /// Real-time status grid showing health of all endpoints
    /// </summary>
    public class DashboardViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// The <see cref="ILogger{DashboardViewModel}"/> used for logging
        /// </summary>
        private readonly ILogger<DashboardViewModel> logger;

        /// <summary>
        /// The <see cref="SourceCache{TObject,TKey}"/> backing the endpoint status collection
        /// </summary>
        private readonly SourceCache<EndpointStatusViewModel, Guid> endpointCache = new(e => e.Identifier);

        /// <summary>
        /// Disposable container for Rx subscriptions
        /// </summary>
        private readonly CompositeDisposable disposables = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardViewModel"/> class
        /// </summary>
        /// <param name="syncService">
        /// The <see cref="IEndpointSyncService"/> used to poll endpoints
        /// </param>
        /// <param name="healthCheckService">
        /// The <see cref="IHealthCheckService"/> used to monitor endpoint health
        /// </param>
        /// <param name="logger">
        /// The <see cref="ILogger{DashboardViewModel}"/> used for logging
        /// </param>
        public DashboardViewModel(IEndpointSyncService syncService, IHealthCheckService healthCheckService, ILogger<DashboardViewModel> logger)
        {
            this.logger = logger;

            var bindSubscription = this.endpointCache
                .Connect()
                .ObserveOn(AvaloniaScheduler.Instance)
                .Bind(out var endpoints)
                .Subscribe();

            this.disposables.Add(bindSubscription);

            this.Endpoints = endpoints;

            var endpointSubscription = syncService.EndpointsObservable
                .Subscribe(
                    incomingEndpoints =>
                    {
                        this.logger.LogDebug("EndpointsObservable received {Count} endpoint(s)", incomingEndpoints.Count);

                        this.endpointCache.Edit(updater =>
                        {
                            var incomingIds = incomingEndpoints.Select(e => e.Identifier).ToHashSet();
                            var staleKeys = updater.Keys.Where(k => !incomingIds.Contains(k)).ToList();

                            updater.RemoveKeys(staleKeys);

                            var added = 0;

                            foreach (var ep in incomingEndpoints)
                            {
                                if (!updater.Lookup(ep.Identifier).HasValue)
                                {
                                    updater.AddOrUpdate(new EndpointStatusViewModel(ep.Identifier, ep.Name, ep.Url));
                                    added++;
                                }
                            }

                            this.logger.LogDebug("Endpoints synced: {AddedCount} added, {RemovedCount} removed, {TotalCount} total", added, staleKeys.Count, updater.Count);
                        });
                    },
                    ex => this.logger.LogError(ex, "EndpointsObservable subscription error"));

            var resultsSubscription = healthCheckService.ResultsObservable
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(
                    result =>
                    {
                        var lookup = this.endpointCache.Lookup(result.HealthEndPoint);

                        if (lookup.HasValue)
                        {
                            var row = lookup.Value;
                            row.StatusCode = result.StatusCode;
                            row.LastChecked = result.Timestamp;
                            row.ErrorMessage = result.ErrorMessage;
                            this.logger.LogDebug("Health check result for {EndpointName}: HTTP {StatusCode}", row.Name, result.StatusCode);
                        }
                    },
                    ex => this.logger.LogError(ex, "ResultsObservable subscription error"));

            disposables.Add(endpointSubscription);
            disposables.Add(resultsSubscription);
        }

        /// <summary>
        /// Gets the collection of endpoint status rows displayed on the dashboard
        /// </summary>
        public ReadOnlyObservableCollection<EndpointStatusViewModel> Endpoints { get; }

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            this.disposables.Dispose();
            this.endpointCache.Dispose();
        }
    }
}
