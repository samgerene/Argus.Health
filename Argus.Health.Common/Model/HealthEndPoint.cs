// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthEndPoint.cs">
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
//  ------------------------------------------------------------------------------------------------

namespace Argus.Health.Common.Model
{
    using System;

    /// <summary>
    /// Represents a Health EndPoint
    /// </summary>
    public class HealthEndPoint
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        public Guid Identifier { get; set; }

        /// <summary>
        /// Gets or sets the human-readable name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Urls that are to be checked for this <see cref="HealthEndPoint"/>
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the frequency measured in seconds with which this endpoint needs to be
        /// monitored
        /// </summary>
        /// <remarks>
        /// The default value is 30 seconds
        /// </remarks>
        public int Frequency { get; set; } = 30;

        /// <summary>
        /// Gets or sets the timeout measured in seconds with which this endpoint needs to be
        /// monitored
        /// </summary>
        /// <remarks>
        /// The default value is 5 seconds
        /// </remarks>
        public int Timeout { get; set; } = 5;

        /// <summary>
        /// Gets or sets the amount of times the endpoint shall be retried
        /// </summary>
        /// <remarks>
        /// The default value is 3
        /// </remarks>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value whether this endpoint needs to be checked or not.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
