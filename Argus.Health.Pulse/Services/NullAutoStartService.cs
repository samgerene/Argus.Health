// -------------------------------------------------------------------------------------------------
//  <copyright file="NullAutoStartService.cs">
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
    /// No-op implementation of <see cref="IAutoStartService"/> for non-Windows platforms
    /// </summary>
    public class NullAutoStartService : IAutoStartService
    {
        /// <summary>
        /// Gets a value indicating whether automatic startup is currently enabled.
        /// Always returns <c>false</c> on non-Windows platforms.
        /// </summary>
        public bool IsEnabled => false;

        /// <summary>
        /// No-op on non-Windows platforms
        /// </summary>
        public void Enable()
        {
        }

        /// <summary>
        /// No-op on non-Windows platforms
        /// </summary>
        public void Disable()
        {
        }
    }
}
