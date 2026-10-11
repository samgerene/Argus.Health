// -------------------------------------------------------------------------------------------------
//  <copyright file="NotFoundError.cs">
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
    /// The <see cref="Error"/> of a repository operation that failed because the subject of the operation
    /// does not exist
    /// </summary>
    public class NotFoundError : Error
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundError"/> class
        /// </summary>
        /// <param name="message">
        /// A description of what was not found
        /// </param>
        public NotFoundError(string message)
            : base(message)
        {
        }
    }
}
