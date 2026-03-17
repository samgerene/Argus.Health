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

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Polls <see cref="HealthEndPointClient.GetAllAsync()"/> every 10 seconds
    /// and publishes the current endpoint list
    /// </summary>
    public class EndpointSyncService : IEndpointSyncService
    {
        /// <summary>
        /// The polling interval
        /// </summary>
        public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The <see cref="ILogger{EndpointSyncService}"/> used for logging
        /// </summary>
        private readonly ILogger<EndpointSyncService> logger;

        /// <summary>
        /// The <see cref="HealthEndPointClient"/> used to poll endpoints
        /// </summary>
        private readonly HealthEndPointClient client;

        /// <summary>
        /// Subject that publishes the latest endpoint list
        /// </summary>
        private readonly BehaviorSubject<IList<HealthEndPoint>> endpointsSubject = new(Array.Empty<HealthEndPoint>());

        /// <summary>
        /// Subject that publishes connection error state
        /// </summary>
        private readonly BehaviorSubject<bool> connectionErrorSubject = new(false);

        /// <summary>
        /// Disposable container for Rx subscriptions
        /// </summary>
        private readonly CompositeDisposable disposables = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointSyncService"/> class
        /// </summary>
        /// <param name="client">
        /// The <see cref="HealthEndPointClient"/> used to poll endpoints
        /// </param>
        /// <param name="logger">
        /// The <see cref="ILogger{EndpointSyncService}"/> used for logging
        /// </param>
        public EndpointSyncService(HealthEndPointClient client, ILogger<EndpointSyncService> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        /// <summary>Gets an observable that emits the current list of endpoints on each poll</summary>
        public IObservable<IList<HealthEndPoint>> EndpointsObservable => endpointsSubject.AsObservable();

        /// <summary>Gets an observable that indicates whether the service connection has an error</summary>
        public IObservable<bool> ConnectionErrorObservable => connectionErrorSubject.AsObservable();

        /// <summary>Starts polling</summary>
        public void Start()
        {
            var subscription = Observable.Timer(TimeSpan.Zero, PollInterval)
                .SelectMany(_ => Observable.FromAsync(async ct =>
                {
                    try
                    {
                        var endpoints = await client.GetAllAsync(ct);
                        connectionErrorSubject.OnNext(false);
                        this.logger.LogDebug("Poll completed successfully, {EndpointCount} endpoint(s) returned", endpoints.Count);
                        return endpoints;
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Poll failed");
                        connectionErrorSubject.OnNext(true);
                        return (IList<HealthEndPoint>)Array.Empty<HealthEndPoint>();
                    }
                }))
                .Subscribe(endpoints => endpointsSubject.OnNext(endpoints));

            disposables.Add(subscription);
        }

        /// <summary>Disposes managed resources</summary>
        public void Dispose()
        {
            disposables.Dispose();
            endpointsSubject.Dispose();
            connectionErrorSubject.Dispose();
        }
    }
}
