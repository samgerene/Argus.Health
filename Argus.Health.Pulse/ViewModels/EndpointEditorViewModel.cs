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

    using ReactiveUI;
    using ReactiveUI.Fody.Helpers;
    

    /// <summary>
    /// Create/edit form for a health endpoint
    /// </summary>
    public class EndpointEditorViewModel : ViewModelBase
    {
        private readonly HealthEndPointClient client;
        private readonly Action<ViewModelBase> navigate;
        private readonly Guid? existingId;

        public EndpointEditorViewModel(HealthEndPointClient client, Action<ViewModelBase> navigate, HealthEndPoint? existing)
        {
            this.client = client;
            this.navigate = navigate;

            if (existing != null)
            {
                existingId = existing.Identifier;
                Name = existing.Name;
                Url = existing.Url;
                Frequency = existing.Frequency;
                Timeout = existing.Timeout;
                RetryCount = existing.RetryCount;
                IsEditing = true;
            }
            else
            {
                Frequency = 30;
                Timeout = 5;
                RetryCount = 3;
            }

            var canSave = this.WhenAnyValue(
                x => x.Name, x => x.Url,
                x => x.Frequency, x => x.Timeout, x => x.RetryCount,
                (name, url, freq, timeout, retry) =>
                    !string.IsNullOrWhiteSpace(name) &&
                    !string.IsNullOrWhiteSpace(url) &&
                    Uri.TryCreate(url, UriKind.Absolute, out _) &&
                    freq > 0 && timeout > 0 && retry > 0);

            SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
            CancelCommand = ReactiveCommand.Create(OnCancel);

            SaveCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ex => ErrorMessage = ex.Message);
        }

        [Reactive]
        public string Name { get; set; } = string.Empty;

        [Reactive]
        public string Url { get; set; } = string.Empty;

        [Reactive]
        public int Frequency { get; set; }

        [Reactive]
        public int Timeout { get; set; }

        [Reactive]
        public int RetryCount { get; set; }

        [Reactive]
        public string? ErrorMessage { get; set; }

        public bool IsEditing { get; }

        public string Title => IsEditing ? "Edit Endpoint" : "New Endpoint";

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

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
                RetryCount = RetryCount
            };

            if (IsEditing)
            {
                await client.UpdateAsync(endpoint);
            }
            else
            {
                await client.CreateAsync(endpoint);
            }

            OnCancel(); // navigate back
        }

        private void OnCancel()
        {
            navigate(null!); // signal to go back to endpoint list
        }
    }
}
