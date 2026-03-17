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

        private readonly HealthEndPointClient client;
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
            DeleteCommand = ReactiveCommand.CreateFromTask<HealthEndPoint>(OnDeleteAsync);

            RefreshCommand.ThrownExceptions
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(ex =>
                {
                    this.logger.LogError(ex, "RefreshCommand failed");
                    ErrorMessage = ex.Message;
                });

            DeleteCommand.ThrownExceptions
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(ex =>
                {
                    this.logger.LogError(ex, "DeleteCommand failed");
                    ErrorMessage = ex.Message;
                });

            // auto-refresh on construction
            RefreshCommand.Execute().Subscribe();
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
        /// Gets the command to delete the selected endpoint
        /// </summary>
        public ReactiveCommand<HealthEndPoint, Unit> DeleteCommand { get; }

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

            this.logger.LogDebug("RefreshAsync completed with {EndpointCount} endpoint(s)", endpoints.Count);
        }

        private void OnAdd()
        {
            var editor = new EndpointEditorViewModel(client, navigate, null, loggerFactory.CreateLogger<EndpointEditorViewModel>());
            navigate(editor);
        }

        private void OnEdit(HealthEndPoint endpoint)
        {
            var editor = new EndpointEditorViewModel(client, navigate, endpoint, loggerFactory.CreateLogger<EndpointEditorViewModel>());
            navigate(editor);
        }

        private async Task OnDeleteAsync(HealthEndPoint endpoint)
        {
            await client.DeleteAsync(endpoint.Identifier);
            Endpoints.Remove(endpoint);
        }
    }
}
