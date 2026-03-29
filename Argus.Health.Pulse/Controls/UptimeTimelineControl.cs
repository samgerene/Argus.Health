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
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.ViewModels;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Media;

    /// <summary>
    /// Renders a response time line chart with colored dots per check result and hover tooltips
    /// </summary>
    public class UptimeTimelineControl : Control
    {
        /// <summary>
        /// Left margin for Y-axis labels
        /// </summary>
        private const double LeftMargin = 40;

        /// <summary>
        /// Bottom margin for X-axis labels
        /// </summary>
        private const double BottomMargin = 16;

        /// <summary>
        /// Top padding
        /// </summary>
        private const double TopPadding = 6;

        /// <summary>
        /// Right padding
        /// </summary>
        private const double RightPadding = 6;

        /// <summary>
        /// Radius of the dot drawn per result
        /// </summary>
        private const double DotRadius = 3;

        /// <summary>
        /// Hit test distance threshold for tooltip activation
        /// </summary>
        private const double HitThreshold = 8;

        /// <summary>
        /// Defines the <see cref="Data"/> styled property
        /// </summary>
        public static readonly StyledProperty<IReadOnlyList<HealthEndPointCheckResult>?> DataProperty =
            AvaloniaProperty.Register<UptimeTimelineControl, IReadOnlyList<HealthEndPointCheckResult>?>(nameof(Data));

        /// <summary>
        /// Defines the <see cref="SelectedTimeRange"/> styled property
        /// </summary>
        public static readonly StyledProperty<TimeRange> SelectedTimeRangeProperty =
            AvaloniaProperty.Register<UptimeTimelineControl, TimeRange>(nameof(SelectedTimeRange));

        /// <summary>
        /// Cached dot positions and associated results for hit testing
        /// </summary>
        private readonly List<(Point Position, HealthEndPointCheckResult Result)> dotPositions = new();

        /// <summary>
        /// Initializes static members of the <see cref="UptimeTimelineControl"/> class
        /// </summary>
        static UptimeTimelineControl()
        {
            AffectsRender<UptimeTimelineControl>(DataProperty, SelectedTimeRangeProperty);
        }

        /// <summary>
        /// Gets or sets the check results to display
        /// </summary>
        public IReadOnlyList<HealthEndPointCheckResult>? Data
        {
            get => this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
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
        /// Renders the response time line chart with colored dots
        /// </summary>
        /// <param name="context">The drawing context</param>
        public override void Render(DrawingContext context)
        {
            this.dotPositions.Clear();

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

            var minTime = data[0].Timestamp;
            var maxTime = data[^1].Timestamp;
            var timeRange = (maxTime - minTime).TotalSeconds;

            if (timeRange <= 0)
            {
                timeRange = 1;
            }

            // Draw Y-axis grid lines
            var gridPen = new Pen(new SolidColorBrush(Color.Parse("#333333")), 0.5);

            var maxLabel = new FormattedText($"{maxMs}ms", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 9, DashboardColors.Gray);

            context.DrawText(maxLabel, new Point(LeftMargin - maxLabel.Width - 3, TopPadding - maxLabel.Height / 2));
            context.DrawLine(gridPen, new Point(LeftMargin, TopPadding), new Point(LeftMargin + chartWidth, TopPadding));

            var zeroLabel = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, 9, DashboardColors.Gray);

            context.DrawText(zeroLabel, new Point(LeftMargin - zeroLabel.Width - 3, TopPadding + chartHeight - zeroLabel.Height / 2));
            context.DrawLine(gridPen, new Point(LeftMargin, TopPadding + chartHeight), new Point(LeftMargin + chartWidth, TopPadding + chartHeight));

            // Draw dots per result
            for (var i = 0; i < data.Count; i++)
            {
                var result = data[i];
                var point = this.CalculatePoint(result, minTime, timeRange, maxMs, chartWidth, chartHeight);

                IBrush brush;

                if (result.StatusCode >= 200 && result.StatusCode < 300)
                {
                    brush = DashboardColors.Green;
                }
                else if (result.StatusCode == 0)
                {
                    brush = DashboardColors.Red;
                }
                else
                {
                    brush = DashboardColors.Amber;
                }

                context.DrawEllipse(brush, null, point, DotRadius, DotRadius);

                this.dotPositions.Add((point, result));
            }

            // Draw X-axis time labels
            var startTimeLabel = new FormattedText(minTime.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture),
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 9, DashboardColors.Gray);

            context.DrawText(startTimeLabel, new Point(LeftMargin, TopPadding + chartHeight + 2));

            var endTimeLabel = new FormattedText(maxTime.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture),
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 9, DashboardColors.Gray);

            context.DrawText(endTimeLabel, new Point(LeftMargin + chartWidth - endTimeLabel.Width, TopPadding + chartHeight + 2));
        }

        /// <summary>
        /// Handles pointer movement to show tooltips on nearby dots
        /// </summary>
        /// <param name="e">The pointer event args</param>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            var position = e.GetPosition(this);
            var nearest = this.FindNearestDot(position);

            if (nearest != null)
            {
                var result = nearest;
                var timeText = result.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);

                var tooltip = result.ErrorMessage != null
                    ? $"{timeText} — {result.StatusCode} ({result.ResponseTimeMs}ms)\n{result.ErrorMessage}"
                    : $"{timeText} — {result.StatusCode} ({result.ResponseTimeMs}ms)";

                ToolTip.SetTip(this, tooltip);
                ToolTip.SetIsOpen(this, true);
            }
            else
            {
                ToolTip.SetIsOpen(this, false);
                ToolTip.SetTip(this, null);
            }
        }

        /// <summary>
        /// Clears the tooltip when the pointer exits the control
        /// </summary>
        /// <param name="e">The pointer event args</param>
        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            ToolTip.SetIsOpen(this, false);
            ToolTip.SetTip(this, null);
        }

        /// <summary>
        /// Calculates the screen position for a check result
        /// </summary>
        /// <param name="result">The check result</param>
        /// <param name="minTime">The earliest timestamp in the data</param>
        /// <param name="timeRange">The total time range in seconds</param>
        /// <param name="maxMs">The maximum response time in the data</param>
        /// <param name="chartWidth">The drawable chart width</param>
        /// <param name="chartHeight">The drawable chart height</param>
        /// <returns>The screen point for the result</returns>
        private Point CalculatePoint(HealthEndPointCheckResult result, DateTime minTime, double timeRange, long maxMs, double chartWidth, double chartHeight)
        {
            var x = LeftMargin + chartWidth * (result.Timestamp - minTime).TotalSeconds / timeRange;
            var y = TopPadding + chartHeight - chartHeight * result.ResponseTimeMs / maxMs;

            return new Point(x, y);
        }

        /// <summary>
        /// Finds the nearest dot to the given position within the hit threshold
        /// </summary>
        /// <param name="position">The pointer position</param>
        /// <returns>The nearest check result, or null if none within threshold</returns>
        private HealthEndPointCheckResult? FindNearestDot(Point position)
        {
            HealthEndPointCheckResult? nearest = null;
            var minDistance = HitThreshold;

            foreach (var (dotPoint, result) in this.dotPositions)
            {
                var dx = position.X - dotPoint.X;
                var dy = position.Y - dotPoint.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = result;
                }
            }

            return nearest;
        }
    }
}
