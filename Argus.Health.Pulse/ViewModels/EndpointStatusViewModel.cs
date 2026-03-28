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
    using System.Reactive.Linq;

    using ReactiveUI;
    using ReactiveUI.SourceGenerators;

    /// <summary>
    /// Represents a single endpoint row on the dashboard
    /// </summary>
    public partial class EndpointStatusViewModel : ViewModelBase
    {
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
            Identifier = identifier;
            Name = name;
            Url = url;

            isHealthyHelper = this.WhenAnyValue(x => x.StatusCode)
                .Select(code => code >= 200 && code < 300)
                .ToProperty(this, x => x.IsHealthy);

            statusDisplayHelper = this.WhenAnyValue(x => x.StatusCode, x => x.ErrorMessage)
                .Select(t =>
                {
                    if (t.Item1 == 0 && t.Item2 != null)
                    {
                        return t.Item2;
                    }

                    return t.Item1 == 0 ? "Pending" : t.Item1.ToString();
                })
                .ToProperty(this, x => x.StatusDisplay);
        }

        /// <summary>
        /// Gets the unique identifier of the endpoint
        /// </summary>
        public Guid Identifier { get; }

        /// <summary>
        /// Gets the human-readable name of the endpoint
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the URL of the endpoint
        /// </summary>
        public string Url { get; }

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

        private readonly ObservableAsPropertyHelper<bool> isHealthyHelper;

        /// <summary>
        /// Gets a value indicating whether the endpoint is healthy (status code 200–299)
        /// </summary>
        public bool IsHealthy => isHealthyHelper.Value;

        private readonly ObservableAsPropertyHelper<string> statusDisplayHelper;

        /// <summary>
        /// Gets the display text for the endpoint status
        /// </summary>
        public string StatusDisplay => statusDisplayHelper.Value;
    }
}
