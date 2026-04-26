// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeDaysRangeTestFixture.cs">
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
    using Argus.Health.Pulse.ViewModels;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="UptimeDaysRange"/> enum and <see cref="UptimeDaysRangeExtensions"/>
    /// </summary>
    [TestFixture]
    public class UptimeDaysRangeTestFixture
    {
        [Test]
        public void Verify_that_ToDays_returns_expected_value_for_each_member()
        {
            Assert.That(UptimeDaysRange.Last7Days.ToDays(), Is.EqualTo(7));
            Assert.That(UptimeDaysRange.Last30Days.ToDays(), Is.EqualTo(30));
            Assert.That(UptimeDaysRange.Last60Days.ToDays(), Is.EqualTo(60));
            Assert.That(UptimeDaysRange.Last90Days.ToDays(), Is.EqualTo(90));
        }

        [Test]
        public void Verify_that_ToDays_falls_back_to_90_for_unknown_value()
        {
            var unknown = (UptimeDaysRange)999;

            Assert.That(unknown.ToDays(), Is.EqualTo(90));
        }

        [Test]
        public void Verify_that_ToDisplayString_returns_expected_label_for_each_member()
        {
            Assert.That(UptimeDaysRange.Last7Days.ToDisplayString(), Is.EqualTo("7d"));
            Assert.That(UptimeDaysRange.Last30Days.ToDisplayString(), Is.EqualTo("30d"));
            Assert.That(UptimeDaysRange.Last60Days.ToDisplayString(), Is.EqualTo("60d"));
            Assert.That(UptimeDaysRange.Last90Days.ToDisplayString(), Is.EqualTo("90d"));
        }

        [Test]
        public void Verify_that_ToDisplayString_falls_back_to_90d_for_unknown_value()
        {
            var unknown = (UptimeDaysRange)999;

            Assert.That(unknown.ToDisplayString(), Is.EqualTo("90d"));
        }
    }
}
