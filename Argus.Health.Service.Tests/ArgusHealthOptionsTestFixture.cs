// -------------------------------------------------------------------------------------------------
//   <copyright file="ArgusHealthOptionsTestFixture.cs">
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

namespace Argus.Health.Service.Tests
{
    using Argus.Health.Service;

    /// <summary>
    /// Suite of tests for the <see cref="ArgusHealthOptions"/> class
    /// </summary>
    [TestFixture]
    public class ArgusHealthOptionsTestFixture
    {
        [Test]
        public void Verify_that_default_PipeName_is_ArgusHealth()
        {
            var options = new ArgusHealthOptions();

            Assert.That(options.PipeName, Is.EqualTo("ArgusHealth"));
        }

        [Test]
        public void Verify_that_default_DatabaseFileName_is_ArgusHealth_sqlite()
        {
            var options = new ArgusHealthOptions();

            Assert.That(options.DatabaseFileName, Is.EqualTo("ArgusHealth.sqlite"));
        }

        [Test]
        public void Verify_that_PipeName_can_be_overridden()
        {
            var options = new ArgusHealthOptions
            {
                PipeName = "ArgusHealth-Dev"
            };

            Assert.That(options.PipeName, Is.EqualTo("ArgusHealth-Dev"));
        }

        [Test]
        public void Verify_that_DatabaseFileName_can_be_overridden()
        {
            var options = new ArgusHealthOptions
            {
                DatabaseFileName = "Custom.sqlite"
            };

            Assert.That(options.DatabaseFileName, Is.EqualTo("Custom.sqlite"));
        }
    }
}
