// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointSerializationTestFixture.cs"  >
//
//     Copyright (c) 2025-2026 Sam Gerené
//
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
//
//         http://www.apache.org/licenses/LICENSE-2.0
//
//     Unless required by applicable law or agreed to in writing, softwareUseCases
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.
//
//   </copyright>
//   ------------------------------------------------------------------------------------------------

namespace Argus.Health.Service.Tests.Serialization
{
    using System;
    using System.Collections.Generic;

    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;

    /// <summary>
    /// Suite of tests for the <see cref="HealthEndPointReader"/> and <see cref="HealthEndPointWriter"/> classes
    /// </summary>
    [TestFixture]
    public class HealthEndPointSerializationTestFixture
    {
        [Test]
        public void Verify_that_round_trip_serialization_preserves_all_properties()
        {
            var identifier = Guid.NewGuid();

            var original = new HealthEndPoint
            {
                Identifier = identifier,
                Name = "test-endpoint",
                Url = "https://example.com/healthz",
                Frequency = 15,
                Timeout = 3,
                RetryCount = 2,
                IsActive = false
            };

            var json = HealthEndPointWriter.Write(original);
            var deserialized = HealthEndPointReader.Read(json);

            Assert.That(deserialized.Identifier, Is.EqualTo(original.Identifier));
            Assert.That(deserialized.Name, Is.EqualTo(original.Name));
            Assert.That(deserialized.Url, Is.EqualTo(original.Url));
            Assert.That(deserialized.Frequency, Is.EqualTo(original.Frequency));
            Assert.That(deserialized.Timeout, Is.EqualTo(original.Timeout));
            Assert.That(deserialized.RetryCount, Is.EqualTo(original.RetryCount));
            Assert.That(deserialized.IsActive, Is.EqualTo(original.IsActive));
        }

        [Test]
        public void Verify_that_Writer_produces_valid_json()
        {
            var healthEndPoint = new HealthEndPoint
            {
                Identifier = Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da"),
                Name = "my-endpoint",
                Url = "https://example.com",
                Frequency = 30,
                Timeout = 5,
                RetryCount = 3
            };

            var json = HealthEndPointWriter.Write(healthEndPoint);

            Assert.That(json, Does.Contain("\"identifier\""));
            Assert.That(json, Does.Contain("\"name\""));
            Assert.That(json, Does.Contain("\"url\""));
            Assert.That(json, Does.Contain("\"frequency\""));
            Assert.That(json, Does.Contain("\"timeout\""));
            Assert.That(json, Does.Contain("\"retryCount\""));
            Assert.That(json, Does.Contain("\"isActive\""));
            Assert.That(json, Does.Contain("my-endpoint"));
        }

