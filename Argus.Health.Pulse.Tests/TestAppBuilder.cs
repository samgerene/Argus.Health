// -------------------------------------------------------------------------------------------------
//  <copyright file="TestAppBuilder.cs">
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

[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(Argus.Health.Pulse.Tests.TestAppBuilder))]

namespace Argus.Health.Pulse.Tests
{
    using Avalonia;
    using Avalonia.Headless;
    using Avalonia.Themes.Fluent;

    using ReactiveUI.Avalonia;

    /// <summary>
    /// Provides a headless Avalonia application builder for unit tests
    /// </summary>
    public class TestAppBuilder
    {
        /// <summary>
        /// Builds a headless Avalonia application for testing
        /// </summary>
        /// <returns>
        /// An <see cref="AppBuilder"/> configured for headless testing
        /// </returns>
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<TestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .UseReactiveUI(_ => { });
        }
    }

    /// <summary>
    /// Minimal Avalonia application used for headless testing
    /// </summary>
    public class TestApp : Application
    {
        /// <summary>
        /// Initializes the application styles
        /// </summary>
        public override void Initialize()
        {
            this.Styles.Add(new FluentTheme());
        }
    }
}
