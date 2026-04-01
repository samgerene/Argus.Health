// -------------------------------------------------------------------------------------------------
//  <copyright file="WindowsToastNotificationService.cs">
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
    using System.Collections.Generic;
    using System.Runtime.Versioning;

    using Microsoft.Extensions.Logging;
    using Microsoft.Toolkit.Uwp.Notifications;

    /// <summary>
    /// Shows Windows toast notifications for endpoint health failures
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsToastNotificationService : IToastNotificationService
    {
        /// <summary>
        /// The minimum interval between duplicate notifications for the same endpoint
        /// </summary>
        private static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The <see cref="ILogger{WindowsToastNotificationService}"/> used for logging
        /// </summary>
        private readonly ILogger<WindowsToastNotificationService> logger;

        /// <summary>
        /// Tracks the last notification time per endpoint name for throttling
        /// </summary>
        private readonly Dictionary<string, DateTime> lastNotificationTime = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsToastNotificationService"/> class
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{WindowsToastNotificationService}"/> used for logging
        /// </param>
        public WindowsToastNotificationService(ILogger<WindowsToastNotificationService> logger)
        {
            this.logger = logger;

            ToastNotificationManagerCompat.OnActivated += this.OnToastActivated;
        }

        /// <summary>
        /// Raised when the user clicks on a toast notification
        /// </summary>
        public event EventHandler? ToastActivated;

        /// <summary>
        /// Shows a toast notification for a failed endpoint health check
        /// </summary>
        /// <param name="endpointName">
        /// The display name of the endpoint that failed
        /// </param>
        /// <param name="statusCode">
        /// The HTTP status code returned by the health check
        /// </param>
        /// <param name="errorMessage">
        /// An optional error message describing the failure
        /// </param>
        public void ShowEndpointFailure(string endpointName, int statusCode, string? errorMessage)
        {
            if (this.IsThrottled(endpointName))
            {
                this.logger.LogDebug("Toast notification throttled for endpoint {EndpointName}", endpointName);
                return;
            }

            try
            {
                var builder = new ToastContentBuilder()
                    .AddArgument("action", "show")
                    .AddText($"Endpoint Down: {endpointName}")
                    .AddText($"Status {statusCode}");

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    builder.AddText(errorMessage);
                }

                builder.Show();

                this.lastNotificationTime[endpointName] = DateTime.UtcNow;
                this.logger.LogInformation("Toast notification shown for endpoint {EndpointName} (status {StatusCode})", endpointName, statusCode);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to show toast notification for endpoint {EndpointName}", endpointName);
            }
        }

        /// <summary>
        /// Disposes managed resources and cleans up toast notification registrations
        /// </summary>
        public void Dispose()
        {
            ToastNotificationManagerCompat.OnActivated -= this.OnToastActivated;

            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to clear toast notification history");
            }
        }

        /// <summary>
        /// Determines whether notifications for the specified endpoint are currently throttled
        /// </summary>
        /// <param name="endpointName">
        /// The endpoint name to check
        /// </param>
        /// <returns>
        /// <c>true</c> if a notification was shown for this endpoint within the throttle interval
        /// </returns>
        private bool IsThrottled(string endpointName)
        {
            if (this.lastNotificationTime.TryGetValue(endpointName, out var lastTime))
            {
                return DateTime.UtcNow - lastTime < ThrottleInterval;
            }

            return false;
        }

        /// <summary>
        /// Handles toast activation when the user clicks a notification
        /// </summary>
        /// <param name="e">
        /// The toast activation event arguments
        /// </param>
        private void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            this.ToastActivated?.Invoke(this, EventArgs.Empty);
        }
    }
}
