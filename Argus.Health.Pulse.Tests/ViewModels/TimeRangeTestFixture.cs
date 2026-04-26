// -------------------------------------------------------------------------------------------------
//  <copyright file="TimeRangeTestFixture.cs">
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

namespace Argus.Health.Pulse.Tests.ViewModels
{
    using System;

    using Argus.Health.Pulse.ViewModels;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="TimeRange"/> enum and <see cref="TimeRangeExtensions"/>
    /// </summary>
    [TestFixture]
    public class TimeRangeTestFixture
    {
        [Test]
        public void Verify_that_ToTimeSpan_returns_one_hour_for_LastHour()
        {
            Assert.That(TimeRange.LastHour.ToTimeSpan(), Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void Verify_that_ToTimeSpan_returns_six_hours_for_Last6Hours()
        {
            Assert.That(TimeRange.Last6Hours.ToTimeSpan(), Is.EqualTo(TimeSpan.FromHours(6)));
        }

        [Test]
        public void Verify_that_ToTimeSpan_returns_24_hours_for_Last24Hours()
        {
            Assert.That(TimeRange.Last24Hours.ToTimeSpan(), Is.EqualTo(TimeSpan.FromHours(24)));
        }

        [Test]
        public void Verify_that_ToTimeSpan_returns_seven_days_for_Last7Days()
        {
            Assert.That(TimeRange.Last7Days.ToTimeSpan(), Is.EqualTo(TimeSpan.FromDays(7)));
        }

        [Test]
        public void Verify_that_ToTimeSpan_returns_30_days_for_Last30Days()
        {
            Assert.That(TimeRange.Last30Days.ToTimeSpan(), Is.EqualTo(TimeSpan.FromDays(30)));
        }

        [Test]
        public void Verify_that_ToTimeSpan_falls_back_to_one_hour_for_unknown_value()
        {
            var unknown = (TimeRange)999;

            Assert.That(unknown.ToTimeSpan(), Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void Verify_that_ToDisplayString_returns_expected_label_for_each_value()
        {
            Assert.That(TimeRange.LastHour.ToDisplayString(), Is.EqualTo("1h"));
            Assert.That(TimeRange.Last6Hours.ToDisplayString(), Is.EqualTo("6h"));
            Assert.That(TimeRange.Last24Hours.ToDisplayString(), Is.EqualTo("24h"));
            Assert.That(TimeRange.Last7Days.ToDisplayString(), Is.EqualTo("7d"));
            Assert.That(TimeRange.Last30Days.ToDisplayString(), Is.EqualTo("30d"));
        }

        [Test]
        public void Verify_that_ToDisplayString_falls_back_for_unknown_value()
        {
            var unknown = (TimeRange)999;

            Assert.That(unknown.ToDisplayString(), Is.EqualTo("1h"));
        }
    }
}
