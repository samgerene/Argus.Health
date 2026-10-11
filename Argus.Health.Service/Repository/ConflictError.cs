// -------------------------------------------------------------------------------------------------
//  <copyright file="ConflictError.cs">
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

namespace Argus.Health.Service.Repository
{
    using FluentResults;

    /// <summary>
    /// The <see cref="Error"/> of a repository operation that failed because it conflicts with data that
    /// already exists, such as a name or identifier that is already in use
    /// </summary>
    public class ConflictError : Error
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictError"/> class
        /// </summary>
        /// <param name="message">
        /// A description of the conflict
        /// </param>
        public ConflictError(string message)
            : base(message)
        {
        }
    }
}
