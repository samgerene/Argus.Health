// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointCheckResultTestFixture.cs">
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

namespace Argus.Health.Service.Tests.Model
{
    using System;

    using Argus.Health.Common.Model;

    /// <summary>
    /// Suite of tests for the <see cref="HealthEndPointCheckResult"/> class
    /// </summary>
    [TestFixture]
    public class HealthEndPointCheckResultTestFixture
    {
        [Test]
        public void Verify_that_properties_can_be_set_and_read()
        {
            var identifier = Guid.NewGuid();
            var healthEndPointId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;

            var result = new HealthEndPointCheckResult
            {
                Identifier = identifier,
                Timestamp = timestamp,
                StatusCode = 200,
                ErrorMessage = "some error",
                HealthEndPoint = healthEndPointId
            };

            Assert.That(result.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Timestamp, Is.EqualTo(timestamp));
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.ErrorMessage, Is.EqualTo("some error"));
            Assert.That(result.HealthEndPoint, Is.EqualTo(healthEndPointId));
        }

        [Test]
        public void Verify_that_default_values_are_set()
        {
            var result = new HealthEndPointCheckResult();

            Assert.That(result.Identifier, Is.EqualTo(Guid.Empty));
            Assert.That(result.StatusCode, Is.EqualTo(0));
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.HealthEndPoint, Is.EqualTo(Guid.Empty));
            Assert.That(result.Timestamp, Is.Not.EqualTo(default(DateTime)));
        }
    }
}
