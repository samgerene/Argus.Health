// -------------------------------------------------------------------------------------------------
//  <copyright file="IHealthEndPointRepository.cs"  >
//
//    Copyright (c) 2025-2026 Sam Gerené
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, softwareUseCases
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
//  </copyright>
// ------------------------------------------------------------------------------------------------

namespace Argus.Health.Service.Repository
{
    using System;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;

    using FluentResults;

    /// <summary>
    /// The purpose of the <see cref="IHealthEndPointRepository"/> is to perform
    /// CRUD on the <see cref="HealthEndPoint"/> repository
    /// </summary>
    public interface IHealthEndPointRepository
    {
        /// <summary>
        /// The event that is raised when a <see cref="HealthEndPoint"/>
        /// has been added
        /// </summary>
        public event EventHandler<HealthEndPoint>? EndpointAdded;

        /// <summary>
        /// The event that is raised when a <see cref="HealthEndPoint"/>
        /// has been updated
        /// </summary>
        public event EventHandler<HealthEndPoint>? EndpointUpdated;

        /// <summary>
        /// The event that is raised when a <see cref="HealthEndPoint"/>
        /// has been deleted
        /// </summary>
        public event EventHandler<HealthEndPoint>? EndpointRemoved;

        /// <summary>
        /// Initializes a new database file if it does not yet exist
        /// </summary>
        public void InitializeDatabase(bool force = false);

        /// <summary>
        /// Reads all the <see cref="HealthEndPoint"/> from the repository
        /// </summary>
        /// <param name="identifiers">
        /// The array of unique identifiers to query, if null or empty all are queried
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableList{HealthEndPoint}"/>
        /// </returns>
        Task<ImmutableList<HealthEndPoint>> ReadAsync(Guid[]? identifiers = null);

        /// <summary>
        /// Adds the provided <see cref="HealthEndPoint"/> to the repository
        /// </summary>
        /// <param name="healthEndPoint">
        /// the subject <see cref="HealthEndPoint"/>
        /// </param>
        Task<Result> CreateAsync(HealthEndPoint healthEndPoint);

        /// <summary>
        /// Updates the provided <see cref="HealthEndPoint"/> to the repository
        /// </summary>
        /// <param name="healthEndPoint">
        /// the subject <see cref="HealthEndPoint"/>
        /// </param>
        Task<Result> UpdateAsync(HealthEndPoint healthEndPoint);

        /// <summary>
        /// Deletes the provided <see cref="HealthEndPoint"/> from the repository
        /// </summary>
        /// <param name="healthEndPoint">
        /// the subject <see cref="HealthEndPoint"/>
        /// </param>
        Task<Result> DeleteAsync(HealthEndPoint healthEndPoint);
    }
}
