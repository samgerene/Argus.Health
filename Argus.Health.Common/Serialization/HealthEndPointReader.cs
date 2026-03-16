// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthEndPointReader.cs">
//
//    Copyright (c) 2025-2026 Sam Gerené
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, softwareUseCases
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
    using System.Text.Json;

    using Argus.Health.Common.Model;

    /// <summary>
    /// Provides optimized deserialization of <see cref="HealthEndPoint"/> instances
    /// using the low-level <see cref="Utf8JsonReader"/> API
    /// </summary>
    public static class HealthEndPointReader
    {
        /// <summary>
        /// Deserializes a <see cref="HealthEndPoint"/> from a JSON string
        /// </summary>
        /// <param name="json">
        /// The JSON string to deserialize
        /// </param>
        /// <returns>
        /// A <see cref="HealthEndPoint"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static HealthEndPoint Read(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);

            return Read(ref reader);
        }

        /// <summary>
        /// Deserializes a <see cref="HealthEndPoint"/> from a <see cref="Utf8JsonReader"/>
        /// </summary>
        /// <param name="reader">
        /// The <see cref="Utf8JsonReader"/> positioned at the start of the JSON object
        /// </param>
        /// <returns>
        /// A <see cref="HealthEndPoint"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static HealthEndPoint Read(ref Utf8JsonReader reader)
        {
            var healthEndPoint = new HealthEndPoint();

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
                            healthEndPoint.Identifier = reader.GetGuid();
                            break;
                        case "name":
                        case "Name":
                            healthEndPoint.Name = reader.GetString() ?? string.Empty;
                            break;
                        case "url":
                        case "Url":
                            healthEndPoint.Url = reader.GetString() ?? string.Empty;
                            break;
                        case "frequency":
                        case "Frequency":
                            healthEndPoint.Frequency = reader.GetInt32();
                            break;
                        case "timeout":
                        case "Timeout":
                            healthEndPoint.Timeout = reader.GetInt32();
                            break;
                        case "retryCount":
                        case "RetryCount":
                            healthEndPoint.RetryCount = reader.GetInt32();
                            break;
                        case "isActive":
                        case "IsActive":
                            healthEndPoint.IsActive = reader.GetBoolean();
                            break;
                    }
                }
            }

            return healthEndPoint;
        }

        /// <summary>
        /// Deserializes a JSON array of <see cref="HealthEndPoint"/> instances
        /// </summary>
        /// <param name="json">
        /// The JSON array string to deserialize
        /// </param>
        /// <returns>
        /// A <see cref="IList{HealthEndPoint}"/> populated from the JSON data
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is malformed or cannot be parsed
        /// </exception>
        public static IList<HealthEndPoint> ReadArray(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);

            var result = new List<HealthEndPoint>();

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
