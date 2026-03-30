// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeSummary.cs">
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

namespace Argus.Health.Common.Model
{
    using System;

    /// <summary>
    /// Represents an aggregated uptime summary for a single time period
    /// </summary>
    public class UptimeSummary
    {
        /// <summary>
        /// Gets or sets the start of the aggregation period
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets the total number of health checks in this period
        /// </summary>
        public int TotalChecks { get; set; }

        /// <summary>
        /// Gets or sets the number of healthy (HTTP 2xx) checks in this period
        /// </summary>
        public int HealthyChecks { get; set; }

        /// <summary>
        /// Gets or sets the average response time in milliseconds for this period
        /// </summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum response time in milliseconds for this period
        /// </summary>
        public long MaxResponseTimeMs { get; set; }

        /// <summary>
        /// Gets the uptime percentage for this period (0–100)
        /// </summary>
        public double UptimePercent => this.TotalChecks > 0
            ? 100.0 * this.HealthyChecks / this.TotalChecks
            : 0.0;
    }
}
