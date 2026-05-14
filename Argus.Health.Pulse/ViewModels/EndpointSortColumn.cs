// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointSortColumn.cs">
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
    /// <summary>
    /// Identifies which column the dashboard endpoint table is sorted by
    /// </summary>
    public enum EndpointSortColumn
    {
        /// <summary>
        /// Sort by the endpoint name (alphabetical, case-insensitive)
        /// </summary>
        Name,

        /// <summary>
        /// Sort by the endpoint URL (alphabetical, case-insensitive)
        /// </summary>
        Url,

        /// <summary>
        /// Sort by the HTTP status code from the latest health check (numerical)
        /// </summary>
        Status,

        /// <summary>
        /// Sort by the response time in milliseconds from the latest health check (numerical)
        /// </summary>
        ResponseTime,

        /// <summary>
        /// Sort by the timestamp of the latest health check (chronological, nulls last)
        /// </summary>
        LastChecked,

        /// <summary>
        /// Sort by the error message from the latest health check (alphabetical, nulls last)
        /// </summary>
        ErrorMessage
    }
}
