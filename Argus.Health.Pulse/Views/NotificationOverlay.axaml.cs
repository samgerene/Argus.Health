// -------------------------------------------------------------------------------------------------
//  <copyright file="NotificationOverlay.axaml.cs">
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
    using System.Reactive.Disposables.Fluent;

    using Argus.Health.Pulse.ViewModels;

    using ReactiveUI;
    using ReactiveUI.Avalonia;

    /// <summary>
    /// Overlay control for displaying toast notifications
    /// </summary>
    public partial class NotificationOverlay : ReactiveUserControl<MainWindowViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationOverlay"/> class
        /// </summary>
        public NotificationOverlay()
        {
            this.InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(this.ViewModel, vm => vm.Notifications, v => v.NotificationsItemsControl.ItemsSource)
                    .DisposeWith(disposables);
            });
        }
    }
}
