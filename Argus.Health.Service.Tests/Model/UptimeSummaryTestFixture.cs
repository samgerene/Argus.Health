// -------------------------------------------------------------------------------------------------
//   <copyright file="UptimeSummaryTestFixture.cs">
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
    using System;

    using Argus.Health.Common.Model;

    /// <summary>
    /// Suite of tests for the <see cref="UptimeSummary"/> class
    /// </summary>
    [TestFixture]
    public class UptimeSummaryTestFixture
    {
        [Test]
        public void Verify_that_UptimePercent_is_zero_when_TotalChecks_is_zero()
        {
            var summary = new UptimeSummary
            {
                TotalChecks = 0,
                HealthyChecks = 0
            };

            Assert.That(summary.UptimePercent, Is.EqualTo(0.0));
        }

        [Test]
        public void Verify_that_UptimePercent_is_100_when_all_checks_healthy()
        {
            var summary = new UptimeSummary
            {
                TotalChecks = 10,
                HealthyChecks = 10
            };

            Assert.That(summary.UptimePercent, Is.EqualTo(100.0));
        }

        [Test]
        public void Verify_that_UptimePercent_is_50_when_half_healthy()
        {
            var summary = new UptimeSummary
            {
                TotalChecks = 10,
                HealthyChecks = 5
            };

            Assert.That(summary.UptimePercent, Is.EqualTo(50.0));
        }

        [Test]
        public void Verify_that_property_setters_round_trip()
        {
            var periodStart = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

            var summary = new UptimeSummary
            {
                PeriodStart = periodStart,
                TotalChecks = 120,
                HealthyChecks = 118,
                AverageResponseTimeMs = 42.7,
                MaxResponseTimeMs = 1234
            };

            Assert.That(summary.PeriodStart, Is.EqualTo(periodStart));
            Assert.That(summary.TotalChecks, Is.EqualTo(120));
            Assert.That(summary.HealthyChecks, Is.EqualTo(118));
            Assert.That(summary.AverageResponseTimeMs, Is.EqualTo(42.7));
            Assert.That(summary.MaxResponseTimeMs, Is.EqualTo(1234));
        }
    }
}
