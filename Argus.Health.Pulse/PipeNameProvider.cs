// -------------------------------------------------------------------------------------------------
//  <copyright file="PipeNameProvider.cs">
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
    /// <summary>
    /// Holds the named-pipe identifier the running Pulse instance is connected to.
    /// </summary>
    public class PipeNameProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipeNameProvider"/> class.
        /// </summary>
        /// <param name="pipeName">
        /// The named pipe Pulse uses to talk to the Argus Health service.
        /// </param>
        public PipeNameProvider(string pipeName)
        {
            this.PipeName = pipeName;
        }

        /// <summary>
        /// Gets the named pipe Pulse is connected to.
        /// </summary>
        public string PipeName { get; }
    }
}
