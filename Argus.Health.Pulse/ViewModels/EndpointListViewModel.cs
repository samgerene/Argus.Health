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

    using ReactiveUI;
    using ReactiveUI.Fody.Helpers;

    
    /// <summary>
    /// Endpoint CRUD list view model
    /// </summary>
    public class EndpointListViewModel : ViewModelBase
    {
        private readonly HealthEndPointClient client;
        private readonly Action<ViewModelBase> navigate;

        public EndpointListViewModel(HealthEndPointClient client, Action<ViewModelBase> navigate)
        {
            this.client = client;
            this.navigate = navigate;

            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
            AddCommand = ReactiveCommand.Create(OnAdd);
            EditCommand = ReactiveCommand.Create<HealthEndPoint>(OnEdit);
            DeleteCommand = ReactiveCommand.CreateFromTask<HealthEndPoint>(OnDeleteAsync);

            RefreshCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ex => ErrorMessage = ex.Message);

            DeleteCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ex => ErrorMessage = ex.Message);

            // auto-refresh on construction
            RefreshCommand.Execute().Subscribe();
        }

        public ObservableCollection<HealthEndPoint> Endpoints { get; } = new();

        [Reactive]
        public HealthEndPoint? SelectedEndpoint { get; set; }

        [Reactive]
        public string? ErrorMessage { get; set; }

        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

        public ReactiveCommand<Unit, Unit> AddCommand { get; }

        public ReactiveCommand<HealthEndPoint, Unit> EditCommand { get; }

        public ReactiveCommand<HealthEndPoint, Unit> DeleteCommand { get; }

        private async Task RefreshAsync()
        {
            ErrorMessage = null;
            var endpoints = await client.GetAllAsync();
            Endpoints.Clear();

            foreach (var ep in endpoints)
            {
                Endpoints.Add(ep);
            }
        }

        private void OnAdd()
        {
            var editor = new EndpointEditorViewModel(client, navigate, null);
            navigate(editor);
        }

        private void OnEdit(HealthEndPoint endpoint)
        {
            var editor = new EndpointEditorViewModel(client, navigate, endpoint);
            navigate(editor);
        }

        private async Task OnDeleteAsync(HealthEndPoint endpoint)
        {
            await client.DeleteAsync(endpoint.Identifier);
            Endpoints.Remove(endpoint);
        }
    }
}
