// -------------------------------------------------------------------------------------------------
//  <copyright file="IncidentItem.cs">
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

    /// <summary>
    /// Represents a single incident (failed health check) for display in the incidents list
    /// </summary>
    public class IncidentItem
    {
        /// <summary>
        /// Gets or sets the name of the endpoint that failed
        /// </summary>
        public string EndpointName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the failed check
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response time in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
