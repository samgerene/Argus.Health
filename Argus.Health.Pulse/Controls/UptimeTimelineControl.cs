// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeTimelineControl.cs">
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
    using Argus.Health.Pulse.ViewModels;

    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// Renders a response time scatter chart with colored dots per check result and hover tooltips
    /// </summary>
    public class UptimeTimelineControl : ChartControlBase
    {
        /// <summary>
        /// Defines the <see cref="SelectedTimeRange"/> styled property
        /// </summary>
        public static readonly StyledProperty<TimeRange> SelectedTimeRangeProperty =
            AvaloniaProperty.Register<UptimeTimelineControl, TimeRange>(nameof(SelectedTimeRange));

        /// <summary>
        /// Initializes static members of the <see cref="UptimeTimelineControl"/> class
        /// </summary>
        static UptimeTimelineControl()
        {
            AffectsRender<UptimeTimelineControl>(DataProperty, SelectedTimeRangeProperty);
        }

        /// <summary>
        /// Gets or sets the selected time range
        /// </summary>
        public TimeRange SelectedTimeRange
        {
            get => this.GetValue(SelectedTimeRangeProperty);
            set => this.SetValue(SelectedTimeRangeProperty, value);
        }

        /// <summary>
        /// Gets the left margin for Y-axis labels
        /// </summary>
        protected override double LeftMargin => 40;

        /// <summary>
        /// Gets the bottom margin for X-axis labels
        /// </summary>
        protected override double BottomMargin => 16;

        /// <summary>
        /// Gets the top padding
        /// </summary>
        protected override double TopPadding => 6;

        /// <summary>
        /// Gets the right padding
        /// </summary>
        protected override double RightPadding => 6;

        /// <summary>
        /// Gets the font size used for axis labels
        /// </summary>
        protected override double LabelFontSize => 9;

        /// <summary>
        /// Renders top and bottom grid lines with max/zero labels, and colored dots
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
            // Draw top grid line with max label
            var maxLabel = new FormattedText($"{maxMs}ms", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, this.LabelFontSize, DashboardColors.Gray);

            context.DrawText(maxLabel, new Point(this.LeftMargin - maxLabel.Width - 3, this.TopPadding - maxLabel.Height / 2));
            context.DrawLine(gridPen, new Point(this.LeftMargin, this.TopPadding), new Point(this.LeftMargin + chartWidth, this.TopPadding));

            // Draw bottom grid line with zero label
            var zeroLabel = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, this.LabelFontSize, DashboardColors.Gray);

            context.DrawText(zeroLabel, new Point(this.LeftMargin - zeroLabel.Width - 3, this.TopPadding + chartHeight - zeroLabel.Height / 2));
            context.DrawLine(gridPen, new Point(this.LeftMargin, this.TopPadding + chartHeight), new Point(this.LeftMargin + chartWidth, this.TopPadding + chartHeight));

            // Draw dots per result
            for (var i = 0; i < data.Count; i++)
            {
                this.DrawDot(context, data[i], points[i]);
            }
        }
    }
}
