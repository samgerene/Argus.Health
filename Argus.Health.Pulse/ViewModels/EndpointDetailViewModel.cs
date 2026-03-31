// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointDetailViewModel.cs">
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
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Argus.Health.Common.Model;

    using ReactiveUI;
    using ReactiveUI.SourceGenerators;

    /// <summary>
    /// View model for the endpoint detail panel shown when a dashboard row is selected
    /// </summary>
    public partial class EndpointDetailViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// Disposable container for Rx subscriptions
        /// </summary>
        private readonly CompositeDisposable disposables = new();

        /// <summary>
        /// Helper for <see cref="FilteredResults"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<IReadOnlyList<HealthEndPointCheckResult>> filteredResultsHelper;

        /// <summary>
        /// Helper for <see cref="UptimePercent"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<double> uptimePercentHelper;

        /// <summary>
        /// Helper for <see cref="AverageResponseTimeMs"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<double> averageResponseTimeMsHelper;

        /// <summary>
        /// Helper for <see cref="P95ResponseTimeMs"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<long> p95ResponseTimeMsHelper;

        /// <summary>
        /// Helper for <see cref="MaxResponseTimeMs"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<long> maxResponseTimeMsHelper;

        /// <summary>
        /// Helper for <see cref="TotalChecks"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<int> totalChecksHelper;

        /// <summary>
        /// Helper for <see cref="TotalFailures"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<int> totalFailuresHelper;

        /// <summary>
        /// Helper for <see cref="IsStatusBarView"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> isStatusBarViewHelper;

        /// <summary>
        /// Helper for <see cref="IsHeatmapView"/>
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> isHeatmapViewHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointDetailViewModel"/> class
        /// </summary>
        /// <param name="endpoint">The <see cref="EndpointStatusViewModel"/> to show details for</param>
        public EndpointDetailViewModel(EndpointStatusViewModel endpoint)
        {
            this.Endpoint = endpoint;
            this.SelectedTimeRange = TimeRange.Last24Hours;

            var recalculate = this.WhenAnyValue(
                    x => x.SelectedTimeRange,
                    x => x.Endpoint.ResponseTimeMs)
                .Select(_ => this.ComputeFilteredResults())
                .Replay(1)
                .RefCount();

            this.filteredResultsHelper = recalculate
                .ToProperty(this, x => x.FilteredResults);

            this.uptimePercentHelper = recalculate
                .Select(EndpointDetailViewModel.ComputeUptimePercent)
                .ToProperty(this, x => x.UptimePercent);

            this.averageResponseTimeMsHelper = recalculate
                .Select(EndpointDetailViewModel.ComputeAverageResponseTime)
                .ToProperty(this, x => x.AverageResponseTimeMs);

            this.p95ResponseTimeMsHelper = recalculate
                .Select(EndpointDetailViewModel.ComputeP95ResponseTime)
                .ToProperty(this, x => x.P95ResponseTimeMs);

            this.maxResponseTimeMsHelper = recalculate
                .Select(EndpointDetailViewModel.ComputeMaxResponseTime)
                .ToProperty(this, x => x.MaxResponseTimeMs);

            this.totalChecksHelper = recalculate
                .Select(r => r.Count)
                .ToProperty(this, x => x.TotalChecks);

            this.totalFailuresHelper = recalculate
                .Select(r => r.Count(x => !x.IsHealthy()))
                .ToProperty(this, x => x.TotalFailures);

            this.SelectLastHourCommand = ReactiveCommand.Create(() => this.SelectedTimeRange = TimeRange.LastHour);
            this.SelectLast6HoursCommand = ReactiveCommand.Create(() => this.SelectedTimeRange = TimeRange.Last6Hours);
            this.SelectLast24HoursCommand = ReactiveCommand.Create(() => this.SelectedTimeRange = TimeRange.Last24Hours);
            this.SelectLast7DaysCommand = ReactiveCommand.Create(() => this.SelectedTimeRange = TimeRange.Last7Days);

            this.SelectStatusBarCommand = ReactiveCommand.Create(() => this.SelectedUptimeViewMode = UptimeViewMode.StatusBar);
            this.SelectHeatmapCommand = ReactiveCommand.Create(() => this.SelectedUptimeViewMode = UptimeViewMode.Heatmap);

            this.isStatusBarViewHelper = this.WhenAnyValue(x => x.SelectedUptimeViewMode)
                .Select(mode => mode == UptimeViewMode.StatusBar)
                .ToProperty(this, x => x.IsStatusBarView);

            this.isHeatmapViewHelper = this.WhenAnyValue(x => x.SelectedUptimeViewMode)
                .Select(mode => mode == UptimeViewMode.Heatmap)
                .ToProperty(this, x => x.IsHeatmapView);

            this.disposables.Add(this.isStatusBarViewHelper);
            this.disposables.Add(this.isHeatmapViewHelper);

            this.disposables.Add(this.filteredResultsHelper);
            this.disposables.Add(this.uptimePercentHelper);
            this.disposables.Add(this.averageResponseTimeMsHelper);
            this.disposables.Add(this.p95ResponseTimeMsHelper);
            this.disposables.Add(this.maxResponseTimeMsHelper);
            this.disposables.Add(this.totalChecksHelper);
            this.disposables.Add(this.totalFailuresHelper);
        }

        /// <summary>
        /// Gets the source endpoint
        /// </summary>
        public EndpointStatusViewModel Endpoint { get; }

        /// <summary>
        /// Gets or sets the selected time range for filtering results
        /// </summary>
        [Reactive]
        public partial TimeRange SelectedTimeRange { get; set; }

        /// <summary>
        /// Gets the check results filtered by the selected time range
        /// </summary>
        public IReadOnlyList<HealthEndPointCheckResult> FilteredResults => this.filteredResultsHelper.Value;

        /// <summary>
        /// Gets the uptime percentage for the selected time range
        /// </summary>
        public double UptimePercent => this.uptimePercentHelper.Value;

        /// <summary>
        /// Gets the average response time in milliseconds for the selected time range
        /// </summary>
        public double AverageResponseTimeMs => this.averageResponseTimeMsHelper.Value;

        /// <summary>
        /// Gets the 95th percentile response time in milliseconds for the selected time range
        /// </summary>
        public long P95ResponseTimeMs => this.p95ResponseTimeMsHelper.Value;

        /// <summary>
        /// Gets the maximum response time in milliseconds for the selected time range
        /// </summary>
        public long MaxResponseTimeMs => this.maxResponseTimeMsHelper.Value;

        /// <summary>
        /// Gets the total number of checks in the selected time range
        /// </summary>
        public int TotalChecks => this.totalChecksHelper.Value;

        /// <summary>
        /// Gets the total number of failed checks in the selected time range
        /// </summary>
        public int TotalFailures => this.totalFailuresHelper.Value;

        /// <summary>
        /// Gets the last error message from the endpoint
        /// </summary>
        public string? LastError => this.Endpoint.ErrorMessage;

        /// <summary>
        /// Gets the current status display text
        /// </summary>
        public string CurrentStatus => this.Endpoint.StatusDisplay;

        /// <summary>
        /// Gets the command to select the last hour time range
        /// </summary>
        public ReactiveCommand<Unit, TimeRange> SelectLastHourCommand { get; }

        /// <summary>
        /// Gets the command to select the last 6 hours time range
        /// </summary>
        public ReactiveCommand<Unit, TimeRange> SelectLast6HoursCommand { get; }

        /// <summary>
        /// Gets the command to select the last 24 hours time range
        /// </summary>
        public ReactiveCommand<Unit, TimeRange> SelectLast24HoursCommand { get; }

        /// <summary>
        /// Gets the command to select the last 7 days time range
        /// </summary>
        public ReactiveCommand<Unit, TimeRange> SelectLast7DaysCommand { get; }

        /// <summary>
        /// Gets the command to select the status bar uptime view
        /// </summary>
        public ReactiveCommand<Unit, UptimeViewMode> SelectStatusBarCommand { get; }

        /// <summary>
        /// Gets the command to select the heatmap uptime view
        /// </summary>
        public ReactiveCommand<Unit, UptimeViewMode> SelectHeatmapCommand { get; }

        /// <summary>
        /// Gets or sets the selected uptime visualization mode
        /// </summary>
        [Reactive]
        public partial UptimeViewMode SelectedUptimeViewMode { get; set; }

        /// <summary>
        /// Gets or sets the hourly uptime summaries for the status bar visualization
        /// </summary>
        [Reactive]
        public partial IReadOnlyList<UptimeSummary>? HourlySummaries { get; set; }

        /// <summary>
        /// Gets or sets the daily uptime summaries for the heatmap visualization
        /// </summary>
        [Reactive]
        public partial IReadOnlyList<UptimeSummary>? DailySummaries { get; set; }

        /// <summary>
        /// Gets a value indicating whether the status bar view is active
        /// </summary>
        public bool IsStatusBarView => this.isStatusBarViewHelper.Value;

        /// <summary>
        /// Gets a value indicating whether the heatmap view is active
        /// </summary>
        public bool IsHeatmapView => this.isHeatmapViewHelper.Value;

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            this.disposables.Dispose();
        }

        /// <summary>
        /// Computes the filtered results based on the selected time range
        /// </summary>
        /// <returns>A list of check results within the selected time range</returns>
        private IReadOnlyList<HealthEndPointCheckResult> ComputeFilteredResults()
        {
            var cutoff = DateTime.UtcNow - this.SelectedTimeRange.ToTimeSpan();

            return this.Endpoint.History
                .Where(r => r.Timestamp >= cutoff)
                .OrderBy(r => r.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Computes the uptime percentage from a list of check results
        /// </summary>
        /// <param name="results">The check results to compute uptime from</param>
        /// <returns>The uptime percentage (0-100)</returns>
        private static double ComputeUptimePercent(IReadOnlyList<HealthEndPointCheckResult> results)
        {
            if (results.Count == 0)
            {
                return 0;
            }

            var healthy = results.Count(r => r.IsHealthy());
            return Math.Round(100.0 * healthy / results.Count, 2);
        }

        /// <summary>
        /// Computes the average response time from a list of check results
        /// </summary>
        /// <param name="results">The check results</param>
        /// <returns>The average response time in milliseconds</returns>
        private static double ComputeAverageResponseTime(IReadOnlyList<HealthEndPointCheckResult> results)
        {
            if (results.Count == 0)
            {
                return 0;
            }

            return Math.Round(results.Average(r => r.ResponseTimeMs), 1);
        }

        /// <summary>
        /// Computes the 95th percentile response time from a list of check results
        /// </summary>
        /// <param name="results">The check results</param>
        /// <returns>The P95 response time in milliseconds</returns>
        private static long ComputeP95ResponseTime(IReadOnlyList<HealthEndPointCheckResult> results)
        {
            if (results.Count == 0)
            {
                return 0;
            }

            var sorted = results.Select(r => r.ResponseTimeMs).OrderBy(t => t).ToArray();
            var index = (int)Math.Ceiling(0.95 * sorted.Length) - 1;
            return sorted[Math.Max(0, index)];
        }

        /// <summary>
        /// Computes the maximum response time from a list of check results
        /// </summary>
        /// <param name="results">The check results</param>
        /// <returns>The maximum response time in milliseconds</returns>
        private static long ComputeMaxResponseTime(IReadOnlyList<HealthEndPointCheckResult> results)
        {
            if (results.Count == 0)
            {
                return 0;
            }

            return results.Max(r => r.ResponseTimeMs);
        }
    }
}
