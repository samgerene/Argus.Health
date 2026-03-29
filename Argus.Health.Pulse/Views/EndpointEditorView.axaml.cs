// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointEditorView.axaml.cs">
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
    using System.Reactive.Disposables;
    using System.Reactive.Disposables.Fluent;

    using Argus.Health.Pulse.ViewModels;

    using ReactiveUI;
    using ReactiveUI.Avalonia;

    /// <summary>
    /// View for the endpoint create/edit form
    /// </summary>
    public partial class EndpointEditorView : ReactiveUserControl<EndpointEditorViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointEditorView"/> class
        /// </summary>
        public EndpointEditorView()
        {
            this.InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.TitleText.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.ErrorMessage, v => v.ErrorMessageText.Text)
                    .DisposeWith(disposables);

                this.OneWayBind(this.ViewModel, vm => vm.ErrorMessage, v => v.ErrorMessageText.IsVisible,
                        message => !string.IsNullOrEmpty(message))
                    .DisposeWith(disposables);

                this.Bind(this.ViewModel, vm => vm.Name, v => v.NameTextBox.Text)
                    .DisposeWith(disposables);

                this.Bind(this.ViewModel, vm => vm.Url, v => v.UrlTextBox.Text)
                    .DisposeWith(disposables);

                this.Bind(this.ViewModel, vm => vm.Frequency, v => v.FrequencyUpDown.Value)
                    .DisposeWith(disposables);

                this.Bind(this.ViewModel, vm => vm.Timeout, v => v.TimeoutUpDown.Value)
                    .DisposeWith(disposables);

                this.Bind(this.ViewModel, vm => vm.RetryCount, v => v.RetryCountUpDown.Value)
                    .DisposeWith(disposables);

                this.Bind(this.ViewModel, vm => vm.IsActive, v => v.IsActiveCheckBox.IsChecked)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.SaveCommand, v => v.SaveButton)
                    .DisposeWith(disposables);

                this.BindCommand(this.ViewModel, vm => vm.CancelCommand, v => v.CancelButton)
                    .DisposeWith(disposables);
            });
        }
    }
}
