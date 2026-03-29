// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointStatusViewModel.cs">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using Argus.Health.Common.Model;

    using ReactiveUI;
    using ReactiveUI.SourceGenerators;

    /// <summary>
    /// Represents a single endpoint row on the dashboard
    /// </summary>
    public partial class EndpointStatusViewModel : ViewModelBase
    {
        /// <summary>
        /// Maximum number of historical check results to retain (~7 days at 30s interval)
        /// </summary>
        private const int MaxHistorySize = 20160;

        /// <summary>
        /// Maximum number of recent response times for sparkline display
        /// </summary>
        private const int SparklineSize = 20;

        /// <summary>
        /// Backing list for historical check results
        /// </summary>
        private readonly List<HealthEndPointCheckResult> history = new();

        /// <summary>
        /// Helper for the <see cref="IsHealthy"/> computed property
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> isHealthyHelper;

        /// <summary>
        /// Helper for the <see cref="StatusDisplay"/> computed property
        /// </summary>
        private readonly ObservableAsPropertyHelper<string> statusDisplayHelper;

        /// <summary>
        /// Helper for the <see cref="RecentResponseTimes"/> computed property
        /// </summary>
        private readonly ObservableAsPropertyHelper<IReadOnlyList<long>> recentResponseTimesHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointStatusViewModel"/> class
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier of the endpoint
        /// </param>
        /// <param name="name">
        /// The human-readable name of the endpoint
        /// </param>
        /// <param name="url">
        /// The URL of the endpoint
        /// </param>
        public EndpointStatusViewModel(Guid identifier, string name, string url)
        {
            this.Identifier = identifier;
            this.Name = name;
            this.Url = url;

            this.isHealthyHelper = this.WhenAnyValue(x => x.StatusCode)
                .Select(code => code >= 200 && code < 300)
                .ToProperty(this, x => x.IsHealthy);

            this.statusDisplayHelper = this.WhenAnyValue(x => x.StatusCode, x => x.ErrorMessage)
                .Select(t =>
                {
                    if (t.Item1 == 0 && t.Item2 != null)
                    {
                        return t.Item2;
                    }

                    return t.Item1 == 0 ? "Pending" : t.Item1.ToString();
                })
                .ToProperty(this, x => x.StatusDisplay);

            this.recentResponseTimesHelper = this.WhenAnyValue(x => x.ResponseTimeMs)
                .Select(_ => this.history
                    .Skip(Math.Max(0, this.history.Count - SparklineSize))
                    .Select(r => r.ResponseTimeMs)
                    .ToList() as IReadOnlyList<long>)
                .ToProperty(this, x => x.RecentResponseTimes);
        }

        /// <summary>
        /// Gets the unique identifier of the endpoint
        /// </summary>
        public Guid Identifier { get; }

        /// <summary>
        /// Gets or sets the human-readable name of the endpoint
        /// </summary>
        [Reactive]
        public partial string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL of the endpoint
        /// </summary>
        [Reactive]
        public partial string Url { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code from the last health check
        /// </summary>
        [Reactive]
        public partial int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last health check
        /// </summary>
        [Reactive]
        public partial DateTime? LastChecked { get; set; }

        /// <summary>
        /// Gets or sets the error message from the last health check
        /// </summary>
        [Reactive]
        public partial string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the response time in milliseconds from the last health check
        /// </summary>
        [Reactive]
        public partial long ResponseTimeMs { get; set; }

        /// <summary>
        /// Gets a value indicating whether the endpoint is healthy (status code 200-299)
        /// </summary>
        public bool IsHealthy => this.isHealthyHelper.Value;

        /// <summary>
        /// Gets the display text for the endpoint status
        /// </summary>
        public string StatusDisplay => this.statusDisplayHelper.Value;

        /// <summary>
        /// Gets the most recent response times for sparkline display (last 20 values)
        /// </summary>
        public IReadOnlyList<long> RecentResponseTimes => this.recentResponseTimesHelper.Value;

        /// <summary>
        /// Gets the historical check results for this endpoint
        /// </summary>
        public IReadOnlyList<HealthEndPointCheckResult> History => this.history;

        /// <summary>
        /// Appends a check result to the history and updates reactive properties
        /// </summary>
        /// <param name="result">The <see cref="HealthEndPointCheckResult"/> to add</param>
        public void AddCheckResult(HealthEndPointCheckResult result)
        {
            this.history.Add(result);

            if (this.history.Count > MaxHistorySize)
            {
                this.history.RemoveAt(0);
            }

            this.StatusCode = result.StatusCode;
            this.LastChecked = result.Timestamp;
            this.ErrorMessage = result.ErrorMessage;
            this.ResponseTimeMs = result.ResponseTimeMs;
        }
    }
}
