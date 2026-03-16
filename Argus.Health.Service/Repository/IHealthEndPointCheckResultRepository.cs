// -------------------------------------------------------------------------------------------------
//  <copyright file="IHealthEndPointCheckResultRepository.cs">
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
    /// The purpose of the <see cref="IHealthEndPointCheckResultRepository"/> is to perform
    /// Create and Read operations on <see cref="HealthEndPointCheckResult"/> records
    /// </summary>
    public interface IHealthEndPointCheckResultRepository
    {
        /// <summary>
        /// Asynchronously creates a <see cref="HealthEndPointCheckResult"/> in the database
        /// </summary>
        /// <param name="checkResult">
        /// The <see cref="HealthEndPointCheckResult"/> that is to be added
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure
        /// </returns>
        Task<Result> CreateAsync(HealthEndPointCheckResult checkResult);

        /// <summary>
        /// Asynchronously reads <see cref="HealthEndPointCheckResult"/> records from the database
        /// </summary>
        /// <param name="healthEndPointIdentifier">
        /// Optional filter to return only results for the specified <see cref="HealthEndPoint"/>
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableList{HealthEndPointCheckResult}"/>
        /// </returns>
        Task<ImmutableList<HealthEndPointCheckResult>> ReadAsync(Guid? healthEndPointIdentifier = null);
    }
}
