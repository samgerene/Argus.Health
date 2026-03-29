// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthEndPointCheckResultReader.cs">
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
    /// Provides optimized deserialization of <see cref="HealthEndPointCheckResult"/> instances
    /// using the low-level <see cref="Utf8JsonReader"/> API
    /// </summary>
    public static class HealthEndPointCheckResultReader
    {
        /// <summary>
        /// Deserializes a <see cref="HealthEndPointCheckResult"/> from a JSON string
        /// </summary>
        /// <param name="json">
        /// The JSON string to deserialize
        /// </param>
        /// <returns>
        /// A <see cref="HealthEndPointCheckResult"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static HealthEndPointCheckResult Read(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);

            return Read(ref reader);
        }

        /// <summary>
        /// Deserializes a <see cref="HealthEndPointCheckResult"/> from a <see cref="Utf8JsonReader"/>
        /// </summary>
        /// <param name="reader">
        /// The <see cref="Utf8JsonReader"/> positioned at the start of the JSON object
        /// </param>
        /// <returns>
        /// A <see cref="HealthEndPointCheckResult"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static HealthEndPointCheckResult Read(ref Utf8JsonReader reader)
        {
            var checkResult = new HealthEndPointCheckResult();

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
                        case "identifier":
                        case "Identifier":
                            checkResult.Identifier = reader.GetGuid();
                            break;
                        case "timestamp":
                        case "Timestamp":
                            var timestampString = reader.GetString();
                            checkResult.Timestamp = DateTime.Parse(timestampString!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                            break;
                        case "statusCode":
                        case "StatusCode":
                            checkResult.StatusCode = reader.GetInt32();
                            break;
                        case "errorMessage":
                        case "ErrorMessage":
                            checkResult.ErrorMessage = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                            break;
                        case "responseTimeMs":
                        case "ResponseTimeMs":
                            checkResult.ResponseTimeMs = reader.GetInt64();
                            break;
                        case "healthEndPoint":
                        case "HealthEndPoint":
                            checkResult.HealthEndPoint = reader.GetGuid();
                            break;
                    }
                }
            }

            return checkResult;
        }

        /// <summary>
        /// Deserializes a JSON array of <see cref="HealthEndPointCheckResult"/> instances
        /// </summary>
        /// <param name="json">
        /// The JSON array string to deserialize
        /// </param>
        /// <returns>
        /// A <see cref="IList{HealthEndPointCheckResult}"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static IList<HealthEndPointCheckResult> ReadArray(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);

            var result = new List<HealthEndPointCheckResult>();

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
