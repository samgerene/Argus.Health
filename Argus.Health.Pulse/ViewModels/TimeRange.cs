// -------------------------------------------------------------------------------------------------
//  <copyright file="TimeRange.cs">
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
    /// Represents a selectable time range for the dashboard detail panel
    /// </summary>
    public enum TimeRange
    {
        /// <summary>
        /// Last hour
        /// </summary>
        LastHour,

        /// <summary>
        /// Last 6 hours
        /// </summary>
        Last6Hours,

        /// <summary>
        /// Last 24 hours
        /// </summary>
        Last24Hours,

        /// <summary>
        /// Last 7 days
        /// </summary>
        Last7Days
    }

    /// <summary>
    /// Extension methods for <see cref="TimeRange"/>
    /// </summary>
    public static class TimeRangeExtensions
    {
        /// <summary>
        /// Converts a <see cref="TimeRange"/> to its corresponding <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="range">The time range to convert</param>
        /// <returns>The corresponding <see cref="TimeSpan"/></returns>
        public static TimeSpan ToTimeSpan(this TimeRange range)
        {
            return range switch
            {
                TimeRange.LastHour => TimeSpan.FromHours(1),
                TimeRange.Last6Hours => TimeSpan.FromHours(6),
                TimeRange.Last24Hours => TimeSpan.FromHours(24),
                TimeRange.Last7Days => TimeSpan.FromDays(7),
                _ => TimeSpan.FromHours(1)
            };
        }

        /// <summary>
        /// Gets a human-readable display label for the <see cref="TimeRange"/>
        /// </summary>
        /// <param name="range">The time range</param>
        /// <returns>A display string</returns>
        public static string ToDisplayString(this TimeRange range)
        {
            return range switch
            {
                TimeRange.LastHour => "1h",
                TimeRange.Last6Hours => "6h",
                TimeRange.Last24Hours => "24h",
                TimeRange.Last7Days => "7d",
                _ => "1h"
            };
        }
    }
}
