// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointCheckResult.cs">
// 
//     Copyright (c) 2025-2026 Sam Gerené
// 
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
// 
//         http://www.apache.org/licenses/LICENSE-2.0
// 
//     Unless required by applicable law or agreed to in writing, softwareUseCases
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.
// 
//   </copyright>
//   ------------------------------------------------------------------------------------------------

namespace Argus.Health.Common.Model
{
    using System;

    /// <summary>
    /// Represents the results of a <see cref="HealthEndPoint"/> check
    /// </summary>
    public class HealthEndPointCheckResult
    {
        /// <summary>
        /// The unique identifier
        /// </summary>
        public Guid Identifier { get; set; }

        /// <summary>
        /// The <see cref="DateTime"/> when the <see cref="HealthEndPoint"/> was accessed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the HTTP status code associated to this request
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error message that may be associated to this result.
        /// </summary>
        public string? ErrorMessage { get; set; } = null;

        /// <summary>
        /// Gets or sets the unique identifier of the <see cref="HealthEndPoint"/>
        /// that this result is associated with
        /// </summary>
        public Guid HealthEndPoint { get; set; }
    }
}
