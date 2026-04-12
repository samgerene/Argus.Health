// -------------------------------------------------------------------------------------------------
//  <copyright file="AboutWindow.axaml.cs">
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
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Disposables.Fluent;

    using Argus.Health.Pulse.ViewModels;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media.Imaging;

    using ReactiveUI;
    using ReactiveUI.Avalonia;

    /// <summary>
    /// About dialog window displaying application details, license, and copyright
    /// </summary>
    public partial class AboutWindow : ReactiveWindow<AboutViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutWindow"/> class
        /// </summary>
        public AboutWindow()
        {
            this.InitializeComponent();

            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "argus-health-pulse-icon.ico");

            if (File.Exists(iconPath))
            {
                this.AppIcon.Source = new Bitmap(iconPath);
            }

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(this.ViewModel, vm => vm.ApplicationName, v => v.AppNameText.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.Version, v => v.VersionText.Text,
                        version => $"Version {version}")
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.Description, v => v.DescriptionText.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.Copyright, v => v.CopyrightText.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.LicenseText, v => v.LicenseText.Text)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.OpenRepositoryCommand, v => v.RepositoryButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.CloseCommand, v => v.CloseButton)
                    .DisposeWith(disposables);

                this.ViewModel!.CloseRequested += (_, _) => this.Close();
            });
        }
    }
}
