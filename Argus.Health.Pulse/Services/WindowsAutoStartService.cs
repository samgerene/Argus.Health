// -------------------------------------------------------------------------------------------------
//  <copyright file="WindowsAutoStartService.cs">
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

namespace Argus.Health.Pulse.Services
{
    using System;
    using System.Runtime.Versioning;

    using Microsoft.Extensions.Logging;
    using Microsoft.Win32;

    /// <summary>
    /// Manages automatic startup registration via the Windows registry
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsAutoStartService : IAutoStartService
    {
        /// <summary>
        /// The registry key path for user login startup entries
        /// </summary>
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// The registry value name used for this application
        /// </summary>
        private const string ValueName = "ArgusHealthPulse";

        /// <summary>
        /// The <see cref="ILogger{WindowsAutoStartService}"/> used for logging
        /// </summary>
        private readonly ILogger<WindowsAutoStartService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsAutoStartService"/> class
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{WindowsAutoStartService}"/> used for logging
        /// </param>
        public WindowsAutoStartService(ILogger<WindowsAutoStartService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets a value indicating whether automatic startup is currently enabled
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                    return key?.GetValue(ValueName) != null;
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to read autostart registry key");
                    return false;
                }
            }
        }

        /// <summary>
        /// Enables automatic startup so the application launches on user login
        /// </summary>
        public void Enable()
        {
            try
            {
                var executablePath = Environment.ProcessPath;

                if (string.IsNullOrEmpty(executablePath))
                {
                    this.logger.LogWarning("Cannot enable autostart: executable path is not available");
                    return;
                }

                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);

                if (key == null)
                {
                    this.logger.LogWarning("Cannot enable autostart: registry key {KeyPath} not found", RunKeyPath);
                    return;
                }

                var value = $"\"{executablePath}\" --minimized";
                key.SetValue(ValueName, value);
                this.logger.LogInformation("Autostart enabled: {Value}", value);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to enable autostart");
            }
        }

        /// <summary>
        /// Disables automatic startup
        /// </summary>
        public void Disable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                key?.DeleteValue(ValueName, false);
                this.logger.LogInformation("Autostart disabled");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to disable autostart");
            }
        }
    }
}
