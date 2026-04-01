// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeDaysRange.cs">
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
    /// <summary>
    /// Represents a selectable number of days for the uptime visualization
    /// </summary>
    public enum UptimeDaysRange
    {
        /// <summary>
        /// Last 7 days
        /// </summary>
        Last7Days,

        /// <summary>
        /// Last 30 days
        /// </summary>
        Last30Days,

        /// <summary>
        /// Last 60 days
        /// </summary>
        Last60Days,

        /// <summary>
        /// Last 90 days
        /// </summary>
        Last90Days
    }

    /// <summary>
    /// Extension methods for <see cref="UptimeDaysRange"/>
    /// </summary>
    public static class UptimeDaysRangeExtensions
    {
        /// <summary>
        /// Converts a <see cref="UptimeDaysRange"/> to its corresponding number of days
        /// </summary>
        /// <param name="range">The uptime days range to convert</param>
        /// <returns>The number of days</returns>
        public static int ToDays(this UptimeDaysRange range)
        {
            return range switch
            {
                UptimeDaysRange.Last7Days => 7,
                UptimeDaysRange.Last30Days => 30,
                UptimeDaysRange.Last60Days => 60,
                UptimeDaysRange.Last90Days => 90,
                _ => 90
            };
        }

        /// <summary>
        /// Gets a human-readable display label for the <see cref="UptimeDaysRange"/>
        /// </summary>
        /// <param name="range">The uptime days range</param>
        /// <returns>A display string</returns>
        public static string ToDisplayString(this UptimeDaysRange range)
        {
            return range switch
            {
                UptimeDaysRange.Last7Days => "7d",
                UptimeDaysRange.Last30Days => "30d",
                UptimeDaysRange.Last60Days => "60d",
                UptimeDaysRange.Last90Days => "90d",
                _ => "90d"
            };
        }
    }
}
