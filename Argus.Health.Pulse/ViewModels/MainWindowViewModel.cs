// -------------------------------------------------------------------------------------------------
//  <copyright file="MainWindowViewModel.cs">
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
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Services;

    using Microsoft.Extensions.Logging;

    using ReactiveUI.Avalonia;

    using ReactiveUI;
    using ReactiveUI.Fody.Helpers;

    /// <summary>
    /// Shell view model with navigation, notification queue, and tray commands
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// The <see cref="HealthEndPointClient"/> used for endpoint CRUD operations
        /// </summary>
        private readonly HealthEndPointClient client;

        /// <summary>
        /// The <see cref="IEndpointSyncService"/> used to poll endpoints
        /// </summary>
        private readonly IEndpointSyncService syncService;

        /// <summary>
        /// The <see cref="IHealthCheckService"/> used to monitor endpoint health
        /// </summary>
        private readonly IHealthCheckService healthCheckService;

        /// <summary>
        /// The <see cref="ILoggerFactory"/> used to create loggers for child view models
        /// </summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// The <see cref="ILogger{MainWindowViewModel}"/> used for logging
        /// </summary>
        private readonly ILogger<MainWindowViewModel> logger;

        /// <summary>
        /// Disposable container for Rx subscriptions
        /// </summary>
        private readonly CompositeDisposable disposables = new();

        /// <summary>
        /// Tracks the number of consecutive connection failures
        /// </summary>
        private int consecutiveFailures;

        /// <summary>
        /// Cached dashboard view model instance
        /// </summary>
        private DashboardViewModel? dashboardViewModel;

        /// <summary>
        /// Current endpoint list view model instance
        /// </summary>
        private EndpointListViewModel? endpointListViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class
        /// </summary>
        /// <param name="client">
        /// The <see cref="HealthEndPointClient"/> used for endpoint CRUD operations
        /// </param>
        /// <param name="syncService">
        /// The <see cref="IEndpointSyncService"/> used to poll endpoints
        /// </param>
        /// <param name="healthCheckService">
        /// The <see cref="IHealthCheckService"/> used to monitor endpoint health
        /// </param>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to create loggers for child view models
        /// </param>
        public MainWindowViewModel(
            HealthEndPointClient client,
            IEndpointSyncService syncService,
            IHealthCheckService healthCheckService,
            ILoggerFactory loggerFactory)
        {
            this.client = client;
            this.syncService = syncService;
            this.healthCheckService = healthCheckService;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<MainWindowViewModel>();

            this.GoToDashboardCommand = ReactiveCommand.Create(this.NavigateToDashboard);
            this.GoToEndpointsCommand = ReactiveCommand.Create(this.NavigateToEndpoints);
            this.DismissNotificationCommand = ReactiveCommand.Create<NotificationItem>(this.DismissNotification);
            this.ExitCommand = ReactiveCommand.Create(() => { this.ExitRequested?.Invoke(this, EventArgs.Empty); });
            this.ToggleWindowCommand = ReactiveCommand.Create(() => { this.ToggleWindowRequested?.Invoke(this, EventArgs.Empty); });
            this.ShowWindowCommand = ReactiveCommand.Create(() => { this.ShowWindowRequested?.Invoke(this, EventArgs.Empty); });

            this.ToggleSyncCommand = ReactiveCommand.Create(() =>
            {
                if (this.IsSyncRunning)
                {
                    this.syncService.Stop();
                    this.healthCheckService.UpdateEndpoints(Array.Empty<HealthEndPoint>());
                }
                else
                {
                    this.IsConnecting = true;
                    this.syncService.Start();
                }
            });

            // subscribe to sync service to feed health check service
            var syncSubscription = syncService.EndpointsObservable
                .Subscribe(endpoints => healthCheckService.UpdateEndpoints(endpoints));
            disposables.Add(syncSubscription);

            // subscribe to failures for toast notifications
            var failureSubscription = healthCheckService.FailureObservable
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(result =>
                {
                    var notification = new NotificationItem
                    {
                        EndpointName = $"Endpoint {result.HealthEndPoint}",
                        StatusCode = result.StatusCode,
                        ErrorMessage = result.ErrorMessage,
                        Timestamp = result.Timestamp
                    };

                    // try to find endpoint name from dashboard
                    if (dashboardViewModel != null)
                    {
                        var row = dashboardViewModel.Endpoints.FirstOrDefault(e => e.Identifier == result.HealthEndPoint);

                        if (row != null)
                        {
                            notification.EndpointName = row.Name;
                        }
                    }

                    Notifications.Add(notification);

                    // auto-remove after 5 seconds
                    Observable.Timer(TimeSpan.FromSeconds(5))
                        .ObserveOn(AvaloniaScheduler.Instance)
                        .Subscribe(_ => Notifications.Remove(notification));
                });

            disposables.Add(failureSubscription);

            // subscribe to connection errors for status indicator
            var connectionSubscription = this.syncService.ConnectionErrorObservable
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(hasError =>
                {
                    if (!hasError)
                    {
                        this.consecutiveFailures = 0;
                        this.IsConnecting = false;
                        this.IsConnectionDegraded = false;
                        this.IsConnectionError = false;
                        this.IsConnected = this.IsSyncRunning;
                    }
                    else
                    {
                        this.consecutiveFailures++;
                        this.IsConnecting = false;
                        this.IsConnected = false;

                        if (this.consecutiveFailures >= 2)
                        {
                            this.IsConnectionDegraded = false;
                            this.IsConnectionError = true;
                            this.SyncProgress = 0;
                            this.syncService.Stop();
                            this.healthCheckService.UpdateEndpoints(Array.Empty<HealthEndPoint>());
                        }
                        else
                        {
                            this.IsConnectionDegraded = true;
                        }
                    }
                });
            this.disposables.Add(connectionSubscription);

            // subscribe to running state
            var runningSubscription = this.syncService.IsRunningObservable
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(isRunning =>
                {
                    this.IsSyncRunning = isRunning;

                    if (!isRunning)
                    {
                        this.IsConnecting = false;
                        this.IsConnected = false;
                        this.IsConnectionDegraded = false;
                    }
                });
            this.disposables.Add(runningSubscription);

            // countdown timer: 100 ticks over the poll interval
            var countdownSubscription = Observable.Interval(TimeSpan.FromMilliseconds(100))
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(_ =>
                {
                    if (SyncProgress > 0)
                    {
                        SyncProgress -= 1;
                    }
                });
            disposables.Add(countdownSubscription);

            // reset progress bar on every poll result
            var resetSubscription = syncService.EndpointsObservable
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(_ =>
                {
                    if (!this.IsConnectionError)
                    {
                        this.SyncProgress = 100;
                    }
                });
            disposables.Add(resetSubscription);

            // start with dashboard
            NavigateToDashboard();
            this.IsConnecting = true;
            syncService.Start();
        }

        /// <summary>
        /// Gets or sets the currently displayed child view model
        /// </summary>
        [Reactive]
        public ViewModelBase? CurrentView { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the service is in the initial connecting state
        /// </summary>
        [Reactive]
        public bool IsConnecting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the service connection has an error
        /// </summary>
        [Reactive]
        public bool IsConnectionError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection is degraded after a single failure
        /// </summary>
        [Reactive]
        public bool IsConnectionDegraded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sync service is currently running
        /// </summary>
        [Reactive]
        public bool IsSyncRunning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the service is connected and sync is healthy
        /// </summary>
        [Reactive]
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the sync progress countdown value (0–100)
        /// </summary>
        [Reactive]
        public double SyncProgress { get; set; } = 100;

        /// <summary>
        /// Gets the collection of active toast notifications
        /// </summary>
        public ObservableCollection<NotificationItem> Notifications { get; } = new();

        /// <summary>
        /// Gets the command to navigate to the dashboard view
        /// </summary>
        public ReactiveCommand<Unit, Unit> GoToDashboardCommand { get; }

        /// <summary>
        /// Gets the command to navigate to the endpoints list view
        /// </summary>
        public ReactiveCommand<Unit, Unit> GoToEndpointsCommand { get; }

        /// <summary>
        /// Gets the command to toggle sync polling on or off
        /// </summary>
        public ReactiveCommand<Unit, Unit> ToggleSyncCommand { get; }

        /// <summary>
        /// Gets the command to dismiss a notification
        /// </summary>
        public ReactiveCommand<NotificationItem, Unit> DismissNotificationCommand { get; }

        /// <summary>
        /// Gets the command to exit the application
        /// </summary>
        public ICommand ExitCommand { get; }

        /// <summary>
        /// Gets the command to toggle the main window visibility from the tray icon
        /// </summary>
        public ICommand ToggleWindowCommand { get; }

        /// <summary>
        /// Gets the command to show the main window from the tray context menu
        /// </summary>
        public ICommand ShowWindowCommand { get; }

        /// <summary>
        /// Raised when the application should exit
        /// </summary>
        public event EventHandler? ExitRequested;

        /// <summary>
        /// Raised when the main window visibility should be toggled
        /// </summary>
        public event EventHandler? ToggleWindowRequested;

        /// <summary>
        /// Raised when the main window should be shown
        /// </summary>
        public event EventHandler? ShowWindowRequested;

        /// <summary>
        /// Navigates to the dashboard view, creating it on first use
        /// </summary>
        private void NavigateToDashboard()
        {
            logger.LogDebug("Navigating to dashboard");
            dashboardViewModel ??= new DashboardViewModel(syncService, healthCheckService, loggerFactory.CreateLogger<DashboardViewModel>());
            CurrentView = dashboardViewModel;
        }

        /// <summary>
        /// Navigates to the endpoints list view
        /// </summary>
        private void NavigateToEndpoints()
        {
            logger.LogDebug("Navigating to endpoints list");
            endpointListViewModel = new EndpointListViewModel(client, NavigateFromEditor, loggerFactory.CreateLogger<EndpointListViewModel>(), loggerFactory);
            CurrentView = endpointListViewModel;
        }

        /// <summary>
        /// Handles navigation from the editor, showing the editor or returning to the list
        /// </summary>
        /// <param name="viewModel">
        /// The view model to navigate to, or null to return to the endpoint list
        /// </param>
        private void NavigateFromEditor(ViewModelBase? viewModel)
        {
            if (viewModel is EndpointEditorViewModel)
            {
                logger.LogDebug("Navigating to endpoint editor");
                CurrentView = viewModel;
            }
            else
            {
                logger.LogDebug("Navigating back to endpoint list");
                // navigate back to endpoint list (refresh it)
                NavigateToEndpoints();
            }
        }

        /// <summary>
        /// Removes the specified notification from the collection
        /// </summary>
        /// <param name="item">
        /// The <see cref="NotificationItem"/> to dismiss
        /// </param>
        private void DismissNotification(NotificationItem item)
        {
            logger.LogDebug("Dismissing notification {NotificationId}", item.Id);
            Notifications.Remove(item);
        }

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        public void Dispose()
        {
            disposables.Dispose();
            dashboardViewModel?.Dispose();
        }
    }
}
