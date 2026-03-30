// -------------------------------------------------------------------------------------------------
//  <copyright file="HealthEndPointCheckResultExtensions.cs">
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

namespace Argus.Health.Common.Model
{
    /// <summary>
    /// Extension methods for <see cref="HealthEndPointCheckResult"/> and status code evaluation
    /// </summary>
    public static class HealthEndPointCheckResultExtensions
    {
        /// <summary>
        /// Determines whether the check result represents a healthy response (HTTP 2xx)
        /// </summary>
        /// <param name="result">The check result to evaluate</param>
        /// <returns><c>true</c> if the status code is in the 200–299 range; otherwise <c>false</c></returns>
        public static bool IsHealthy(this HealthEndPointCheckResult result)
        {
            return result.StatusCode.IsHealthy();
        }

        /// <summary>
        /// Determines whether the HTTP status code represents a healthy response (HTTP 2xx)
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate</param>
        /// <returns><c>true</c> if the status code is in the 200–299 range; otherwise <c>false</c></returns>
        public static bool IsHealthy(this int statusCode)
        {
            return statusCode >= 200 && statusCode < 300;
        }
    }
}
