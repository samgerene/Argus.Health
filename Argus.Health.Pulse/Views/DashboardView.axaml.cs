// -------------------------------------------------------------------------------------------------
//  <copyright file="DashboardView.axaml.cs">
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

namespace Argus.Health.Pulse.Views
{
    using Argus.Health.Pulse.ViewModels;

    using ReactiveUI.Avalonia;

    /// <summary>
    /// Dashboard view displaying real-time endpoint health status
    /// </summary>
    public partial class DashboardView : ReactiveUserControl<DashboardViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardView"/> class
        /// </summary>
        public DashboardView()
        {
            InitializeComponent();
        }
    }
}
