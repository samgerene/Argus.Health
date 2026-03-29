// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointListView.axaml.cs">
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
    using System.Reactive.Disposables;
    using System.Reactive.Disposables.Fluent;

    using Argus.Health.Pulse.ViewModels;

    using ReactiveUI;
    using ReactiveUI.Avalonia;

    /// <summary>
    /// View for the endpoint CRUD list
    /// </summary>
    public partial class EndpointListView : ReactiveUserControl<EndpointListViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointListView"/> class
        /// </summary>
        public EndpointListView()
        {
            this.InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.BindCommand(this.ViewModel, vm => vm.AddCommand, v => v.AddButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.RefreshCommand, v => v.RefreshButton)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.ErrorMessage, v => v.ErrorMessageText.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.ErrorMessage, v => v.ErrorMessageText.IsVisible,
                        message => !string.IsNullOrEmpty(message))
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.HasEndpoints, v => v.EmptyStateText.IsVisible,
                        has => !has)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.Endpoints, v => v.EndpointsGrid.ItemsSource)
                    .DisposeWith(disposables);

                this.Bind(this.ViewModel, vm => vm.SelectedEndpoint, v => v.EndpointsGrid.SelectedItem)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.HasEndpoints, v => v.EndpointsGrid.IsVisible)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.IsDeleteConfirmationVisible, v => v.DeleteOverlay.IsVisible)
                    .DisposeWith(disposables);

                this.WhenAnyValue(v => v.ViewModel!.PendingDeleteEndpoint)
                    .Subscribe(endpoint => this.PendingDeleteNameRun.Text = endpoint?.Name ?? string.Empty)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.CancelDeleteCommand, v => v.CancelDeleteButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.ConfirmDeleteCommand, v => v.ConfirmDeleteButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
