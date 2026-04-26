// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointCheckResultExtensionsTestFixture.cs">
//
//     Copyright (c) 2025-2026 Sam Gerené
//
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
//
//         http://www.apache.org/licenses/LICENSE-2.0
//
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.
//
//   </copyright>
//   ------------------------------------------------------------------------------------------------

namespace Argus.Health.Service.Tests.Model
{
    using Argus.Health.Common.Model;

    /// <summary>
    /// Suite of tests for the <see cref="HealthEndPointCheckResultExtensions"/> class
    /// </summary>
    [TestFixture]
    public class HealthEndPointCheckResultExtensionsTestFixture
    {
        [Test]
        public void Verify_that_IsHealthy_returns_true_for_2xx_status_code()
        {
            Assert.That(200.IsHealthy(), Is.True);
            Assert.That(201.IsHealthy(), Is.True);
            Assert.That(250.IsHealthy(), Is.True);
            Assert.That(299.IsHealthy(), Is.True);
        }

        [Test]
        public void Verify_that_IsHealthy_returns_false_below_200()
        {
            Assert.That(0.IsHealthy(), Is.False);
            Assert.That(100.IsHealthy(), Is.False);
            Assert.That(199.IsHealthy(), Is.False);
        }

        [Test]
        public void Verify_that_IsHealthy_returns_false_at_or_above_300()
        {
            Assert.That(300.IsHealthy(), Is.False);
            Assert.That(404.IsHealthy(), Is.False);
            Assert.That(500.IsHealthy(), Is.False);
        }

        [Test]
        public void Verify_that_IsHealthy_extension_on_check_result_delegates_to_status_code()
        {
            var healthy = new HealthEndPointCheckResult { StatusCode = 200 };
            var unhealthy = new HealthEndPointCheckResult { StatusCode = 503 };

            Assert.That(healthy.IsHealthy(), Is.True);
            Assert.That(unhealthy.IsHealthy(), Is.False);
        }
    }
}
