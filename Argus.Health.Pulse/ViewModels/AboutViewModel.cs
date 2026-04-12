// -------------------------------------------------------------------------------------------------
//  <copyright file="AboutViewModel.cs">
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

namespace Argus.Health.Pulse.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Reactive;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using ReactiveUI;

    /// <summary>
    /// View model for the About dialog window
    /// </summary>
    public class AboutViewModel : ViewModelBase
    {
        /// <summary>
        /// The repository URL
        /// </summary>
        private const string RepositoryUrl = "https://github.com/samgerene/Argus.Health";

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutViewModel"/> class
        /// </summary>
        public AboutViewModel()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            this.ApplicationName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                                   ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
                                   ?? "Argus Health Pulse";

            this.Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                           ?? assembly.GetName().Version?.ToString()
                           ?? "0.0.0";

            this.Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
                             ?? "Copyright © Sam Gerené";

            this.Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
                               ?? "Avalonia desktop dashboard for monitoring Argus Health endpoints.";

            this.OpenRepositoryCommand = ReactiveCommand.Create(this.OpenRepository);
            this.CloseCommand = ReactiveCommand.Create(() => this.CloseRequested?.Invoke(this, EventArgs.Empty));
        }

        /// <summary>
        /// Gets the application name
        /// </summary>
        public string ApplicationName { get; }

        /// <summary>
        /// Gets the application version
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the copyright notice
        /// </summary>
        public string Copyright { get; }

        /// <summary>
        /// Gets the application description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the license text
        /// </summary>
        public string LicenseText { get; } =
            "Licensed under the Apache License, Version 2.0. " +
            "You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0";

        /// <summary>
        /// Gets the command to open the repository in the default browser
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenRepositoryCommand { get; }

        /// <summary>
        /// Gets the command to close the About window
        /// </summary>
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        /// <summary>
        /// Raised when the About window should be closed
        /// </summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// Opens the repository URL in the default browser
        /// </summary>
        private void OpenRepository()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", RepositoryUrl);
            }
        }
    }
}
