// -------------------------------------------------------------------------------------------------
//  <copyright file="NullToastNotificationServiceTestFixture.cs">
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

namespace Argus.Health.Pulse.Tests.Services
{
    using System;

    using Argus.Health.Pulse.Services;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="NullToastNotificationService"/> class
    /// </summary>
    [TestFixture]
    public class NullToastNotificationServiceTestFixture
    {
        [Test]
        public void Verify_that_ShowEndpointFailure_does_not_throw()
        {
            var service = new NullToastNotificationService();

            Assert.DoesNotThrow(() => service.ShowEndpointFailure("api", 503, "service unavailable"));
            Assert.DoesNotThrow(() => service.ShowEndpointFailure("api", 0, null));
        }

        [Test]
        public void Verify_that_Dispose_does_not_throw()
        {
            var service = new NullToastNotificationService();

            Assert.DoesNotThrow(() => service.Dispose());
        }

        [Test]
        public void Verify_that_ToastActivated_event_can_be_subscribed_and_unsubscribed_without_throwing()
        {
            var service = new NullToastNotificationService();

            EventHandler handler = (_, _) => { };

            Assert.DoesNotThrow(() =>
            {
                service.ToastActivated += handler;
                service.ToastActivated -= handler;
            });
        }
    }
}
