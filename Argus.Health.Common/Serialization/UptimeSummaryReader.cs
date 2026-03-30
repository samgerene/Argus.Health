// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeSummaryReader.cs">
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
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.Json;

    using Argus.Health.Common.Model;

    /// <summary>
    /// Provides optimized deserialization of <see cref="UptimeSummary"/> instances
    /// using the low-level <see cref="Utf8JsonReader"/> API
    /// </summary>
    public static class UptimeSummaryReader
    {
        /// <summary>
        /// Deserializes a <see cref="UptimeSummary"/> from a JSON string
        /// </summary>
        /// <param name="json">
        /// The JSON string to deserialize
        /// </param>
        /// <returns>
        /// A <see cref="UptimeSummary"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static UptimeSummary Read(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);

            return Read(ref reader);
        }

        /// <summary>
        /// Deserializes a <see cref="UptimeSummary"/> from a <see cref="Utf8JsonReader"/>
        /// </summary>
        /// <param name="reader">
        /// The <see cref="Utf8JsonReader"/> positioned at the start of the JSON object
        /// </param>
        /// <returns>
        /// A <see cref="UptimeSummary"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static UptimeSummary Read(ref Utf8JsonReader reader)
        {
            var summary = new UptimeSummary();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "periodStart":
                        case "PeriodStart":
                            var periodStartString = reader.GetString();
                            summary.PeriodStart = DateTime.Parse(periodStartString!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                            break;
                        case "totalChecks":
                        case "TotalChecks":
                            summary.TotalChecks = reader.GetInt32();
                            break;
                        case "healthyChecks":
                        case "HealthyChecks":
                            summary.HealthyChecks = reader.GetInt32();
                            break;
                        case "averageResponseTimeMs":
                        case "AverageResponseTimeMs":
                            summary.AverageResponseTimeMs = reader.GetDouble();
                            break;
                        case "maxResponseTimeMs":
                        case "MaxResponseTimeMs":
                            summary.MaxResponseTimeMs = reader.GetInt64();
                            break;
                    }
                }
            }

            return summary;
        }

        /// <summary>
        /// Deserializes a JSON array of <see cref="UptimeSummary"/> instances
        /// </summary>
        /// <param name="json">
        /// The JSON array string to deserialize
        /// </param>
        /// <returns>
        /// A <see cref="IList{UptimeSummary}"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static IList<UptimeSummary> ReadArray(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);

            var result = new List<UptimeSummary>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    continue;
                }

                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    result.Add(Read(ref reader));
                }
            }

            return result;
        }
    }
}
