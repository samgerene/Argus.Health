// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeSummaryWriter.cs">
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

namespace Argus.Health.Common.Serialization
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.Json;

    using Argus.Health.Common.Model;

    /// <summary>
    /// Provides optimized serialization of <see cref="UptimeSummary"/> instances
    /// using the low-level <see cref="Utf8JsonWriter"/> API
    /// </summary>
    public static class UptimeSummaryWriter
    {
        /// <summary>
        /// Serializes a <see cref="UptimeSummary"/> to a JSON string
        /// </summary>
        /// <param name="summary">
        /// The <see cref="UptimeSummary"/> to serialize
        /// </param>
        /// <returns>
        /// A JSON string representation of the <see cref="UptimeSummary"/>
        /// </returns>
        public static string Write(UptimeSummary summary)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            Write(writer, summary);

            writer.Flush();

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Writes a <see cref="UptimeSummary"/> to a <see cref="Utf8JsonWriter"/>
        /// </summary>
        /// <param name="writer">
        /// The <see cref="Utf8JsonWriter"/> to write to
        /// </param>
        /// <param name="summary">
        /// The <see cref="UptimeSummary"/> to serialize
        /// </param>
        public static void Write(Utf8JsonWriter writer, UptimeSummary summary)
        {
            writer.WriteStartObject();

            writer.WriteString("periodStart", summary.PeriodStart.ToString("o", CultureInfo.InvariantCulture));
            writer.WriteNumber("totalChecks", summary.TotalChecks);
            writer.WriteNumber("healthyChecks", summary.HealthyChecks);
            writer.WriteNumber("averageResponseTimeMs", summary.AverageResponseTimeMs);
            writer.WriteNumber("maxResponseTimeMs", summary.MaxResponseTimeMs);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Serializes a collection of <see cref="UptimeSummary"/> instances to a JSON array string
        /// </summary>
        /// <param name="summaries">
        /// The collection of <see cref="UptimeSummary"/> instances to serialize
        /// </param>
        /// <returns>
        /// A JSON array string representation of the <see cref="UptimeSummary"/> collection
        /// </returns>
        public static string WriteArray(IEnumerable<UptimeSummary> summaries)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartArray();

            foreach (var summary in summaries)
            {
                Write(writer, summary);
            }

            writer.WriteEndArray();

            writer.Flush();

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
