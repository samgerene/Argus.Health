// -------------------------------------------------------------------------------------------------
//  <copyright file="IEndpointSyncService.cs">
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

    using Argus.Health.Common.Model;

    /// <summary>
    /// Polls the Argus Health service for the current list of endpoints
    /// </summary>
    public interface IEndpointSyncService : IDisposable
    {
        /// <summary>
        /// Gets an observable that emits the current list of endpoints on each poll
        /// </summary>
        IObservable<IList<HealthEndPoint>> EndpointsObservable { get; }

        /// <summary>
        /// Gets an observable that indicates whether the service connection has an error
        /// </summary>
        IObservable<bool> ConnectionErrorObservable { get; }

        /// <summary>
        /// Gets an observable that indicates whether the sync service is currently running
        /// </summary>
        IObservable<bool> IsRunningObservable { get; }

        /// <summary>
        /// Starts polling
        /// </summary>
        void Start();

        /// <summary>
        /// Stops polling and resets connection error state
        /// </summary>
        void Stop();
    }
}
