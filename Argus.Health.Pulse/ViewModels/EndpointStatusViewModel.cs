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
    using ReactiveUI.Fody.Helpers;
    
    /// <summary>
    /// Represents a single endpoint row on the dashboard
    /// </summary>
    public class EndpointStatusViewModel : ViewModelBase
    {
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

        public Guid Identifier { get; }

        public string Name { get; }

        public string Url { get; }

        [Reactive]
        public int StatusCode { get; set; }

        [Reactive]
        public DateTime? LastChecked { get; set; }

        [Reactive]
        public string? ErrorMessage { get; set; }

        private readonly ObservableAsPropertyHelper<bool> isHealthyHelper;
        public bool IsHealthy => isHealthyHelper.Value;

        private readonly ObservableAsPropertyHelper<string> statusDisplayHelper;
        public string StatusDisplay => statusDisplayHelper.Value;
    }
}
