// -------------------------------------------------------------------------------------------------
//  <copyright file="ArgusHealthOptions.cs">
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

namespace Argus.Health.Service
{
    /// <summary>
    /// Configuration options for the Argus Health service
    /// </summary>
    public class ArgusHealthOptions
    {
        /// <summary>
        /// Gets or sets the name of the named pipe for IPC communication
        /// </summary>
        public string PipeName { get; set; } = "ArgusHealth";

        /// <summary>
        /// Gets or sets the SQLite database file name
        /// </summary>
        public string DatabaseFileName { get; set; } = "ArgusHealth.sqlite";
    }
}
