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
    using System.Threading;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Collections;
    using Argus.Health.Pulse.Services;

    using Microsoft.Extensions.Logging;

    using ReactiveUI;
    using ReactiveUI.Primitives;
    using ReactiveUI.Primitives.Concurrency;
    using ReactiveUI.Primitives.Disposables;
    using ReactiveUI.Primitives.Signals;
    using ReactiveUI.SourceGenerators;

    /// <summary>
    /// Real-time status grid showing health of all endpoints
    /// </summary>
    public sealed partial class DashboardViewModel : ViewModelBase, IDisposable
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
        /// The names of the <see cref="EndpointStatusViewModel"/> properties that the filter, the sort order
        /// or a summary value depends on
        /// </summary>
        private static readonly HashSet<string> RefreshingProperties = new()
        {
            nameof(EndpointStatusViewModel.StatusCode),
            nameof(EndpointStatusViewModel.IsActive),
            nameof(EndpointStatusViewModel.Name),
            nameof(EndpointStatusViewModel.Url),
            nameof(EndpointStatusViewModel.ResponseTimeMs),
            nameof(EndpointStatusViewModel.LastChecked),
            nameof(EndpointStatusViewModel.ErrorMessage)
        };

        /// <summary>
        /// The endpoint status rows by identifier. Like the collections below it is only used on the UI thread
        /// </summary>
        private readonly Dictionary<Guid, EndpointStatusViewModel> endpointRows = new();

        /// <summary>
        /// The endpoint status rows in the order in which they were added, backing <see cref="Endpoints"/>
        /// </summary>
        private readonly ObservableCollection<EndpointStatusViewModel> endpoints = new();

        /// <summary>
        /// The filtered and sorted endpoint status rows, backing <see cref="FilteredEndpoints"/>
        /// </summary>
        private readonly ObservableCollection<EndpointStatusViewModel> filteredEndpoints = new();

        /// <summary>
        /// Publishes a value when the filtered rows and the summary values have to be computed again
        /// </summary>
        private readonly Signal<RxVoid> refreshRequests = new();

        /// <summary>
        /// Disposable container for Rx subscriptions
        /// </summary>
        private readonly MultipleDisposable disposables = new();

        /// <summary>
        /// 1 while a refresh was requested that has not started yet, otherwise 0
        /// </summary>
        private int refreshPending;

        /// <summary>
        /// Helper for <see cref="IsEndpointsView"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> isEndpointsViewHelper;

        /// <summary>
        /// Helper for <see cref="IsIncidentsView"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> isIncidentsViewHelper;

        /// <summary>
        /// The incidents of the last 24 hours, backing <see cref="IncidentResults"/>
        /// </summary>
        private readonly ObservableCollection<IncidentItem> incidents = new();

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

            this.Endpoints = new ReadOnlyObservableCollection<EndpointStatusViewModel>(this.endpoints);
            this.FilteredEndpoints = new ReadOnlyObservableCollection<EndpointStatusViewModel>(this.filteredEndpoints);
            this.IncidentResults = new ReadOnlyObservableCollection<IncidentItem>(this.incidents);

            // A row requests a refresh when one of its properties changes, for as long as it is in the collection
            var rowSubscription = this.endpoints.ActOnEveryObject(
                row => row.PropertyChanged += this.OnRowPropertyChanged,
                row => row.PropertyChanged -= this.OnRowPropertyChanged);

            this.disposables.Add(rowSubscription);

            var endpointSubscription = syncService.EndpointsObservable
                .ObserveOn(UiSequencer.Current)
                .Subscribe(
                    this.SyncEndpoints,
                    ex => this.logger.LogError(ex, "EndpointsObservable subscription error"));

            var resultsSubscription = healthCheckService.ResultsObservable
                .ObserveOn(UiSequencer.Current)
                .Subscribe(
                    result =>
                    {
                        if (this.endpointRows.TryGetValue(result.HealthEndPoint, out var row))
                        {
                            row.AddCheckResult(result);
                            this.logger.LogDebug("Health check result for {EndpointName}: HTTP {StatusCode} in {ResponseTimeMs}ms", row.Name, result.StatusCode, result.ResponseTimeMs);
                        }
                    },
                    ex => this.logger.LogError(ex, "ResultsObservable subscription error"));

            this.disposables.Add(endpointSubscription);
            this.disposables.Add(resultsSubscription);

            // Filtered rows and summary cards: computed again on the UI thread when a row, the filter or
            // the sort order changes. Requests that arrive before a refresh has started are merged into it
            var refreshSubscription = this.refreshRequests
                .ObserveOn(UiSequencer.Current)
                .Subscribe(_ =>
                {
                    Interlocked.Exchange(ref this.refreshPending, 0);
                    this.Refresh();
                });

            this.disposables.Add(refreshSubscription);

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

            // Filtered endpoints: filter and sort again when the view mode, the activity filter, the sort
            // column or the sort direction changes
            var filterSubscription = this.WhenAnyValue(x => x.ActiveViewMode, x => x.ActiveActivityFilter, x => x.SortColumn, x => x.SortDirection)
                .Subscribe(_ => this.RequestRefresh());

            this.disposables.Add(filterSubscription);

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
        [Reactive]
        public partial int TotalEndpoints { get; private set; }

        /// <summary>
        /// Gets the number of endpoints currently reporting unhealthy status
        /// </summary>
        [Reactive]
        public partial int EndpointsDown { get; private set; }

        /// <summary>
        /// Gets the average response time across all endpoints in milliseconds
        /// </summary>
        [Reactive]
        public partial double AverageResponseTimeMs { get; private set; }

        /// <summary>
        /// Gets the overall uptime percentage across all endpoints in the last 24 hours
        /// </summary>
        [Reactive]
        public partial double OverallUptimePercent { get; private set; }

        /// <summary>
        /// Gets the number of non-healthy check results in the last 24 hours
        /// </summary>
        [Reactive]
        public partial int IncidentsLast24Hours { get; private set; }

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
        public ReadOnlyObservableCollection<EndpointStatusViewModel> FilteredEndpoints { get; }

        /// <summary>
        /// Gets the collection of failed check results in the last 24 hours
        /// </summary>
        public ReadOnlyObservableCollection<IncidentItem> IncidentResults { get; }

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
        public ReactiveCommand<RxVoid, DashboardViewMode> ShowAllEndpointsCommand { get; private set; }

        /// <summary>
        /// Gets the command to filter to down endpoints only
        /// </summary>
        public ReactiveCommand<RxVoid, DashboardViewMode> ShowDownEndpointsCommand { get; private set; }

        /// <summary>
        /// Gets the command to switch to the incidents view
        /// </summary>
        public ReactiveCommand<RxVoid, DashboardViewMode> ShowIncidentsCommand { get; private set; }

        /// <summary>
        /// Gets the command to filter to active endpoints only
        /// </summary>
        public ReactiveCommand<RxVoid, EndpointActivityFilter> ShowActiveCommand { get; private set; }

        /// <summary>
        /// Gets the command to filter to inactive endpoints only
        /// </summary>
        public ReactiveCommand<RxVoid, EndpointActivityFilter> ShowInactiveCommand { get; private set; }

        /// <summary>
        /// Gets the command to show all endpoints regardless of activity status
        /// </summary>
        public ReactiveCommand<RxVoid, EndpointActivityFilter> ShowAllActivityCommand { get; private set; }

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            this.SelectedDetail?.Dispose();
            this.disposables.Dispose();
            this.refreshRequests.Dispose();
        }

        /// <summary>
        /// Builds the predicate that decides whether an endpoint is shown, given the view mode and the
        /// activity filter
        /// </summary>
        /// <param name="mode">
        /// The <see cref="DashboardViewMode"/>
        /// </param>
        /// <param name="activity">
        /// The <see cref="EndpointActivityFilter"/>
        /// </param>
        /// <returns>
        /// The predicate over <see cref="EndpointStatusViewModel"/>
        /// </returns>
        private static Func<EndpointStatusViewModel, bool> BuildFilter(DashboardViewMode mode, EndpointActivityFilter activity)
        {
            return e =>
            {
                var passesActivity = activity switch
                {
                    EndpointActivityFilter.ActiveOnly => e.IsActive,
                    EndpointActivityFilter.InactiveOnly => !e.IsActive,
                    _ => true
                };

                if (!passesActivity)
                {
                    return false;
                }

                return mode switch
                {
                    DashboardViewMode.DownOnly => !e.IsHealthy && e.StatusCode != 0,
                    _ => true
                };
            };
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
            var ascending = direction == ListSortDirection.Ascending;
            var ignoreCase = StringComparer.OrdinalIgnoreCase;

            return column switch
            {
                EndpointSortColumn.Url => OrderRowsBy(e => e.Url, ignoreCase, ascending),
                EndpointSortColumn.Status => OrderRowsBy(e => e.StatusCode, Comparer<int>.Default, ascending),
                EndpointSortColumn.ResponseTime => OrderRowsBy(e => e.ResponseTimeMs, Comparer<long>.Default, ascending),
                EndpointSortColumn.LastChecked => ascending
                    ? OrderedComparer<EndpointStatusViewModel>.OrderBy(e => e.LastChecked == null).ThenBy(e => e.LastChecked)
                    : OrderedComparer<EndpointStatusViewModel>.OrderBy(e => e.LastChecked == null).ThenByDescending(e => e.LastChecked),
                EndpointSortColumn.ErrorMessage => ascending
                    ? OrderedComparer<EndpointStatusViewModel>.OrderBy(e => string.IsNullOrEmpty(e.ErrorMessage)).ThenBy(e => e.ErrorMessage ?? string.Empty, ignoreCase)
                    : OrderedComparer<EndpointStatusViewModel>.OrderBy(e => string.IsNullOrEmpty(e.ErrorMessage)).ThenByDescending(e => e.ErrorMessage ?? string.Empty, ignoreCase),
                _ => OrderRowsBy(e => e.Name, ignoreCase, ascending),
            };
        }

        /// <summary>
        /// Builds an <see cref="IComparer{T}"/> that orders the endpoint rows by one value
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value
        /// </typeparam>
        /// <param name="selector">
        /// Selects the value of a row
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> that compares two values
        /// </param>
        /// <param name="ascending">
        /// true to order from low to high, false to order from high to low
        /// </param>
        /// <returns>
        /// An <see cref="IComparer{T}"/> over <see cref="EndpointStatusViewModel"/>
        /// </returns>
        private static IComparer<EndpointStatusViewModel> OrderRowsBy<TValue>(Func<EndpointStatusViewModel, TValue> selector, IComparer<TValue> comparer, bool ascending)
        {
            return ascending
                ? OrderedComparer<EndpointStatusViewModel>.OrderBy(selector, comparer)
                : OrderedComparer<EndpointStatusViewModel>.OrderByDescending(selector, comparer);
        }

        /// <summary>
        /// Rebuilds the incidents list from all endpoint histories (last 24h, non-2xx)
        /// </summary>
        private void RefreshIncidents()
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);

            var recentIncidents = this.endpoints
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

            this.incidents.Clear();

            foreach (var incident in recentIncidents)
            {
                this.incidents.Add(incident);
            }
        }

        /// <summary>
        /// Brings the endpoint status rows in line with the endpoints of the service: removes the rows of
        /// endpoints that no longer exist, updates the rows of existing endpoints and adds rows for new ones.
        /// Called on the UI thread
        /// </summary>
        /// <param name="incomingEndpoints">
        /// The endpoints as returned by the service
        /// </param>
        private void SyncEndpoints(IList<HealthEndPoint> incomingEndpoints)
        {
            this.logger.LogDebug("EndpointsObservable received {Count} endpoint(s)", incomingEndpoints.Count);

            var incomingIds = incomingEndpoints.Select(e => e.Identifier).ToHashSet();
            var staleRows = this.endpoints.Where(row => !incomingIds.Contains(row.Identifier)).ToList();

            foreach (var row in staleRows)
            {
                this.endpointRows.Remove(row.Identifier);
                this.endpoints.Remove(row);
            }

            var toBootstrap = new List<EndpointStatusViewModel>();

            foreach (var ep in incomingEndpoints)
            {
                if (this.endpointRows.TryGetValue(ep.Identifier, out var existing))
                {
                    existing.Name = ep.Name;
                    existing.Url = ep.Url;
                    existing.IsActive = ep.IsActive;
                }
                else
                {
                    var row = new EndpointStatusViewModel(ep.Identifier, ep.Name, ep.Url, ep.IsActive);
                    this.endpointRows.Add(row.Identifier, row);
                    this.endpoints.Add(row);
                    toBootstrap.Add(row);
                }
            }

            this.logger.LogDebug("Endpoints synced: {AddedCount} added, {RemovedCount} removed, {TotalCount} total", toBootstrap.Count, staleRows.Count, this.endpoints.Count);

            this.Refresh();

            if (toBootstrap.Count > 0)
            {
                _ = this.BootstrapHistoryAsync(toBootstrap);
            }
        }

        /// <summary>
        /// Requests a refresh when a property of an endpoint status row changes that the filter, the sort
        /// order or a summary value depends on
        /// </summary>
        /// <param name="sender">
        /// The <see cref="EndpointStatusViewModel"/> that changed
        /// </param>
        /// <param name="e">
        /// The <see cref="PropertyChangedEventArgs"/> with the name of the property
        /// </param>
        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null && RefreshingProperties.Contains(e.PropertyName))
            {
                this.RequestRefresh();
            }
        }

        /// <summary>
        /// Requests that <see cref="Refresh"/> runs on the UI thread. A request that is made while an
        /// earlier one has not started yet is merged into that one. Can be called from any thread
        /// </summary>
        private void RequestRefresh()
        {
            if (Interlocked.Exchange(ref this.refreshPending, 1) == 0)
            {
                this.refreshRequests.OnNext(RxVoid.Default);
            }
        }

        /// <summary>
        /// Computes the filtered and sorted endpoint rows and the values of the summary cards from the
        /// current rows. Called on the UI thread
        /// </summary>
        private void Refresh()
        {
            var filter = BuildFilter(this.ActiveViewMode, this.ActiveActivityFilter);
            var comparer = BuildComparer(this.SortColumn, this.SortDirection);

            var visibleRows = this.endpoints
                .Where(filter)
                .OrderBy(row => row, comparer)
                .ToList();

            this.filteredEndpoints.SyncWith(visibleRows);

            this.TotalEndpoints = this.endpoints.Count;
            this.EndpointsDown = this.endpoints.Count(e => !e.IsHealthy && e.StatusCode != 0);

            var withResults = this.endpoints.Where(e => e.ResponseTimeMs > 0).ToList();
            this.AverageResponseTimeMs = withResults.Count > 0 ? Math.Round(withResults.Average(e => e.ResponseTimeMs), 1) : 0;

            var cutoff = DateTime.UtcNow.AddHours(-24);
            var totalChecks = 0;
            var healthyChecks = 0;

            foreach (var result in this.endpoints.SelectMany(e => e.History).Where(r => r.Timestamp >= cutoff))
            {
                totalChecks++;

                if (result.IsHealthy())
                {
                    healthyChecks++;
                }
            }

            this.OverallUptimePercent = totalChecks > 0 ? Math.Round(100.0 * healthyChecks / totalChecks, 2) : 0.0;
            this.IncidentsLast24Hours = totalChecks - healthyChecks;
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
                try
                {
                    var alreadyBootstrapped = false;

                    await UiSequencer.RunAsync(() => alreadyBootstrapped = this.bootstrappedEndpoints.Contains(row.Identifier));

                    if (alreadyBootstrapped)
                    {
                        continue;
                    }

                    var results = await this.client.GetCheckResultsAsync(row.Identifier);

                    // The row and the set of bootstrapped endpoints are only touched on the UI thread
                    await UiSequencer.RunAsync(() =>
                    {
                        if (results.Count > 0)
                        {
                            row.AddCheckResults(results);
                            this.logger.LogDebug("Bootstrapped {Count} historical result(s) for {EndpointName}", results.Count, row.Name);
                        }

                        this.bootstrappedEndpoints.Add(row.Identifier);
                    });
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

                // The detail view model is only touched on the UI thread
                await UiSequencer.RunAsync(() =>
                {
                    detail.HourlySummaries = hourlyTask.Result as IReadOnlyList<UptimeSummary> ?? hourlyTask.Result.ToList();
                    detail.DailySummaries = dailyTask.Result as IReadOnlyList<UptimeSummary> ?? dailyTask.Result.ToList();
                });

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
