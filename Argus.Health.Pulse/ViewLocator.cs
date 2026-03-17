// -------------------------------------------------------------------------------------------------
//  <copyright file="ViewLocator.cs">
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

namespace Argus.Health.Pulse
{
    using System;

    using Avalonia.Controls;
    using Avalonia.Controls.Templates;

    using Argus.Health.Pulse.ViewModels;
    
    /// <summary>
    /// Resolves views for view models by convention, replacing "ViewModel" with "View" in the type name
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        /// <summary>
        /// Builds a view for the given view model data object
        /// </summary>
        /// <param name="data">
        /// The view model to resolve a view for
        /// </param>
        /// <returns>
        /// The resolved <see cref="Control"/>, or a <see cref="TextBlock"/> with a "Not Found" message
        /// </returns>
        public Control? Build(object? data)
        {
            if (data is null)
            {
                return null;
            }

            var name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        /// <summary>
        /// Determines whether this data template can be applied to the given data object
        /// </summary>
        /// <param name="data">
        /// The data object to check
        /// </param>
        /// <returns>
        /// <c>true</c> if the data object is a <see cref="ViewModelBase"/>; otherwise <c>false</c>
        /// </returns>
        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
