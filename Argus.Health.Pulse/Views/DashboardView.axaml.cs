// -------------------------------------------------------------------------------------------------
//  <copyright file="DashboardView.axaml.cs">
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

namespace Argus.Health.Pulse.Views
{
    using System;
    using System.Globalization;
    using System.Reactive.Disposables;
    using System.Reactive.Disposables.Fluent;
    using System.Reactive.Linq;

    using Argus.Health.Pulse.Controls;
    using Argus.Health.Pulse.ViewModels;

    using Avalonia;
    using Avalonia.Controls;

    using ReactiveUI;
    using ReactiveUI.Avalonia;

    /// <summary>
    /// Dashboard view displaying real-time endpoint health status
    /// </summary>
    public partial class DashboardView : ReactiveUserControl<DashboardViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardView"/> class
        /// </summary>
        public DashboardView()
        {
            this.InitializeComponent();

            this.WhenActivated(disposables =>
            {
                // Summary cards
                this.OneWayBind(this.ViewModel, vm => vm.TotalEndpoints, v => v.TotalEndpointsText.Text,
                        value => value.ToString(CultureInfo.CurrentCulture))
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.OverallUptimePercent, v => v.UptimePercentText.Text,
                        value => $"{value:F1}%")
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.AverageResponseTimeMs, v => v.AvgResponseTimeText.Text,
                        value => $"{value:F0}ms")
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.EndpointsDown, v => v.EndpointsDownText.Text,
                        value => value.ToString(CultureInfo.CurrentCulture))
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IncidentsLast24Hours, v => v.IncidentsText.Text,
                        value => value.ToString(CultureInfo.CurrentCulture))
                    .DisposeWith(disposables);

                // Card commands
                this.BindCommand(this.ViewModel, vm => vm.ShowDownEndpointsCommand, v => v.DownCardButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.ShowIncidentsCommand, v => v.IncidentsCardButton)
                    .DisposeWith(disposables);

                // Card active highlight
                this.OneWayBind(this.ViewModel, vm => vm.ActiveViewMode, v => v.DownCardBorder.BorderThickness,
                        mode => mode == DashboardViewMode.DownOnly ? new Thickness(2) : new Thickness(0))
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.ActiveViewMode, v => v.DownCardBorder.BorderBrush,
                        mode => mode == DashboardViewMode.DownOnly ? DashboardColors.Red : null)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.ActiveViewMode, v => v.IncidentsCardBorder.BorderThickness,
                        mode => mode == DashboardViewMode.Incidents ? new Thickness(2) : new Thickness(0))
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.ActiveViewMode, v => v.IncidentsCardBorder.BorderBrush,
                        mode => mode == DashboardViewMode.Incidents ? DashboardColors.Amber : null)
                    .DisposeWith(disposables);

                // Toggle buttons
                this.BindCommand(this.ViewModel, vm => vm.ShowAllEndpointsCommand, v => v.ShowEndpointsButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.ShowIncidentsCommand, v => v.ShowIncidentsButton)
                    .DisposeWith(disposables);

                // Activity filter commands
                this.BindCommand(this.ViewModel, vm => vm.ShowActiveCommand, v => v.ShowActiveButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.ShowInactiveCommand, v => v.ShowInactiveButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.ShowAllActivityCommand, v => v.ShowAllActivityButton)
                    .DisposeWith(disposables);

                // View toggle button highlights
                this.WhenAnyValue(v => v.ViewModel!.ActiveViewMode)
                    .Subscribe(mode =>
                    {
                        this.HighlightButton(this.ShowEndpointsButton, mode != DashboardViewMode.Incidents);
                        this.HighlightButton(this.ShowIncidentsButton, mode == DashboardViewMode.Incidents);
                    })
                    .DisposeWith(disposables);

                // Activity filter button highlights
                this.WhenAnyValue(v => v.ViewModel!.ActiveActivityFilter)
                    .Subscribe(filter =>
                    {
                        this.HighlightButton(this.ShowActiveButton, filter == EndpointActivityFilter.ActiveOnly);
                        this.HighlightButton(this.ShowInactiveButton, filter == EndpointActivityFilter.InactiveOnly);
                        this.HighlightButton(this.ShowAllActivityButton, filter == EndpointActivityFilter.All);
                    })
                    .DisposeWith(disposables);

                // Grid visibility
                this.OneWayBind(this.ViewModel, vm => vm.IsEndpointsView, v => v.EndpointsGrid.IsVisible)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IsIncidentsView, v => v.IncidentsGrid.IsVisible)
                    .DisposeWith(disposables);

                // Detail panel visibility
                this.OneWayBind(this.ViewModel, vm => vm.SelectedDetail, v => v.DetailPanel.IsVisible,
                        detail => detail != null)
                    .DisposeWith(disposables);

                // Detail panel bindings: set chart data, uptime data, stats, and commands
                this.WhenAnyValue(v => v.ViewModel!.SelectedDetail)
                    .Where(detail => detail != null)
                    .Subscribe(detail =>
                    {
                        this.ResponseTimeChart.Data = detail!.FilteredResults;
                        this.UpdateUptimeControls(detail);
                        this.UpdateDetailStats(detail);
                        this.LastHourButton.Command = detail.SelectLastHourCommand;
                        this.Last6HoursButton.Command = detail.SelectLast6HoursCommand;
                        this.Last24HoursButton.Command = detail.SelectLast24HoursCommand;
                        this.Last7DaysButton.Command = detail.SelectLast7DaysCommand;
                        this.StatusBarViewButton.Command = detail.SelectStatusBarCommand;
                        this.HeatmapViewButton.Command = detail.SelectHeatmapCommand;
                    })
                    .DisposeWith(disposables);

                // Refresh response time chart and stats when filtered results change
                this.WhenAnyValue(v => v.ViewModel!.SelectedDetail!.FilteredResults)
                    .Subscribe(results =>
                    {
                        this.ResponseTimeChart.Data = results;

                        var detail = this.ViewModel?.SelectedDetail;

                        if (detail != null)
                        {
                            this.UpdateDetailStats(detail);
                        }
                    })
                    .DisposeWith(disposables);

                // Refresh uptime controls when summary data arrives
                this.WhenAnyValue(v => v.ViewModel!.SelectedDetail!.HourlySummaries)
                    .Subscribe(_ =>
                    {
                        var detail = this.ViewModel?.SelectedDetail;

                        if (detail != null)
                        {
                            this.UpdateUptimeControls(detail);
                        }
                    })
                    .DisposeWith(disposables);

                // Toggle uptime visualization visibility
                this.WhenAnyValue(v => v.ViewModel!.SelectedDetail!.IsStatusBarView)
                    .Subscribe(isStatusBar =>
                    {
                        this.UptimeStatusBar.IsVisible = isStatusBar;
                        this.UptimeHeatmap.IsVisible = !isStatusBar;
                    })
                    .DisposeWith(disposables);

                // Time range button highlights
                this.WhenAnyValue(v => v.ViewModel!.SelectedDetail!.SelectedTimeRange)
                    .Subscribe(range =>
                    {
                        this.HighlightButton(this.LastHourButton, range == TimeRange.LastHour);
                        this.HighlightButton(this.Last6HoursButton, range == TimeRange.Last6Hours);
                        this.HighlightButton(this.Last24HoursButton, range == TimeRange.Last24Hours);
                        this.HighlightButton(this.Last7DaysButton, range == TimeRange.Last7Days);
                    })
                    .DisposeWith(disposables);

                // Uptime view button highlights
                this.WhenAnyValue(v => v.ViewModel!.SelectedDetail!.SelectedUptimeViewMode)
                    .Subscribe(mode =>
                    {
                        this.HighlightButton(this.StatusBarViewButton, mode == UptimeViewMode.StatusBar);
                        this.HighlightButton(this.HeatmapViewButton, mode == UptimeViewMode.Heatmap);
                    })
                    .DisposeWith(disposables);
            });
        }
        /// <summary>
        /// Updates the uptime visualization controls with data from the detail view model
        /// </summary>
        /// <param name="detail">The endpoint detail view model</param>
        private void UpdateUptimeControls(EndpointDetailViewModel detail)
        {
            this.UptimeStatusBar.Data = detail.HourlySummaries;
            this.UptimeHeatmap.Data = detail.DailySummaries;
            this.UptimeStatusBar.IsVisible = detail.IsStatusBarView;
            this.UptimeHeatmap.IsVisible = detail.IsHeatmapView;
        }

        /// <summary>
        /// Updates the detail panel statistics text blocks from the given view model
        /// </summary>
        /// <param name="detail">The endpoint detail view model</param>
        private void UpdateDetailStats(EndpointDetailViewModel detail)
        {
            this.CurrentStatusText.Text = detail.CurrentStatus;
            this.DetailUptimeText.Text = $"{detail.UptimePercent:F1}%";
            this.DetailAvgText.Text = $"{detail.AverageResponseTimeMs:F0}ms";
            this.DetailP95Text.Text = $"{detail.P95ResponseTimeMs}ms";
            this.DetailMaxText.Text = $"{detail.MaxResponseTimeMs}ms";
            this.DetailTotalChecksText.Text = detail.TotalChecks.ToString(CultureInfo.CurrentCulture);
            this.DetailFailuresText.Text = detail.TotalFailures.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Sets the foreground color of a button to indicate whether it is the active selection
        /// </summary>
        /// <param name="button">The button to highlight</param>
        /// <param name="isActive">Whether the button represents the active selection</param>
        private void HighlightButton(Button button, bool isActive)
        {
            button.Foreground = isActive ? DashboardColors.Teal : null;
        }
    }
}
