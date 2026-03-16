// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointSyncService.cs">
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

namespace Argus.Health.Pulse.Services
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using Argus.Health.Pulse.Client;
    using Argus.Health.Common.Model;
    
    /// <summary>
    /// Polls <see cref="HealthEndPointClient.GetAllAsync()"/> every 10 seconds
    /// and publishes the current endpoint list
    /// </summary>
    public class EndpointSyncService : IEndpointSyncService
    {
        public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        private readonly HealthEndPointClient client;
        private readonly BehaviorSubject<IList<HealthEndPoint>> endpointsSubject = new(Array.Empty<HealthEndPoint>());
        private readonly BehaviorSubject<bool> connectionErrorSubject = new(false);
        private readonly CompositeDisposable disposables = new();

        public EndpointSyncService(HealthEndPointClient client)
        {
            this.client = client;
        }

        public IObservable<IList<HealthEndPoint>> EndpointsObservable => endpointsSubject.AsObservable();

        public IObservable<bool> ConnectionErrorObservable => connectionErrorSubject.AsObservable();

        public void Start()
        {
            var subscription = Observable.Timer(TimeSpan.Zero, PollInterval)
                .SelectMany(_ => Observable.FromAsync(async ct =>
                {
                    try
                    {
                        var endpoints = await client.GetAllAsync(ct);
                        connectionErrorSubject.OnNext(false);
                        return endpoints;
                    }
                    catch
                    {
                        connectionErrorSubject.OnNext(true);
                        return (IList<HealthEndPoint>)Array.Empty<HealthEndPoint>();
                    }
                }))
                .Subscribe(endpoints => endpointsSubject.OnNext(endpoints));

            disposables.Add(subscription);
        }

        public void Dispose()
        {
            disposables.Dispose();
            endpointsSubject.Dispose();
            connectionErrorSubject.Dispose();
        }
    }
}
