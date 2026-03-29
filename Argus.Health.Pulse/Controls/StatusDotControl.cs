// -------------------------------------------------------------------------------------------------
//  <copyright file="StatusDotControl.cs">
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

namespace Argus.Health.Pulse.Controls
{
    using System;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media;

    /// <summary>
    /// Renders a colored status indicator dot
    /// </summary>
    public class StatusDotControl : Control
    {
        /// <summary>
        /// Defines the <see cref="IsHealthy"/> styled property
        /// </summary>
        public static readonly StyledProperty<bool> IsHealthyProperty =
            AvaloniaProperty.Register<StatusDotControl, bool>(nameof(IsHealthy));

        /// <summary>
        /// Defines the <see cref="StatusCode"/> styled property
        /// </summary>
        public static readonly StyledProperty<int> StatusCodeProperty =
            AvaloniaProperty.Register<StatusDotControl, int>(nameof(StatusCode));

        /// <summary>
        /// Initializes static members of the <see cref="StatusDotControl"/> class
        /// </summary>
        static StatusDotControl()
        {
            AffectsRender<StatusDotControl>(IsHealthyProperty, StatusCodeProperty);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the endpoint is healthy
        /// </summary>
        public bool IsHealthy
        {
            get => this.GetValue(IsHealthyProperty);
            set => this.SetValue(IsHealthyProperty, value);
        }

        /// <summary>
        /// Gets or sets the HTTP status code
        /// </summary>
        public int StatusCode
        {
            get => this.GetValue(StatusCodeProperty);
            set => this.SetValue(StatusCodeProperty, value);
        }

        /// <summary>
        /// Renders the status dot
        /// </summary>
        /// <param name="context">The drawing context</param>
        public override void Render(DrawingContext context)
        {
            var brush = this.StatusCode == 0
                ? DashboardColors.Gray
                : this.IsHealthy
                    ? DashboardColors.Green
                    : DashboardColors.Red;

            var size = Math.Min(this.Bounds.Width, this.Bounds.Height);
            var radius = size / 2.0 - 1;
            var center = new Point(this.Bounds.Width / 2.0, this.Bounds.Height / 2.0);

            context.DrawEllipse(brush, null, center, radius, radius);
        }
    }
}
