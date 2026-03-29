// -------------------------------------------------------------------------------------------------
//  <copyright file="MainWindow.axaml.cs">
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
    using System.Reactive.Disposables;
    using System.Reactive.Disposables.Fluent;

    using Argus.Health.Pulse.ViewModels;

    using ReactiveUI;
    using ReactiveUI.Avalonia;

    /// <summary>
    /// Main application window
    /// </summary>
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.BindCommand(this.ViewModel, vm => vm.GoToDashboardCommand, v => v.DashboardButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.GoToEndpointsCommand, v => v.EndpointsButton)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IsConnecting, v => v.ConnectingBorder.IsVisible)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IsConnected, v => v.ConnectedBorder.IsVisible)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IsConnectionDegraded, v => v.DegradedBorder.IsVisible)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IsConnectionError, v => v.ErrorBorder.IsVisible)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IsSyncRunning, v => v.SyncButton.Content,
                        isRunning => isRunning ? "Stop Sync" : "Start Sync")
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.ToggleSyncCommand, v => v.SyncButton)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.SyncProgress, v => v.SyncProgressBar.Value)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.CurrentView, v => v.ContentHost.ViewModel)
                    .DisposeWith(disposables);
            });
        }
    }
}
