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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.Client;
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
        /// The <see cref="HealthEndPointClient"/> used to fetch historical check results
        /// </summary>
        private readonly HealthEndPointClient client;

        /// <summary>
        /// Tracks which endpoints have had their history bootstrapped from the service database
        /// </summary>
        private readonly HashSet<Guid> bootstrappedEndpoints = new();

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
        /// Helper for <see cref="IsEndpointsView"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> isEndpointsViewHelper;

        /// <summary>
        /// Helper for <see cref="IsIncidentsView"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> isIncidentsViewHelper;

        /// <summary>
        /// The <see cref="SourceList{T}"/> backing the incidents collection
        /// </summary>
        private readonly SourceList<IncidentItem> incidentSource = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardViewModel"/> class
        /// </summary>
        /// <param name="client">
        /// The <see cref="HealthEndPointClient"/> used to fetch historical check results
        /// </param>
        /// <param name="syncService">
        /// The <see cref="IEndpointSyncService"/> used to poll endpoints
        /// </param>
        /// <param name="healthCheckService">
        /// The <see cref="IHealthCheckService"/> used to monitor endpoint health
        /// </param>
        /// <param name="logger">
        /// The <see cref="ILogger{DashboardViewModel}"/> used for logging
        /// </param>
        public DashboardViewModel(HealthEndPointClient client, IEndpointSyncService syncService, IHealthCheckService healthCheckService, ILogger<DashboardViewModel> logger)
        {
            this.logger = logger;
            this.client = client;

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

                            var toBootstrap = new List<EndpointStatusViewModel>();

                            foreach (var ep in incomingEndpoints)
                            {
                                var existing = updater.Lookup(ep.Identifier);

                                if (existing.HasValue)
                                {
                                    existing.Value.Name = ep.Name;
                                    existing.Value.Url = ep.Url;
                                    existing.Value.IsActive = ep.IsActive;
                                }
                                else
                                {
                                    var row = new EndpointStatusViewModel(ep.Identifier, ep.Name, ep.Url, ep.IsActive);
                                    updater.AddOrUpdate(row);
                                    toBootstrap.Add(row);
                                    added++;
                                }
                            }

                            this.logger.LogDebug("Endpoints synced: {AddedCount} added, {RemovedCount} removed, {TotalCount} total", added, staleKeys.Count, updater.Count);

                            if (toBootstrap.Count > 0)
                            {
                                _ = this.BootstrapHistoryAsync(toBootstrap);
                            }
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

                            if (r.IsHealthy())
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
                        e.History.Count(r => r.Timestamp >= cutoff && !r.IsHealthy()));
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

                    if (selected != null)
                    {
                        var detail = new EndpointDetailViewModel(selected);
                        this.SelectedDetail = detail;
                        _ = this.LoadUptimeSummaryAsync(detail);

                        var uptimeRangeSubscription = detail.WhenAnyValue(d => d.SelectedUptimeDaysRange)
                            .Skip(1)
                            .Subscribe(range => _ = this.LoadUptimeSummaryAsync(detail));

                        detail.Disposables.Add(uptimeRangeSubscription);
                    }
                    else
                    {
                        this.SelectedDetail = null;
                    }
                });

            this.disposables.Add(selectionSubscription);

            // Filtered endpoints: re-filter when ActiveViewMode or ActiveActivityFilter changes
            var filterPredicate = this.WhenAnyValue(x => x.ActiveViewMode, x => x.ActiveActivityFilter)
                .Select<(DashboardViewMode mode, EndpointActivityFilter activity), Func<EndpointStatusViewModel, bool>>(t =>
                {
                    return e =>
                    {
                        var passesActivity = t.activity switch
                        {
                            EndpointActivityFilter.ActiveOnly => e.IsActive,
                            EndpointActivityFilter.InactiveOnly => !e.IsActive,
                            _ => true
                        };

                        if (!passesActivity)
                        {
                            return false;
                        }

                        return t.mode switch
                        {
                            DashboardViewMode.DownOnly => !e.IsHealthy && e.StatusCode != 0,
                            _ => true
                        };
                    };
                });

            var comparerObservable = this.WhenAnyValue(x => x.SortColumn, x => x.SortDirection)
                .Select(t => BuildComparer(t.Item1, t.Item2));

            var filteredSubscription = this.endpointCache
                .Connect()
                .AutoRefresh(x => x.StatusCode)
                .AutoRefresh(x => x.IsActive)
                .AutoRefresh(x => x.Name)
                .AutoRefresh(x => x.Url)
                .AutoRefresh(x => x.ResponseTimeMs)
                .AutoRefresh(x => x.LastChecked)
                .AutoRefresh(x => x.ErrorMessage)
                .Filter(filterPredicate)
                .ObserveOn(AvaloniaScheduler.Instance)
                .SortAndBind(out var filteredEndpoints, comparerObservable)
                .Subscribe();

            this.disposables.Add(filteredSubscription);
            this.FilteredEndpoints = filteredEndpoints;

            // Incidents list
            var incidentBindSubscription = this.incidentSource
                .Connect()
                .ObserveOn(AvaloniaScheduler.Instance)
                .Bind(out var incidentResults)
                .Subscribe();

            this.disposables.Add(incidentBindSubscription);
            this.IncidentResults = incidentResults;

            // Refresh incidents when switching to incidents view or when new results arrive
            var incidentRefreshSubscription = this.WhenAnyValue(x => x.ActiveViewMode)
                .Where(mode => mode == DashboardViewMode.Incidents)
                .Subscribe(_ => this.RefreshIncidents());

            this.disposables.Add(incidentRefreshSubscription);

            // View mode helpers
            this.isEndpointsViewHelper = this.WhenAnyValue(x => x.ActiveViewMode)
                .Select(mode => mode != DashboardViewMode.Incidents)
                .ToProperty(this, x => x.IsEndpointsView);

            this.isIncidentsViewHelper = this.WhenAnyValue(x => x.ActiveViewMode)
                .Select(mode => mode == DashboardViewMode.Incidents)
                .ToProperty(this, x => x.IsIncidentsView);

            this.disposables.Add(this.isEndpointsViewHelper);
            this.disposables.Add(this.isIncidentsViewHelper);

            // Commands
            this.ShowAllEndpointsCommand = ReactiveCommand.Create(() => this.ActiveViewMode = DashboardViewMode.AllEndpoints);
            this.ShowDownEndpointsCommand = ReactiveCommand.Create(() => this.ActiveViewMode = DashboardViewMode.DownOnly);
            this.ShowIncidentsCommand = ReactiveCommand.Create(() => this.ActiveViewMode = DashboardViewMode.Incidents);

            this.ShowActiveCommand = ReactiveCommand.Create(() => this.ActiveActivityFilter = EndpointActivityFilter.ActiveOnly);
            this.ShowInactiveCommand = ReactiveCommand.Create(() => this.ActiveActivityFilter = EndpointActivityFilter.InactiveOnly);
            this.ShowAllActivityCommand = ReactiveCommand.Create(() => this.ActiveActivityFilter = EndpointActivityFilter.All);
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
        /// Gets or sets the active view mode (all endpoints, down only, or incidents)
        /// </summary>
        [Reactive]
        public partial DashboardViewMode ActiveViewMode { get; set; }

        /// <summary>
        /// Gets or sets the active endpoint activity filter
        /// </summary>
        [Reactive]
        public partial EndpointActivityFilter ActiveActivityFilter { get; set; }

        /// <summary>
        /// Gets or sets the column the endpoint table is sorted by
        /// </summary>
        [Reactive]
        public partial EndpointSortColumn SortColumn { get; set; }

        /// <summary>
        /// Gets or sets the direction in which the endpoint table is sorted
        /// </summary>
        [Reactive]
        public partial ListSortDirection SortDirection { get; set; }

        /// <summary>
        /// Gets the filtered endpoint collection based on the active view mode
        /// </summary>
        public ReadOnlyObservableCollection<EndpointStatusViewModel> FilteredEndpoints { get; private set; } = null!;

        /// <summary>
        /// Gets the collection of failed check results in the last 24 hours
        /// </summary>
        public ReadOnlyObservableCollection<IncidentItem> IncidentResults { get; private set; } = null!;

        /// <summary>
        /// Gets a value indicating whether the endpoints grid is visible
        /// </summary>
        public bool IsEndpointsView => this.isEndpointsViewHelper.Value;

        /// <summary>
        /// Gets a value indicating whether the incidents list is visible
        /// </summary>
        public bool IsIncidentsView => this.isIncidentsViewHelper.Value;

        /// <summary>
        /// Gets the command to show all endpoints
        /// </summary>
        public ReactiveCommand<Unit, DashboardViewMode> ShowAllEndpointsCommand { get; private set; } = null!;

        /// <summary>
        /// Gets the command to filter to down endpoints only
        /// </summary>
        public ReactiveCommand<Unit, DashboardViewMode> ShowDownEndpointsCommand { get; private set; } = null!;

        /// <summary>
        /// Gets the command to switch to the incidents view
        /// </summary>
        public ReactiveCommand<Unit, DashboardViewMode> ShowIncidentsCommand { get; private set; } = null!;

        /// <summary>
        /// Gets the command to filter to active endpoints only
        /// </summary>
        public ReactiveCommand<Unit, EndpointActivityFilter> ShowActiveCommand { get; private set; } = null!;

        /// <summary>
        /// Gets the command to filter to inactive endpoints only
        /// </summary>
        public ReactiveCommand<Unit, EndpointActivityFilter> ShowInactiveCommand { get; private set; } = null!;

        /// <summary>
        /// Gets the command to show all endpoints regardless of activity status
        /// </summary>
        public ReactiveCommand<Unit, EndpointActivityFilter> ShowAllActivityCommand { get; private set; } = null!;

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            this.SelectedDetail?.Dispose();
            this.disposables.Dispose();
            this.endpointCache.Dispose();
            this.incidentSource.Dispose();
        }

        /// <summary>
        /// Builds an <see cref="IComparer{T}"/> for the endpoint table given the current sort column
        /// and direction. Null values for the chosen field always sort after non-null values
        /// regardless of direction
        /// </summary>
        /// <param name="column">
        /// The <see cref="EndpointSortColumn"/> indicating which field to sort by
        /// </param>
        /// <param name="direction">
        /// The <see cref="ListSortDirection"/> indicating ascending or descending order
        /// </param>
        /// <returns>
        /// An <see cref="IComparer{T}"/> over <see cref="EndpointStatusViewModel"/>
        /// </returns>
        private static IComparer<EndpointStatusViewModel> BuildComparer(EndpointSortColumn column, ListSortDirection direction)
        {
            var sign = direction == ListSortDirection.Ascending ? 1 : -1;

            return column switch
            {
                EndpointSortColumn.Url => Comparer<EndpointStatusViewModel>.Create((a, b) =>
                    sign * string.Compare(a.Url, b.Url, StringComparison.OrdinalIgnoreCase)),
                EndpointSortColumn.Status => Comparer<EndpointStatusViewModel>.Create((a, b) =>
                    sign * a.StatusCode.CompareTo(b.StatusCode)),
                EndpointSortColumn.ResponseTime => Comparer<EndpointStatusViewModel>.Create((a, b) =>
                    sign * a.ResponseTimeMs.CompareTo(b.ResponseTimeMs)),
                EndpointSortColumn.LastChecked => Comparer<EndpointStatusViewModel>.Create((a, b) =>
                {
                    if (a.LastChecked == null && b.LastChecked == null)
                    {
                        return 0;
                    }

                    if (a.LastChecked == null)
                    {
                        return 1;
                    }

                    if (b.LastChecked == null)
                    {
                        return -1;
                    }

                    return sign * a.LastChecked.Value.CompareTo(b.LastChecked.Value);
                }),
                EndpointSortColumn.ErrorMessage => Comparer<EndpointStatusViewModel>.Create((a, b) =>
                {
                    if (string.IsNullOrEmpty(a.ErrorMessage) && string.IsNullOrEmpty(b.ErrorMessage))
                    {
                        return 0;
                    }

                    if (string.IsNullOrEmpty(a.ErrorMessage))
                    {
                        return 1;
                    }

                    if (string.IsNullOrEmpty(b.ErrorMessage))
                    {
                        return -1;
                    }

                    return sign * string.Compare(a.ErrorMessage, b.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                }),
                _ => Comparer<EndpointStatusViewModel>.Create((a, b) =>
                    sign * string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)),
            };
        }

        /// <summary>
        /// Rebuilds the incidents list from all endpoint histories (last 24h, non-2xx)
        /// </summary>
        private void RefreshIncidents()
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);

            var incidents = this.endpointCache.Items
                .SelectMany(e => e.History
                    .Where(r => r.Timestamp >= cutoff && !r.IsHealthy())
                    .Select(r => new IncidentItem
                    {
                        EndpointName = e.Name,
                        Timestamp = r.Timestamp,
                        StatusCode = r.StatusCode,
                        ResponseTimeMs = r.ResponseTimeMs,
                        ErrorMessage = r.ErrorMessage
                    }))
                .OrderByDescending(i => i.Timestamp)
                .ToList();

            this.incidentSource.Edit(updater =>
            {
                updater.Clear();
                updater.AddRange(incidents);
            });
        }

        /// <summary>
        /// Fetches historical check results from the service for newly added endpoints
        /// and populates their history
        /// </summary>
        /// <param name="endpoints">
        /// The endpoint view models to bootstrap with historical data
        /// </param>
        private async Task BootstrapHistoryAsync(List<EndpointStatusViewModel> endpoints)
        {
            foreach (var row in endpoints)
            {
                if (this.bootstrappedEndpoints.Contains(row.Identifier))
                {
                    continue;
                }

                try
                {
                    var results = await this.client.GetCheckResultsAsync(row.Identifier);

                    if (results.Count > 0)
                    {
                        row.AddCheckResults(results);
                        this.logger.LogDebug("Bootstrapped {Count} historical result(s) for {EndpointName}", results.Count, row.Name);
                    }

                    this.bootstrappedEndpoints.Add(row.Identifier);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to bootstrap history for {EndpointName}", row.Name);
                }
            }
        }

        /// <summary>
        /// Fetches hourly and daily uptime summaries from the server and populates the detail view model
        /// </summary>
        /// <param name="detail">
        /// The <see cref="EndpointDetailViewModel"/> to populate with uptime data
        /// </param>
        private async Task LoadUptimeSummaryAsync(EndpointDetailViewModel detail)
        {
            try
            {
                var days = detail.SelectedUptimeDaysRange.ToDays();

                var hourlyTask = this.client.GetUptimeSummaryAsync(
                    detail.Endpoint.Identifier, days, UptimeResolution.Hour);

                var dailyTask = this.client.GetUptimeSummaryAsync(
                    detail.Endpoint.Identifier, days, UptimeResolution.Day);

                await Task.WhenAll(hourlyTask, dailyTask);

                detail.HourlySummaries = hourlyTask.Result as IReadOnlyList<UptimeSummary> ?? hourlyTask.Result.ToList();
                detail.DailySummaries = dailyTask.Result as IReadOnlyList<UptimeSummary> ?? dailyTask.Result.ToList();

                this.logger.LogDebug("Loaded {HourlyCount} hourly and {DailyCount} daily uptime summaries for {EndpointName}",
                    hourlyTask.Result.Count, dailyTask.Result.Count, detail.Endpoint.Name);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to load uptime summary for {EndpointName}", detail.Endpoint.Name);
            }
        }
    }
}
