// -------------------------------------------------------------------------------------------------
//  <copyright file="IncidentItemTestFixture.cs">
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
    /// Suite of tests for the <see cref="IncidentItem"/> class
    /// </summary>
    [TestFixture]
    public class IncidentItemTestFixture
    {
        [Test]
        public void Verify_that_default_EndpointName_is_empty_string()
        {
            var item = new IncidentItem();

            Assert.That(item.EndpointName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Verify_that_default_ErrorMessage_is_null()
        {
            var item = new IncidentItem();

            Assert.That(item.ErrorMessage, Is.Null);
        }

        [Test]
        public void Verify_that_property_setters_round_trip()
        {
            var timestamp = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

            var item = new IncidentItem
            {
                EndpointName = "api",
                Timestamp = timestamp,
                StatusCode = 504,
                ResponseTimeMs = 1234,
                ErrorMessage = "gateway timeout"
            };

            Assert.That(item.EndpointName, Is.EqualTo("api"));
            Assert.That(item.Timestamp, Is.EqualTo(timestamp));
            Assert.That(item.StatusCode, Is.EqualTo(504));
            Assert.That(item.ResponseTimeMs, Is.EqualTo(1234));
            Assert.That(item.ErrorMessage, Is.EqualTo("gateway timeout"));
        }
    }
}
