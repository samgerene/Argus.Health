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

    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Services;

    using ReactiveUI;
    using ReactiveUI.Fody.Helpers;
    
    /// <summary>
    /// Shell view model with navigation, notification queue, and tray commands
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly HealthEndPointClient client;
        private readonly IEndpointSyncService syncService;
        private readonly IHealthCheckService healthCheckService;
        private readonly CompositeDisposable disposables = new();
        private DashboardViewModel? dashboardViewModel;
        private EndpointListViewModel? endpointListViewModel;

        public MainWindowViewModel(
            HealthEndPointClient client,
            IEndpointSyncService syncService,
            IHealthCheckService healthCheckService)
        {
            this.client = client;
            this.syncService = syncService;
            this.healthCheckService = healthCheckService;

            GoToDashboardCommand = ReactiveCommand.Create(NavigateToDashboard);
            GoToEndpointsCommand = ReactiveCommand.Create(NavigateToEndpoints);
            DismissNotificationCommand = ReactiveCommand.Create<NotificationItem>(DismissNotification);
            ToggleWindowCommand = ReactiveCommand.Create(() => { ToggleWindowRequested?.Invoke(this, EventArgs.Empty); });
            ShowWindowCommand = ReactiveCommand.Create(() => { ShowWindowRequested?.Invoke(this, EventArgs.Empty); });
            ExitCommand = ReactiveCommand.Create(() => { ExitRequested?.Invoke(this, EventArgs.Empty); });

            // subscribe to sync service to feed health check service
            var syncSubscription = syncService.EndpointsObservable
                .Subscribe(endpoints => healthCheckService.UpdateEndpoints(endpoints));
            disposables.Add(syncSubscription);

            // subscribe to failures for toast notifications
            var failureSubscription = healthCheckService.FailureObservable
                .ObserveOn(RxApp.MainThreadScheduler)
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
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => Notifications.Remove(notification));
                });

            disposables.Add(failureSubscription);

            // subscribe to connection errors for status indicator
            var connectionSubscription = syncService.ConnectionErrorObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(hasError => IsConnectionError = hasError);
            disposables.Add(connectionSubscription);

            // countdown timer: 100 ticks over the poll interval
            var countdownSubscription = Observable.Interval(TimeSpan.FromMilliseconds(100))
                .ObserveOn(RxApp.MainThreadScheduler)
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
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => SyncProgress = 100);
            disposables.Add(resetSubscription);

            // start with dashboard
            NavigateToDashboard();
            syncService.Start();
        }

        [Reactive]
        public ViewModelBase? CurrentView { get; set; }

        [Reactive]
        public bool IsConnectionError { get; set; }

        [Reactive]
        public double SyncProgress { get; set; } = 100;

        public ObservableCollection<NotificationItem> Notifications { get; } = new();

        public ReactiveCommand<Unit, Unit> GoToDashboardCommand { get; }

        public ReactiveCommand<Unit, Unit> GoToEndpointsCommand { get; }

        public ReactiveCommand<NotificationItem, Unit> DismissNotificationCommand { get; }

        public ICommand ToggleWindowCommand { get; }

        public ICommand ShowWindowCommand { get; }

        public ICommand ExitCommand { get; }

        public event EventHandler? ToggleWindowRequested;

        public event EventHandler? ShowWindowRequested;

        public event EventHandler? ExitRequested;

        private void NavigateToDashboard()
        {
            dashboardViewModel ??= new DashboardViewModel(syncService, healthCheckService);
            CurrentView = dashboardViewModel;
        }

        private void NavigateToEndpoints()
        {
            endpointListViewModel = new EndpointListViewModel(client, NavigateFromEditor);
            CurrentView = endpointListViewModel;
        }

        private void NavigateFromEditor(ViewModelBase? viewModel)
        {
            if (viewModel is EndpointEditorViewModel)
            {
                CurrentView = viewModel;
            }
            else
            {
                // navigate back to endpoint list (refresh it)
                NavigateToEndpoints();
            }
        }

        private void DismissNotification(NotificationItem item)
        {
            Notifications.Remove(item);
        }

        public void Dispose()
        {
            disposables.Dispose();
            dashboardViewModel?.Dispose();
        }
    }
}
