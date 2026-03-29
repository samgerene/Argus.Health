// -------------------------------------------------------------------------------------------------
//  <copyright file="IHealthCheckService.cs">
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
    /// Polls the Argus Health service for recent health check results via IPC
    /// </summary>
    public interface IHealthCheckService : IDisposable
    {
        /// <summary>
        /// Gets an observable of all health check results
        /// </summary>
        IObservable<HealthEndPointCheckResult> ResultsObservable { get; }

        /// <summary>
        /// Gets an observable filtered to non-2xx results
        /// </summary>
        IObservable<HealthEndPointCheckResult> FailureObservable { get; }

        /// <summary>
        /// Starts polling the service for recent check results
        /// </summary>
        void Start();

        /// <summary>
        /// Stops polling
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates the set of endpoint identifiers to poll for check results
        /// </summary>
        /// <param name="endpointIds">
        /// The current set of endpoint identifiers
        /// </param>
        void SetEndpointIds(IEnumerable<Guid> endpointIds);
    }
}
