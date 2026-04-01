// -------------------------------------------------------------------------------------------------
//  <copyright file="NullToastNotificationService.cs">
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

    /// <summary>
    /// No-op implementation of <see cref="IToastNotificationService"/> for non-Windows platforms
    /// </summary>
    public class NullToastNotificationService : IToastNotificationService
    {
        /// <summary>
        /// Raised when the user clicks on a toast notification. Never raised in this implementation.
        /// </summary>
#pragma warning disable CS0067 // Event is never used in no-op implementation
        public event EventHandler? ToastActivated;
#pragma warning restore CS0067

        /// <summary>
        /// No-op on non-Windows platforms
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
        }

        /// <summary>
        /// No-op disposal
        /// </summary>
        public void Dispose()
        {
        }
    }
}