        [Test]
        public void Verify_that_Reader_handles_PascalCase_properties()
        {
            var json = """
                {
                    "Identifier": "cfb2e590-eed6-4223-b2ba-271ed0cb06da",
                    "Name": "pascal-endpoint",
                    "Url": "https://example.com/health",
                    "Frequency": 60,
                    "Timeout": 10,
                    "RetryCount": 5,
                    "IsActive": false
                }
                """;

            var result = HealthEndPointReader.Read(json);

            Assert.That(result.Identifier, Is.EqualTo(Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da")));
            Assert.That(result.Name, Is.EqualTo("pascal-endpoint"));
            Assert.That(result.Url, Is.EqualTo("https://example.com/health"));
            Assert.That(result.Frequency, Is.EqualTo(60));
            Assert.That(result.Timeout, Is.EqualTo(10));
            Assert.That(result.RetryCount, Is.EqualTo(5));
            Assert.That(result.IsActive, Is.EqualTo(false));
        }

        [Test]
        public void Verify_that_Reader_handles_camelCase_properties()
        {
            var json = """
                {
                    "identifier": "fe08550c-d926-4758-808f-a9a991e0ff37",
                    "name": "camel-endpoint",
                    "url": "https://example.com/status",
                    "frequency": 45,
                    "timeout": 7,
                    "retryCount": 4,
                    "isActive": false
                }
                """;

            var result = HealthEndPointReader.Read(json);

            Assert.That(result.Identifier, Is.EqualTo(Guid.Parse("fe08550c-d926-4758-808f-a9a991e0ff37")));
            Assert.That(result.Name, Is.EqualTo("camel-endpoint"));
            Assert.That(result.Url, Is.EqualTo("https://example.com/status"));
            Assert.That(result.Frequency, Is.EqualTo(45));
            Assert.That(result.Timeout, Is.EqualTo(7));
            Assert.That(result.RetryCount, Is.EqualTo(4));
            Assert.That(result.IsActive, Is.EqualTo(false));
        }

        [Test]
        public void Verify_that_Reader_returns_defaults_for_missing_properties()
        {
            var json = """{"name": "minimal"}""";

            var result = HealthEndPointReader.Read(json);

            Assert.That(result.Name, Is.EqualTo("minimal"));
            Assert.That(result.Identifier, Is.EqualTo(Guid.Empty));
            Assert.That(result.Url, Is.EqualTo(string.Empty));
            Assert.That(result.Frequency, Is.EqualTo(30));
            Assert.That(result.Timeout, Is.EqualTo(5));
            Assert.That(result.RetryCount, Is.EqualTo(3));
            Assert.That(result.IsActive, Is.EqualTo(true));
        }

        [Test]
        public void Verify_that_Reader_ignores_unknown_properties()
        {
            var json = """
                {
                    "name": "test",
                    "unknownField": "should be ignored",
                    "url": "https://example.com"
                }
                """;

            var result = HealthEndPointReader.Read(json);

            Assert.That(result.Name, Is.EqualTo("test"));
            Assert.That(result.Url, Is.EqualTo("https://example.com"));
        }

        [Test]
        public void Verify_that_WriteArray_produces_valid_json_array()
        {
            var endpoints = new List<HealthEndPoint>
            {
                new HealthEndPoint
                {
                    Identifier = Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da"),
                    Name = "ep1",
                    Url = "https://example.com/1"
                },
                new HealthEndPoint
                {
                    Identifier = Guid.Parse("fe08550c-d926-4758-808f-a9a991e0ff37"),
                    Name = "ep2",
                    Url = "https://example.com/2"
                }
            };

            var json = HealthEndPointWriter.Write(endpoints);

            Assert.That(json, Does.StartWith("["));
            Assert.That(json, Does.EndWith("]"));
            Assert.That(json, Does.Contain("ep1"));
            Assert.That(json, Does.Contain("ep2"));
        }

        [Test]
        public void Verify_that_ReadArray_returns_list_of_endpoints()
        {
            var json = """
                [
                    { "identifier": "cfb2e590-eed6-4223-b2ba-271ed0cb06da", "name": "ep1", "url": "https://example.com/1" },
                    { "identifier": "fe08550c-d926-4758-808f-a9a991e0ff37", "name": "ep2", "url": "https://example.com/2" }
                ]
                """;

            var result = HealthEndPointReader.ReadArray(json);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("ep1"));
            Assert.That(result[1].Name, Is.EqualTo("ep2"));
        }

        [Test]
        public void Verify_that_array_round_trip_preserves_all_properties()
        {
            var originals = new List<HealthEndPoint>
            {
                new HealthEndPoint
                {
                    Identifier = Guid.NewGuid(),
                    Name = "ep1",
                    Url = "https://example.com/1",
                    Frequency = 10,
                    Timeout = 2,
                    RetryCount = 1,
                    IsActive = false
                },
                new HealthEndPoint
                {
                    Identifier = Guid.NewGuid(),
                    Name = "ep2",
                    Url = "https://example.com/2",
                    Frequency = 60,
                    Timeout = 15,
                    RetryCount = 5,
                    IsActive = true
                }
            };

            var json = HealthEndPointWriter.Write(originals);
            var deserialized = HealthEndPointReader.ReadArray(json);

            Assert.That(deserialized, Has.Count.EqualTo(2));

            for (var i = 0; i < originals.Count; i++)
            {
                Assert.That(deserialized[i].Identifier, Is.EqualTo(originals[i].Identifier));
                Assert.That(deserialized[i].Name, Is.EqualTo(originals[i].Name));
                Assert.That(deserialized[i].Url, Is.EqualTo(originals[i].Url));
                Assert.That(deserialized[i].Frequency, Is.EqualTo(originals[i].Frequency));
                Assert.That(deserialized[i].Timeout, Is.EqualTo(originals[i].Timeout));
                Assert.That(deserialized[i].RetryCount, Is.EqualTo(originals[i].RetryCount));
                Assert.That(deserialized[i].IsActive, Is.EqualTo(originals[i].IsActive));
            }
        }
    }
}
