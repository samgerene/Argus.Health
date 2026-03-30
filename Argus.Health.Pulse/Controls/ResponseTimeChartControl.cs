// -------------------------------------------------------------------------------------------------
//  <copyright file="ResponseTimeChartControl.cs">
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
    using System.Collections.Generic;
    using System.Globalization;

    using Argus.Health.Common.Model;

    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// Renders a response time line chart for the detail panel with hoverable dots
    /// </summary>
    public class ResponseTimeChartControl : ChartControlBase
    {
        /// <summary>
        /// Initializes static members of the <see cref="ResponseTimeChartControl"/> class
        /// </summary>
        static ResponseTimeChartControl()
        {
            AffectsRender<ResponseTimeChartControl>(DataProperty);
        }

        /// <summary>
        /// Renders grid lines with Y-axis labels, connecting lines between points, and colored dots
        /// </summary>
        /// <param name="context">The drawing context</param>
        /// <param name="data">The check result data</param>
        /// <param name="points">Pre-calculated screen positions for each data point</param>
        /// <param name="chartWidth">The drawable chart width</param>
        /// <param name="chartHeight">The drawable chart height</param>
        /// <param name="maxMs">The maximum response time in the data</param>
        /// <param name="gridPen">The pen to use for grid lines</param>
        protected override void RenderChart(
            DrawingContext context,
            IReadOnlyList<HealthEndPointCheckResult> data,
            List<Point> points,
            double chartWidth,
            double chartHeight,
            long maxMs,
            Pen gridPen)
        {
            // Draw grid lines with Y-axis labels
            var gridLines = 4;

            for (var i = 0; i <= gridLines; i++)
            {
                var y = this.TopPadding + chartHeight * i / gridLines;
                context.DrawLine(gridPen, new Point(this.LeftMargin, y), new Point(this.LeftMargin + chartWidth, y));

                var labelValue = maxMs * (gridLines - i) / gridLines;
                var label = new FormattedText($"{labelValue}ms", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default, this.LabelFontSize, DashboardColors.Gray);

                context.DrawText(label, new Point(this.LeftMargin - label.Width - 4, y - label.Height / 2));
            }

            // Draw line segments between consecutive points
            var linePen = new Pen(DashboardColors.Teal, 1.5);

            for (var i = 0; i < points.Count - 1; i++)
            {
                context.DrawLine(linePen, points[i], points[i + 1]);
            }

            // Draw dots at every point
            for (var i = 0; i < data.Count; i++)
            {
                this.DrawDot(context, data[i], points[i]);
            }
        }
    }
}
