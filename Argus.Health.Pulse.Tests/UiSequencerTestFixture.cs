// -------------------------------------------------------------------------------------------------
//  <copyright file="UiSequencerTestFixture.cs">
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

namespace Argus.Health.Pulse.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Avalonia.Headless.NUnit;
    using Avalonia.Threading;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="UiSequencer"/> class
    /// </summary>
    [TestFixture]
    public class UiSequencerTestFixture
    {
        [AvaloniaTest]
        public void Verify_that_RunAsync_runs_the_action_at_once_when_called_on_the_UI_thread()
        {
            var hasRun = false;

            var task = UiSequencer.RunAsync(() => hasRun = true);

            Assert.That(hasRun, Is.True);
            Assert.That(task.IsCompletedSuccessfully, Is.True);
        }

        [AvaloniaTest]
        public async Task Verify_that_RunAsync_runs_the_action_on_the_UI_thread_when_called_on_another_thread()
        {
            var uiThread = Thread.CurrentThread.ManagedThreadId;
            var callingThread = uiThread;
            var actionThread = -1;
            var hadAccess = false;

            await Task.Run(async () =>
            {
                callingThread = Thread.CurrentThread.ManagedThreadId;

                await UiSequencer.RunAsync(() =>
                {
                    actionThread = Thread.CurrentThread.ManagedThreadId;
                    hadAccess = Dispatcher.UIThread.CheckAccess();
                });
            });

            Assert.That(callingThread, Is.Not.EqualTo(uiThread));
            Assert.That(actionThread, Is.EqualTo(uiThread));
            Assert.That(hadAccess, Is.True);
        }

        [AvaloniaTest]
        public async Task Verify_that_RunAsync_passes_on_the_exception_of_an_action_that_ran_on_the_UI_thread()
        {
            var task = Task.Run(() => UiSequencer.RunAsync(() => throw new InvalidOperationException("failed on the UI thread")));

            await Assert.ThatAsync(async () => await task, Throws.InvalidOperationException.With.Message.EqualTo("failed on the UI thread"));
        }

        [AvaloniaTest]
        public void Verify_that_RunAsync_rejects_a_null_action()
        {
            Assert.That(() => UiSequencer.RunAsync(null), Throws.ArgumentNullException);
        }
    }
}
