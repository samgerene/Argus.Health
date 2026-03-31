// -------------------------------------------------------------------------------------------------
//  <copyright file="DashboardColors.cs">
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

namespace Argus.Health.Pulse.Controls
{
    using Avalonia.Media;

    /// <summary>
    /// Shared color constants for dashboard controls
    /// </summary>
    public static class DashboardColors
    {
        /// <summary>
        /// Teal color for healthy/positive indicators
        /// </summary>
        public static readonly SolidColorBrush Teal = new(Color.Parse("#2dd4bf"));

        /// <summary>
        /// Green color for healthy status
        /// </summary>
        public static readonly SolidColorBrush Green = new(Color.Parse("#22c55e"));

        /// <summary>
        /// Amber color for degraded/warning status
        /// </summary>
        public static readonly SolidColorBrush Amber = new(Color.Parse("#f59e0b"));

        /// <summary>
        /// Red color for down/error status
        /// </summary>
        public static readonly SolidColorBrush Red = new(Color.Parse("#ef4444"));

        /// <summary>
        /// Gray color for pending/unknown status
        /// </summary>
        public static readonly SolidColorBrush Gray = new(Color.Parse("#6b7280"));

        /// <summary>
        /// Slate gray color for muted/inactive text
        /// </summary>
        public static readonly SolidColorBrush SlateGray = new(Color.Parse("#94a3b8"));
    }
}
