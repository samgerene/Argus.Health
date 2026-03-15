// -------------------------------------------------------------------------------------------------
//  <copyright file="DashboardViewModel.cs">
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
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Argus.Health.Pulse.Services;

    using ReactiveUI;

    /// <summary>
    /// Real-time status grid showing health of all endpoints
    /// </summary>
    public class DashboardViewModel : ViewModelBase, IDisposable
    {
        private readonly CompositeDisposable disposables = new();

        public DashboardViewModel(IEndpointSyncService syncService, IHealthCheckService healthCheckService)
        {
            var endpointSubscription = syncService.EndpointsObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(endpoints =>
                {
                    var existingIds = Endpoints.Select(e => e.Identifier).ToHashSet();
                    var incomingIds = endpoints.Select(e => e.Identifier).ToHashSet();

                    // remove endpoints that no longer exist
                    var toRemove = Endpoints.Where(e => !incomingIds.Contains(e.Identifier)).ToList();
                    foreach (var item in toRemove)
                    {
                        Endpoints.Remove(item);
                    }

                    // add new endpoints
                    foreach (var ep in endpoints)
                    {
                        if (!existingIds.Contains(ep.Identifier))
                        {
                            Endpoints.Add(new EndpointStatusViewModel(ep.Identifier, ep.Name, ep.Url));
                        }
                    }
                });

            var resultsSubscription = healthCheckService.ResultsObservable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(result =>
                {
                    var row = Endpoints.FirstOrDefault(e => e.Identifier == result.HealthEndPoint);

                    if (row != null)
                    {
                        row.StatusCode = result.StatusCode;
                        row.LastChecked = result.Timestamp;
                        row.ErrorMessage = result.ErrorMessage;
                    }
                });

            disposables.Add(endpointSubscription);
            disposables.Add(resultsSubscription);
        }

        public ObservableCollection<EndpointStatusViewModel> Endpoints { get; } = new();

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
