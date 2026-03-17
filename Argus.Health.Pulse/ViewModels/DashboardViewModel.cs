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

            var endpointSubscription = syncService.EndpointsObservable
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(endpoints =>
                {
                    var existingIds = Endpoints.Select(e => e.Identifier).ToHashSet();
                    var incomingIds = endpoints.Select(e => e.Identifier).ToHashSet();

                    // remove endpoints that no longer exist
                    var toRemove = Endpoints.Where(e => !incomingIds.Contains(e.Identifier)).ToList();
                    foreach (var item in toRemove)
                    {
                        Endpoints.Remove(item);
                    }

                    // add new endpoints
                    var added = 0;
                    foreach (var ep in endpoints)
                    {
                        if (!existingIds.Contains(ep.Identifier))
                        {
                            Endpoints.Add(new EndpointStatusViewModel(ep.Identifier, ep.Name, ep.Url));
                            added++;
                        }
                    }

                    this.logger.LogDebug("Endpoints synced: {AddedCount} added, {RemovedCount} removed, {TotalCount} total", added, toRemove.Count, Endpoints.Count);
                });

            var resultsSubscription = healthCheckService.ResultsObservable
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(result =>
                {
                    var row = Endpoints.FirstOrDefault(e => e.Identifier == result.HealthEndPoint);

                    if (row != null)
                    {
                        row.StatusCode = result.StatusCode;
                        row.LastChecked = result.Timestamp;
                        row.ErrorMessage = result.ErrorMessage;
                        this.logger.LogDebug("Health check result for {EndpointName}: HTTP {StatusCode}", row.Name, result.StatusCode);
                    }
                });

            disposables.Add(endpointSubscription);
            disposables.Add(resultsSubscription);
        }

        /// <summary>
        /// Gets the collection of endpoint status rows displayed on the dashboard
        /// </summary>
        public ObservableCollection<EndpointStatusViewModel> Endpoints { get; } = new();

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
