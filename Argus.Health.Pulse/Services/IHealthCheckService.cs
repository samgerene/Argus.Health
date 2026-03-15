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
    /// Performs HTTP health checks against endpoints, mirroring the service's background service
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
        /// Diffs the provided endpoints against currently running monitors
        /// and starts/stops as needed
        /// </summary>
        void UpdateEndpoints(IList<HealthEndPoint> endpoints);
    }
}
