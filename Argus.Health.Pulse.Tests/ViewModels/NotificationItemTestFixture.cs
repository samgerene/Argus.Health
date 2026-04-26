// -------------------------------------------------------------------------------------------------
//  <copyright file="NotificationItemTestFixture.cs">
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
    /// Suite of tests for the <see cref="NotificationItem"/> class
    /// </summary>
    [TestFixture]
    public class NotificationItemTestFixture
    {
        [Test]
        public void Verify_that_default_Id_is_a_non_empty_Guid()
        {
            var item = new NotificationItem();

            Assert.That(item.Id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Verify_that_default_Timestamp_is_close_to_utc_now()
        {
            var before = DateTime.UtcNow.AddSeconds(-5);
            var item = new NotificationItem();
            var after = DateTime.UtcNow.AddSeconds(5);

            Assert.That(item.Timestamp, Is.GreaterThanOrEqualTo(before));
            Assert.That(item.Timestamp, Is.LessThanOrEqualTo(after));
        }

        [Test]
        public void Verify_that_default_EndpointName_is_empty_string()
        {
            var item = new NotificationItem();

            Assert.That(item.EndpointName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Verify_that_default_ErrorMessage_is_null()
        {
            var item = new NotificationItem();

            Assert.That(item.ErrorMessage, Is.Null);
        }

        [Test]
        public void Verify_that_property_setters_round_trip()
        {
            var id = Guid.NewGuid();
            var timestamp = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

            var item = new NotificationItem
            {
                Id = id,
                EndpointName = "api",
                StatusCode = 503,
                ErrorMessage = "service unavailable",
                Timestamp = timestamp
            };

            Assert.That(item.Id, Is.EqualTo(id));
            Assert.That(item.EndpointName, Is.EqualTo("api"));
            Assert.That(item.StatusCode, Is.EqualTo(503));
            Assert.That(item.ErrorMessage, Is.EqualTo("service unavailable"));
            Assert.That(item.Timestamp, Is.EqualTo(timestamp));
        }
    }
}
