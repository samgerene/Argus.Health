// -------------------------------------------------------------------------------------------------
//  <copyright file="SparklineControl.cs">
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
    using System.Collections.Generic;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media;

    /// <summary>
    /// Renders an inline sparkline chart of response times
    /// </summary>
    public class SparklineControl : Control
    {
        /// <summary>
        /// Defines the <see cref="Data"/> styled property
        /// </summary>
        public static readonly StyledProperty<IReadOnlyList<long>?> DataProperty =
            AvaloniaProperty.Register<SparklineControl, IReadOnlyList<long>?>(nameof(Data));

        /// <summary>
        /// Defines the <see cref="LineBrush"/> styled property
        /// </summary>
        public static readonly StyledProperty<IBrush> LineBrushProperty =
            AvaloniaProperty.Register<SparklineControl, IBrush>(nameof(LineBrush), DashboardColors.Teal);

        /// <summary>
        /// Initializes static members of the <see cref="SparklineControl"/> class
        /// </summary>
        static SparklineControl()
        {
            AffectsRender<SparklineControl>(DataProperty, LineBrushProperty);
        }

        /// <summary>
        /// Gets or sets the response time data points
        /// </summary>
        public IReadOnlyList<long>? Data
        {
            get => this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used to draw the sparkline
        /// </summary>
        public IBrush LineBrush
        {
            get => this.GetValue(LineBrushProperty);
            set => this.SetValue(LineBrushProperty, value);
        }

        /// <summary>
        /// Renders the sparkline chart
        /// </summary>
        /// <param name="context">The drawing context</param>
        public override void Render(DrawingContext context)
        {
            var data = this.Data;

            if (data == null || data.Count < 2)
            {
                return;
            }

            var width = this.Bounds.Width;
            var height = this.Bounds.Height;
            var padding = 2.0;

            long min = long.MaxValue;
            long max = long.MinValue;

            for (var i = 0; i < data.Count; i++)
            {
                if (data[i] < min)
                {
                    min = data[i];
                }

                if (data[i] > max)
                {
                    max = data[i];
                }
            }

            var range = max - min;

            if (range == 0)
            {
                range = 1;
            }

            var xStep = (width - padding * 2) / (data.Count - 1);
            var yScale = (height - padding * 2) / range;

            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                var firstY = height - padding - (data[0] - min) * yScale;
                ctx.BeginFigure(new Point(padding, firstY), false);

                for (var i = 1; i < data.Count; i++)
                {
                    var x = padding + i * xStep;
                    var y = height - padding - (data[i] - min) * yScale;
                    ctx.LineTo(new Point(x, y));
                }
            }

            var pen = new Pen(this.LineBrush, 1.5);
            context.DrawGeometry(null, pen, geometry);
        }
    }
}
