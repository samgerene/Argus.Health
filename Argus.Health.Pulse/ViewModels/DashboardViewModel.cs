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

    using ReactiveUI;
    using ReactiveUI.Avalonia;
    using ReactiveUI.SourceGenerators;

    /// <summary>
    /// Real-time status grid showing health of all endpoints
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase, IDisposable
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
        /// Helper for <see cref="TotalEndpoints"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<int> totalEndpointsHelper;

        /// <summary>
        /// Helper for <see cref="EndpointsDown"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<int> endpointsDownHelper;

        /// <summary>
        /// Helper for <see cref="AverageResponseTimeMs"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<double> averageResponseTimeMsHelper;

        /// <summary>
        /// Helper for <see cref="OverallUptimePercent"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<double> overallUptimePercentHelper;

        /// <summary>
        /// Helper for <see cref="IncidentsLast24Hours"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<int> incidentsLast24HoursHelper;

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
                                var existing = updater.Lookup(ep.Identifier);

                                if (existing.HasValue)
                                {
                                    existing.Value.Name = ep.Name;
                                    existing.Value.Url = ep.Url;
                                }
                                else
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
                            row.AddCheckResult(result);
                            this.logger.LogDebug("Health check result for {EndpointName}: HTTP {StatusCode} in {ResponseTimeMs}ms", row.Name, result.StatusCode, result.ResponseTimeMs);
                        }
                    },
                    ex => this.logger.LogError(ex, "ResultsObservable subscription error"));

            this.disposables.Add(endpointSubscription);
            this.disposables.Add(resultsSubscription);

            // Summary card computations
            var summaryRefresh = this.endpointCache
                .Connect()
                .AutoRefresh(x => x.StatusCode)
                .AutoRefresh(x => x.ResponseTimeMs)
                .ObserveOn(AvaloniaScheduler.Instance);

            this.totalEndpointsHelper = summaryRefresh
                .QueryWhenChanged(q => q.Count)
                .ToProperty(this, x => x.TotalEndpoints);

            this.endpointsDownHelper = summaryRefresh
                .QueryWhenChanged(q => q.Items.Count(e => !e.IsHealthy && e.StatusCode != 0))
                .ToProperty(this, x => x.EndpointsDown);

            this.averageResponseTimeMsHelper = summaryRefresh
                .QueryWhenChanged(q =>
                {
                    var withResults = q.Items.Where(e => e.ResponseTimeMs > 0).ToList();
                    return withResults.Count > 0 ? Math.Round(withResults.Average(e => e.ResponseTimeMs), 1) : 0;
                })
                .ToProperty(this, x => x.AverageResponseTimeMs);

            this.overallUptimePercentHelper = summaryRefresh
                .QueryWhenChanged(q =>
                {
                    var withHistory = q.Items.Where(e => e.History.Count > 0).ToList();

                    if (withHistory.Count == 0)
                    {
                        return 0.0;
                    }

                    var cutoff = DateTime.UtcNow.AddHours(-24);
                    var totalChecks = 0;
                    var healthyChecks = 0;

                    foreach (var ep in withHistory)
                    {
                        foreach (var r in ep.History.Where(r => r.Timestamp >= cutoff))
                        {
                            totalChecks++;

                            if (r.StatusCode >= 200 && r.StatusCode < 300)
                            {
                                healthyChecks++;
                            }
                        }
                    }

                    return totalChecks > 0 ? Math.Round(100.0 * healthyChecks / totalChecks, 2) : 0.0;
                })
                .ToProperty(this, x => x.OverallUptimePercent);

            this.incidentsLast24HoursHelper = summaryRefresh
                .QueryWhenChanged(q =>
                {
                    var cutoff = DateTime.UtcNow.AddHours(-24);

                    return q.Items.Sum(e =>
                        e.History.Count(r => r.Timestamp >= cutoff && (r.StatusCode < 200 || r.StatusCode >= 300)));
                })
                .ToProperty(this, x => x.IncidentsLast24Hours);

            this.disposables.Add(this.totalEndpointsHelper);
            this.disposables.Add(this.endpointsDownHelper);
            this.disposables.Add(this.averageResponseTimeMsHelper);
            this.disposables.Add(this.overallUptimePercentHelper);
            this.disposables.Add(this.incidentsLast24HoursHelper);

            // Detail panel: create/dispose EndpointDetailViewModel when selection changes
            var selectionSubscription = this.WhenAnyValue(x => x.SelectedEndpoint)
                .Subscribe(selected =>
                {
                    this.SelectedDetail?.Dispose();
                    this.SelectedDetail = selected != null ? new EndpointDetailViewModel(selected) : null;
                });

            this.disposables.Add(selectionSubscription);
        }

        /// <summary>
        /// Gets the collection of endpoint status rows displayed on the dashboard
        /// </summary>
        public ReadOnlyObservableCollection<EndpointStatusViewModel> Endpoints { get; }

        /// <summary>
        /// Gets or sets the currently selected endpoint in the DataGrid
        /// </summary>
        [Reactive]
        public partial EndpointStatusViewModel? SelectedEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the detail view model for the currently selected endpoint
        /// </summary>
        [Reactive]
        public partial EndpointDetailViewModel? SelectedDetail { get; set; }

        /// <summary>
        /// Gets the total number of monitored endpoints
        /// </summary>
        public int TotalEndpoints => this.totalEndpointsHelper.Value;

        /// <summary>
        /// Gets the number of endpoints currently reporting unhealthy status
        /// </summary>
        public int EndpointsDown => this.endpointsDownHelper.Value;

        /// <summary>
        /// Gets the average response time across all endpoints in milliseconds
        /// </summary>
        public double AverageResponseTimeMs => this.averageResponseTimeMsHelper.Value;

        /// <summary>
        /// Gets the overall uptime percentage across all endpoints in the last 24 hours
        /// </summary>
        public double OverallUptimePercent => this.overallUptimePercentHelper.Value;

        /// <summary>
        /// Gets the number of non-healthy check results in the last 24 hours
        /// </summary>
        public int IncidentsLast24Hours => this.incidentsLast24HoursHelper.Value;

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            this.SelectedDetail?.Dispose();
            this.disposables.Dispose();
            this.endpointCache.Dispose();
        }
    }
}
