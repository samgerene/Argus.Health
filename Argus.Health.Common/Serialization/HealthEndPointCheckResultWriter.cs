// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthEndPointCheckResultWriter.cs">
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
    /// Provides optimized serialization of <see cref="HealthEndPointCheckResult"/> instances
    /// using the low-level <see cref="Utf8JsonWriter"/> API
    /// </summary>
    public static class HealthEndPointCheckResultWriter
    {
        /// <summary>
        /// Serializes a <see cref="HealthEndPointCheckResult"/> to a JSON string
        /// </summary>
        /// <param name="checkResult">
        /// The <see cref="HealthEndPointCheckResult"/> to serialize
        /// </param>
        /// <returns>
        /// A JSON string representation of the <see cref="HealthEndPointCheckResult"/>
        /// </returns>
        public static string Write(HealthEndPointCheckResult checkResult)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            Write(writer, checkResult);

            writer.Flush();

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Writes a <see cref="HealthEndPointCheckResult"/> to a <see cref="Utf8JsonWriter"/>
        /// </summary>
        /// <param name="writer">
        /// The <see cref="Utf8JsonWriter"/> to write to
        /// </param>
        /// <param name="checkResult">
        /// The <see cref="HealthEndPointCheckResult"/> to serialize
        /// </param>
        public static void Write(Utf8JsonWriter writer, HealthEndPointCheckResult checkResult)
        {
            writer.WriteStartObject();

            writer.WriteString("identifier", checkResult.Identifier);
            writer.WriteString("timestamp", checkResult.Timestamp.ToString("o", CultureInfo.InvariantCulture));
            writer.WriteNumber("statusCode", checkResult.StatusCode);
            writer.WriteString("errorMessage", checkResult.ErrorMessage);
            writer.WriteNumber("responseTimeMs", checkResult.ResponseTimeMs);
            writer.WriteString("healthEndPoint", checkResult.HealthEndPoint);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Serializes a collection of <see cref="HealthEndPointCheckResult"/> instances to a JSON array string
        /// </summary>
        /// <param name="checkResults">
        /// The collection of <see cref="HealthEndPointCheckResult"/> instances to serialize
        /// </param>
        /// <returns>
        /// A JSON array string representation of the <see cref="HealthEndPointCheckResult"/> collection
        /// </returns>
        public static string WriteArray(IEnumerable<HealthEndPointCheckResult> checkResults)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartArray();

            foreach (var checkResult in checkResults)
            {
                Write(writer, checkResult);
            }

            writer.WriteEndArray();

            writer.Flush();

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
