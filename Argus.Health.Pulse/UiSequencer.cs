// -------------------------------------------------------------------------------------------------
//  <copyright file="UiSequencer.cs">
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

namespace Argus.Health.Pulse
{
    using System;
    using System.Threading.Tasks;

    using Avalonia.Threading;

    using ReactiveUI.Primitives.Concurrency;

    /// <summary>
    /// Provides the <see cref="ISequencer"/> that runs work on the UI thread
    /// </summary>
    public static class UiSequencer
    {
        /// <summary>
        /// Gets the <see cref="ISequencer"/> of the current <see cref="Dispatcher.UIThread"/>. Use this
        /// instead of <see cref="AvaloniaScheduler.Instance"/>, which stays bound to the dispatcher that
        /// existed when it was first used: every headless test has a dispatcher of its own, so work
        /// scheduled on that instance never runs in a later test
        /// </summary>
        public static ISequencer Current => AvaloniaScheduler.For(Dispatcher.UIThread);

        /// <summary>
        /// Runs an action on the UI thread. Use it in an asynchronous method to apply what was awaited to
        /// view models, so that this does not depend on the thread on which the method resumes
        /// </summary>
        /// <param name="action">
        /// The action to run
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the action has run and fails when the action throws.
        /// When called on the UI thread the action runs at once, before this method returns
        /// </returns>
        public static Task RunAsync(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (Dispatcher.UIThread.CheckAccess())
            {
                action();

                return Task.CompletedTask;
            }

            return Dispatcher.UIThread.InvokeAsync(action).GetTask();
        }
    }
}
