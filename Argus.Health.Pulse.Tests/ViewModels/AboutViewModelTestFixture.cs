// -------------------------------------------------------------------------------------------------
//  <copyright file="AboutViewModelTestFixture.cs">
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
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    using Argus.Health.Pulse.ViewModels;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="AboutViewModel"/> class
    /// </summary>
    [TestFixture]
    public class AboutViewModelTestFixture
    {
        private AboutViewModel viewModel;

        [SetUp]
        public void SetUp()
        {
            this.viewModel = new AboutViewModel();
        }

        [Test]
        public void Verify_that_ApplicationName_is_populated()
        {
            Assert.That(this.viewModel.ApplicationName, Is.Not.Null);
            Assert.That(this.viewModel.ApplicationName, Is.Not.Empty);
        }

        [Test]
        public void Verify_that_Version_is_populated()
        {
            Assert.That(this.viewModel.Version, Is.Not.Null);
            Assert.That(this.viewModel.Version, Is.Not.Empty);
        }

        [Test]
        public void Verify_that_Copyright_is_populated()
        {
            Assert.That(this.viewModel.Copyright, Is.Not.Null);
            Assert.That(this.viewModel.Copyright, Is.Not.Empty);
        }

        [Test]
        public void Verify_that_Description_is_populated()
        {
            Assert.That(this.viewModel.Description, Is.Not.Null);
            Assert.That(this.viewModel.Description, Is.Not.Empty);
        }

        [Test]
        public void Verify_that_LicenseText_references_Apache_License()
        {
            Assert.That(this.viewModel.LicenseText, Is.Not.Null);
            Assert.That(this.viewModel.LicenseText, Does.Contain("Apache License"));
            Assert.That(this.viewModel.LicenseText, Does.Contain("2.0"));
        }

        [Test]
        public void Verify_that_commands_are_initialized()
        {
            Assert.That(this.viewModel.OpenRepositoryCommand, Is.Not.Null);
            Assert.That(this.viewModel.CloseCommand, Is.Not.Null);
        }

        [Test]
        public async Task Verify_that_CloseCommand_raises_CloseRequested_event()
        {
            var eventRaised = false;
            object raisedSender = null;

            this.viewModel.CloseRequested += (sender, args) =>
            {
                eventRaised = true;
                raisedSender = sender;
            };

            await this.viewModel.CloseCommand.Execute().FirstAsync();

            Assert.That(eventRaised, Is.True);
            Assert.That(raisedSender, Is.SameAs(this.viewModel));
        }

        [Test]
        public async Task Verify_that_CloseCommand_does_not_throw_when_no_subscribers()
        {
            Assert.DoesNotThrowAsync(async () => await this.viewModel.CloseCommand.Execute().FirstAsync());
        }

        [Test]
        public async Task Verify_that_OpenRepositoryCommand_does_not_throw()
        {
            // Only execute on non-Windows/non-Linux platforms where the method is a no-op,
            // to avoid spawning a real browser during automated test runs.
            // On Windows/Linux, only verify the command is executable and skip invocation.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.That(this.viewModel.OpenRepositoryCommand.CanExecute.FirstAsync().Wait(), Is.True);
                return;
            }

            Assert.DoesNotThrowAsync(async () => await this.viewModel.OpenRepositoryCommand.Execute().FirstAsync());
        }
    }
}
