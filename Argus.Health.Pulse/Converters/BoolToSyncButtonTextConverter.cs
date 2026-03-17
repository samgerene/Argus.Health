// -------------------------------------------------------------------------------------------------
//  <copyright file="BoolToSyncButtonTextConverter.cs">
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

namespace Argus.Health.Pulse.Converters
{
    using System;
    using System.Globalization;

    using Avalonia.Data.Converters;

    /// <summary>
    /// Converts a boolean running state to sync button text.
    /// <c>true</c> maps to "Stop Sync", <c>false</c> maps to "Start Sync"
    /// </summary>
    public class BoolToSyncButtonTextConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to the corresponding sync button text
        /// </summary>
        /// <param name="value">The boolean value indicating whether sync is running</param>
        /// <param name="targetType">The target type (unused)</param>
        /// <param name="parameter">The converter parameter (unused)</param>
        /// <param name="culture">The culture info (unused)</param>
        /// <returns>"Stop Sync" when <c>true</c>, "Start Sync" when <c>false</c></returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? "Stop Sync" : "Start Sync";
        }

        /// <summary>
        /// Not supported. Throws <see cref="NotSupportedException"/>
        /// </summary>
        /// <param name="value">The value (unused)</param>
        /// <param name="targetType">The target type (unused)</param>
        /// <param name="parameter">The converter parameter (unused)</param>
        /// <param name="culture">The culture info (unused)</param>
        /// <returns>Does not return</returns>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
