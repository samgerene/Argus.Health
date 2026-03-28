// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointEditorViewModel.cs">
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
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Argus.Health.Pulse.Client;
    using Argus.Health.Common.Model;

    using Microsoft.Extensions.Logging;

    using ReactiveUI.Avalonia;

    using ReactiveUI;
    using ReactiveUI.SourceGenerators;

    /// <summary>
    /// Create/edit form for a health endpoint
    /// </summary>
    public partial class EndpointEditorViewModel : ViewModelBase
    {
        /// <summary>
        /// The <see cref="ILogger{EndpointEditorViewModel}"/> used for logging
        /// </summary>
        private readonly ILogger<EndpointEditorViewModel> logger;

        /// <summary>
        /// The <see cref="HealthEndPointClient"/> used for endpoint CRUD operations
        /// </summary>
        private readonly HealthEndPointClient client;

        /// <summary>
        /// Navigation callback to switch views
        /// </summary>
        private readonly Action<ViewModelBase> navigate;

        /// <summary>
        /// The identifier of the endpoint being edited, or null for a new endpoint
        /// </summary>
        private readonly Guid? existingId;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointEditorViewModel"/> class
        /// </summary>
        /// <param name="client">
        /// The <see cref="HealthEndPointClient"/> used for endpoint CRUD operations
        /// </param>
        /// <param name="navigate">
        /// The navigation callback
        /// </param>
        /// <param name="existing">
        /// The existing <see cref="HealthEndPoint"/> to edit, or null for a new endpoint
        /// </param>
        /// <param name="logger">
        /// The <see cref="ILogger{EndpointEditorViewModel}"/> used for logging
        /// </param>
        public EndpointEditorViewModel(HealthEndPointClient client, Action<ViewModelBase> navigate, HealthEndPoint? existing, ILogger<EndpointEditorViewModel> logger)
        {
            this.client = client;
            this.navigate = navigate;
            this.logger = logger;

            if (existing != null)
            {
                existingId = existing.Identifier;
                Name = existing.Name;
                Url = existing.Url;
                Frequency = existing.Frequency;
                Timeout = existing.Timeout;
                RetryCount = existing.RetryCount;
                IsActive = existing.IsActive;
                IsEditing = true;
            }
            else
            {
                Frequency = 30;
                Timeout = 5;
                RetryCount = 3;
                IsActive = true;
            }

            var canSave = this.WhenAnyValue(
                x => x.Name, x => x.Url,
                x => x.Frequency, x => x.Timeout, x => x.RetryCount,
                (name, url, freq, timeout, retry) =>
                    !string.IsNullOrWhiteSpace(name) &&
                    !string.IsNullOrWhiteSpace(url) &&
                    Uri.TryCreate(url, UriKind.Absolute, out _) &&
                    freq > 0 && timeout > 0 && retry > 0);

            this.SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
            this.CancelCommand = ReactiveCommand.Create(OnCancel);

            this.SaveCommand.ThrownExceptions
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(ex =>
                {
                    this.logger.LogError(ex, "SaveCommand failed");
                    ErrorMessage = ex.Message;
                });
        }

        /// <summary>
        /// Gets or sets the endpoint name
        /// </summary>
        [Reactive]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the endpoint URL
        /// </summary>
        [Reactive]
        public partial string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the polling frequency in seconds
        /// </summary>
        [Reactive]
        public partial int Frequency { get; set; }

        /// <summary>
        /// Gets or sets the HTTP timeout in seconds
        /// </summary>
        [Reactive]
        public partial int Timeout { get; set; }

        /// <summary>
        /// Gets or sets the Polly retry count
        /// </summary>
        [Reactive]
        public partial int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this endpoint is active
        /// </summary>
        [Reactive]
        public partial bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the validation or save error message
        /// </summary>
        [Reactive]
        public partial string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets a value indicating whether an existing endpoint is being edited
        /// </summary>
        public bool IsEditing { get; }

        /// <summary>
        /// Gets the form title based on whether creating or editing
        /// </summary>
        public string Title => IsEditing ? "Edit Endpoint" : "New Endpoint";

        /// <summary>
        /// Gets the command to save the endpoint
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        /// <summary>
        /// Gets the command to cancel and navigate back
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        /// <summary>
        /// Validates and persists the endpoint, then navigates back
        /// </summary>
        private async Task SaveAsync()
        {
            ErrorMessage = null;

            var endpoint = new HealthEndPoint
            {
                Identifier = existingId ?? Guid.NewGuid(),
                Name = Name,
                Url = Url,
                Frequency = Frequency,
                Timeout = Timeout,
                RetryCount = RetryCount,
                IsActive = IsActive
            };

            if (this.IsEditing)
            {
                await client.UpdateAsync(endpoint);
            }
            else
            {
                await client.CreateAsync(endpoint);
            }

            this.OnCancel(); // navigate back
        }

        /// <summary>
        /// Navigates back to the endpoint list without saving
        /// </summary>
        private void OnCancel()
        {
            this.navigate(null!); // signal to go back to endpoint list
        }
    }
}
