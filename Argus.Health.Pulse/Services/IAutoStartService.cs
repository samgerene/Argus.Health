// -------------------------------------------------------------------------------------------------
//  <copyright file="IAutoStartService.cs">
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
    /// <summary>
    /// Manages automatic startup registration for the application
    /// </summary>
    public interface IAutoStartService
    {
        /// <summary>
        /// Gets a value indicating whether automatic startup is currently enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Enables automatic startup so the application launches on user login
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables automatic startup
        /// </summary>
        void Disable();
    }
}
