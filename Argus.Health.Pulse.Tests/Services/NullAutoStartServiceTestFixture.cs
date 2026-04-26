// -------------------------------------------------------------------------------------------------
//  <copyright file="NullAutoStartServiceTestFixture.cs">
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
    using Argus.Health.Pulse.Services;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="NullAutoStartService"/> class
    /// </summary>
    [TestFixture]
    public class NullAutoStartServiceTestFixture
    {
        [Test]
        public void Verify_that_IsEnabled_is_false()
        {
            var service = new NullAutoStartService();

            Assert.That(service.IsEnabled, Is.False);
        }

        [Test]
        public void Verify_that_Enable_does_not_throw()
        {
            var service = new NullAutoStartService();

            Assert.DoesNotThrow(() => service.Enable());
        }

        [Test]
        public void Verify_that_Disable_does_not_throw()
        {
            var service = new NullAutoStartService();

            Assert.DoesNotThrow(() => service.Disable());
        }
    }
}
