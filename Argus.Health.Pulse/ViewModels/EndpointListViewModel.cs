// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointListViewModel.cs">
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
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Argus.Health.Pulse.Client;
    using Argus.Health.Common.Model;

    using Microsoft.Extensions.Logging;

    using ReactiveUI.Avalonia;

    using ReactiveUI;
    using ReactiveUI.Fody.Helpers;

    /// <summary>
    /// Endpoint CRUD list view model
    /// </summary>
    public class EndpointListViewModel : ViewModelBase
    {
        /// <summary>
        /// The <see cref="ILogger{EndpointListViewModel}"/> used for logging
        /// </summary>
        private readonly ILogger<EndpointListViewModel> logger;

        /// <summary>
        /// The <see cref="ILoggerFactory"/> used to create loggers for child view models
        /// </summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// The <see cref="HealthEndPointClient"/> used for endpoint CRUD operations
        /// </summary>
        private readonly HealthEndPointClient client;

        /// <summary>
        /// Navigation callback to switch views
        /// </summary>
        private readonly Action<ViewModelBase> navigate;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointListViewModel"/> class
        /// </summary>
        /// <param name="client">
        /// The <see cref="HealthEndPointClient"/> used for endpoint CRUD operations
        /// </param>
        /// <param name="navigate">
        /// The navigation callback
        /// </param>
        /// <param name="logger">
        /// The <see cref="ILogger{EndpointListViewModel}"/> used for logging
        /// </param>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to create loggers for child view models
        /// </param>
        public EndpointListViewModel(HealthEndPointClient client, Action<ViewModelBase> navigate, ILogger<EndpointListViewModel> logger, ILoggerFactory loggerFactory)
        {
            this.client = client;
            this.navigate = navigate;
            this.logger = logger;
            this.loggerFactory = loggerFactory;

            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
            AddCommand = ReactiveCommand.Create(OnAdd);
            EditCommand = ReactiveCommand.Create<HealthEndPoint>(OnEdit);
            DeleteCommand = ReactiveCommand.Create<HealthEndPoint>(OnRequestDelete);
            ConfirmDeleteCommand = ReactiveCommand.CreateFromTask(OnConfirmDeleteAsync);
            CancelDeleteCommand = ReactiveCommand.Create(OnCancelDelete);

            RefreshCommand.ThrownExceptions
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(ex =>
                {
                    this.logger.LogError(ex, "RefreshCommand failed");
                    ErrorMessage = ex.Message;
                });

            ConfirmDeleteCommand.ThrownExceptions
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(ex =>
                {
                    this.logger.LogError(ex, "ConfirmDeleteCommand failed");
                    ErrorMessage = ex.Message;
                });

            // auto-refresh on construction
            RefreshCommand.Execute()
                .Subscribe(
                    _ => { },
                    ex => this.logger.LogError(ex, "Initial refresh failed"));
        }

        /// <summary>
        /// Gets the collection of health endpoints
        /// </summary>
        public ObservableCollection<HealthEndPoint> Endpoints { get; } = new();

        /// <summary>
        /// Gets or sets the currently selected endpoint
        /// </summary>
        [Reactive]
        public HealthEndPoint? SelectedEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the endpoint collection contains any items
        /// </summary>
        [Reactive]
        public bool HasEndpoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the delete confirmation overlay is visible
        /// </summary>
        [Reactive]
        public bool IsDeleteConfirmationVisible { get; set; }

        /// <summary>
        /// Gets or sets the endpoint that is pending deletion confirmation
        /// </summary>
        [Reactive]
        public HealthEndPoint? PendingDeleteEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the error message from the last failed operation
        /// </summary>
        [Reactive]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets the command to refresh the endpoint list
        /// </summary>
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

        /// <summary>
        /// Gets the command to add a new endpoint
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddCommand { get; }

        /// <summary>
        /// Gets the command to edit the selected endpoint
        /// </summary>
        public ReactiveCommand<HealthEndPoint, Unit> EditCommand { get; }

        /// <summary>
        /// Gets the command to request deletion of the selected endpoint (shows confirmation)
        /// </summary>
        public ReactiveCommand<HealthEndPoint, Unit> DeleteCommand { get; }

        /// <summary>
        /// Gets the command to confirm deletion of the pending endpoint
        /// </summary>
        public ReactiveCommand<Unit, Unit> ConfirmDeleteCommand { get; }

        /// <summary>
        /// Gets the command to cancel the pending deletion
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelDeleteCommand { get; }

        /// <summary>
        /// Fetches the latest endpoint list from the service and updates the collection
        /// </summary>
        private async Task RefreshAsync()
        {
            this.logger.LogDebug("RefreshAsync starting");
            ErrorMessage = null;
            var endpoints = await client.GetAllAsync();
            Endpoints.Clear();

            foreach (var ep in endpoints)
            {
                Endpoints.Add(ep);
            }

            this.HasEndpoints = this.Endpoints.Count > 0;

            this.logger.LogDebug("RefreshAsync completed with {EndpointCount} endpoint(s)", endpoints.Count);
        }

        /// <summary>
        /// Creates a new editor view model and navigates to it
        /// </summary>
        private void OnAdd()
        {
            var editor = new EndpointEditorViewModel(client, navigate, null, loggerFactory.CreateLogger<EndpointEditorViewModel>());
            navigate(editor);
        }

        /// <summary>
        /// Creates an editor view model pre-filled with the selected endpoint and navigates to it
        /// </summary>
        /// <param name="endpoint">
        /// The <see cref="HealthEndPoint"/> to edit
        /// </param>
        private void OnEdit(HealthEndPoint endpoint)
        {
            var editor = new EndpointEditorViewModel(client, navigate, endpoint, loggerFactory.CreateLogger<EndpointEditorViewModel>());
            navigate(editor);
        }

        /// <summary>
        /// Shows the delete confirmation overlay for the specified endpoint
        /// </summary>
        /// <param name="endpoint">
        /// The <see cref="HealthEndPoint"/> to request deletion for
        /// </param>
        private void OnRequestDelete(HealthEndPoint endpoint)
        {
            this.PendingDeleteEndpoint = endpoint;
            this.IsDeleteConfirmationVisible = true;
        }

        /// <summary>
        /// Confirms deletion of the pending endpoint and removes it from the collection
        /// </summary>
        private async Task OnConfirmDeleteAsync()
        {
            if (this.PendingDeleteEndpoint is null)
            {
                return;
            }

            var endpoint = this.PendingDeleteEndpoint;

            await this.client.DeleteAsync(endpoint.Identifier);
            this.Endpoints.Remove(endpoint);
            this.HasEndpoints = this.Endpoints.Count > 0;

            this.PendingDeleteEndpoint = null;
            this.IsDeleteConfirmationVisible = false;
        }

        /// <summary>
        /// Cancels the pending deletion and hides the confirmation overlay
        /// </summary>
        private void OnCancelDelete()
        {
            this.PendingDeleteEndpoint = null;
            this.IsDeleteConfirmationVisible = false;
        }
    }
}
