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
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Argus.Health.Common.Model;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media;

    /// <summary>
    /// Renders a response time line chart for the detail panel
    /// </summary>
    public class ResponseTimeChartControl : Control
    {
        /// <summary>
        /// Left margin for Y-axis labels
        /// </summary>
        private const double LeftMargin = 50;

        /// <summary>
        /// Bottom margin for X-axis labels
        /// </summary>
        private const double BottomMargin = 20;

        /// <summary>
        /// Top padding
        /// </summary>
        private const double TopPadding = 10;

        /// <summary>
        /// Right padding
        /// </summary>
        private const double RightPadding = 10;

        /// <summary>
        /// Defines the <see cref="Data"/> styled property
        /// </summary>
        public static readonly StyledProperty<IReadOnlyList<HealthEndPointCheckResult>?> DataProperty =
            AvaloniaProperty.Register<ResponseTimeChartControl, IReadOnlyList<HealthEndPointCheckResult>?>(nameof(Data));

        /// <summary>
        /// Initializes static members of the <see cref="ResponseTimeChartControl"/> class
        /// </summary>
        static ResponseTimeChartControl()
        {
            AffectsRender<ResponseTimeChartControl>(DataProperty);
        }

        /// <summary>
        /// Gets or sets the check results to plot
        /// </summary>
        public IReadOnlyList<HealthEndPointCheckResult>? Data
        {
            get => this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
        }

        /// <summary>
        /// Renders the response time chart
        /// </summary>
        /// <param name="context">The drawing context</param>
        public override void Render(DrawingContext context)
        {
            var data = this.Data;

            if (data == null || data.Count == 0)
            {
                var noData = new FormattedText("No data", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default, 12, DashboardColors.Gray);

                context.DrawText(noData, new Point(this.Bounds.Width / 2 - noData.Width / 2, this.Bounds.Height / 2));
                return;
            }

            var chartWidth = this.Bounds.Width - LeftMargin - RightPadding;
            var chartHeight = this.Bounds.Height - TopPadding - BottomMargin;

            if (chartWidth <= 0 || chartHeight <= 0)
            {
                return;
            }

            var maxMs = data.Max(r => r.ResponseTimeMs);

            if (maxMs == 0)
            {
                maxMs = 1;
            }

            // Draw grid lines
            var gridPen = new Pen(new SolidColorBrush(Color.Parse("#333333")), 0.5);
            var gridLines = 4;

            for (var i = 0; i <= gridLines; i++)
            {
                var y = TopPadding + chartHeight * i / gridLines;
                context.DrawLine(gridPen, new Point(LeftMargin, y), new Point(LeftMargin + chartWidth, y));

                var labelValue = maxMs * (gridLines - i) / gridLines;
                var label = new FormattedText($"{labelValue}ms", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default, 10, DashboardColors.Gray);

                context.DrawText(label, new Point(LeftMargin - label.Width - 4, y - label.Height / 2));
            }

            // Draw data points
            var minTime = data[0].Timestamp;
            var maxTime = data[^1].Timestamp;
            var timeRange = (maxTime - minTime).TotalSeconds;

            if (timeRange <= 0)
            {
                timeRange = 1;
            }

            var geometry = new StreamGeometry();
            var failurePoints = new List<Point>();

            using (var ctx = geometry.Open())
            {
                var started = false;

                for (var i = 0; i < data.Count; i++)
                {
                    var x = LeftMargin + chartWidth * (data[i].Timestamp - minTime).TotalSeconds / timeRange;
                    var y = TopPadding + chartHeight - chartHeight * data[i].ResponseTimeMs / maxMs;
                    var point = new Point(x, y);

                    if (!started)
                    {
                        ctx.BeginFigure(point, false);
                        started = true;
                    }
                    else
                    {
                        ctx.LineTo(point);
                    }

                    if (data[i].StatusCode < 200 || data[i].StatusCode >= 300)
                    {
                        failurePoints.Add(point);
                    }
                }
            }

            var linePen = new Pen(DashboardColors.Teal, 1.5);
            context.DrawGeometry(null, linePen, geometry);

            // Draw failure markers
            foreach (var point in failurePoints)
            {
                context.DrawEllipse(DashboardColors.Red, null, point, 3, 3);
            }

            // Draw X-axis time labels
            if (data.Count > 1)
            {
                var startLabel = new FormattedText(minTime.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, DashboardColors.Gray);

                context.DrawText(startLabel, new Point(LeftMargin, TopPadding + chartHeight + 4));

                var endLabel = new FormattedText(maxTime.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, DashboardColors.Gray);

                context.DrawText(endLabel, new Point(LeftMargin + chartWidth - endLabel.Width, TopPadding + chartHeight + 4));
            }
        }
    }
}
