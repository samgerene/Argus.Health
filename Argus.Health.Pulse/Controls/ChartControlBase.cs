// -------------------------------------------------------------------------------------------------
//  <copyright file="ChartControlBase.cs">
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
    using Avalonia.Input;
    using Avalonia.Media;

    /// <summary>
    /// Base class for chart controls that render health check result data with hoverable dots
    /// </summary>
    public abstract class ChartControlBase : Control
    {
        /// <summary>
        /// Radius of the dot drawn per result
        /// </summary>
        protected const double DotRadius = 3;

        /// <summary>
        /// Hit test distance threshold for tooltip activation
        /// </summary>
        private const double HitThreshold = 8;

        /// <summary>
        /// Defines the <see cref="Data"/> styled property
        /// </summary>
        public static readonly StyledProperty<IReadOnlyList<HealthEndPointCheckResult>?> DataProperty =
            AvaloniaProperty.Register<ChartControlBase, IReadOnlyList<HealthEndPointCheckResult>?>(nameof(Data));

        /// <summary>
        /// Cached dot positions and associated results for hit testing
        /// </summary>
        private readonly List<(Point Position, HealthEndPointCheckResult Result)> dotPositions = new();

        /// <summary>
        /// Gets or sets the check results to plot
        /// </summary>
        public IReadOnlyList<HealthEndPointCheckResult>? Data
        {
            get => this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
        }

        /// <summary>
        /// Gets the left margin for Y-axis labels
        /// </summary>
        protected virtual double LeftMargin => 50;

        /// <summary>
        /// Gets the bottom margin for X-axis labels
        /// </summary>
        protected virtual double BottomMargin => 20;

        /// <summary>
        /// Gets the top padding
        /// </summary>
        protected virtual double TopPadding => 10;

        /// <summary>
        /// Gets the right padding
        /// </summary>
        protected virtual double RightPadding => 10;

        /// <summary>
        /// Gets the font size used for axis labels
        /// </summary>
        protected virtual double LabelFontSize => 10;

        /// <summary>
        /// Renders the chart by orchestrating shared layout, then delegating to <see cref="RenderChart"/>
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

            var chartWidth = this.Bounds.Width - this.LeftMargin - this.RightPadding;
            var chartHeight = this.Bounds.Height - this.TopPadding - this.BottomMargin;

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

            // Calculate all screen points
            var points = new List<Point>(data.Count);

            for (var i = 0; i < data.Count; i++)
            {
                var x = this.LeftMargin + chartWidth * (data[i].Timestamp - minTime).TotalSeconds / timeRange;
                var y = this.TopPadding + chartHeight - chartHeight * data[i].ResponseTimeMs / maxMs;
                points.Add(new Point(x, y));
            }

            var gridPen = new Pen(new SolidColorBrush(Color.Parse("#333333")), 0.5);

            this.RenderChart(context, data, points, chartWidth, chartHeight, maxMs, gridPen);

            // Draw X-axis time labels
            if (data.Count > 1)
            {
                var startLabel = new FormattedText(minTime.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, this.LabelFontSize, DashboardColors.Gray);

                context.DrawText(startLabel, new Point(this.LeftMargin, this.TopPadding + chartHeight + 2));

                var endLabel = new FormattedText(maxTime.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, this.LabelFontSize, DashboardColors.Gray);

                context.DrawText(endLabel, new Point(this.LeftMargin + chartWidth - endLabel.Width, this.TopPadding + chartHeight + 2));
            }
        }

        /// <summary>
        /// Renders the chart-specific content (grid lines, data points, connecting lines)
        /// </summary>
        /// <param name="context">The drawing context</param>
        /// <param name="data">The check result data</param>
        /// <param name="points">Pre-calculated screen positions for each data point</param>
        /// <param name="chartWidth">The drawable chart width</param>
        /// <param name="chartHeight">The drawable chart height</param>
        /// <param name="maxMs">The maximum response time in the data</param>
        /// <param name="gridPen">The pen to use for grid lines</param>
        protected abstract void RenderChart(
            DrawingContext context,
            IReadOnlyList<HealthEndPointCheckResult> data,
            List<Point> points,
            double chartWidth,
            double chartHeight,
            long maxMs,
            Pen gridPen);

        /// <summary>
        /// Draws a colored dot for the given check result and caches its position for hit testing
        /// </summary>
        /// <param name="context">The drawing context</param>
        /// <param name="result">The check result</param>
        /// <param name="point">The screen position</param>
        protected void DrawDot(DrawingContext context, HealthEndPointCheckResult result, Point point)
        {
            var brush = ChartControlBase.GetDotBrush(result);
            context.DrawEllipse(brush, null, point, DotRadius, DotRadius);
            this.dotPositions.Add((point, result));
        }

        /// <summary>
        /// Returns the brush color for a check result dot based on status code
        /// </summary>
        /// <param name="result">The check result</param>
        /// <returns>Green for healthy, red for connection failure, amber for other errors</returns>
        private static IBrush GetDotBrush(HealthEndPointCheckResult result)
        {
            if (result.IsHealthy())
            {
                return DashboardColors.Green;
            }

            return result.StatusCode == 0 ? DashboardColors.Red : DashboardColors.Amber;
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
                var timeText = nearest.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);

                var tooltip = nearest.ErrorMessage != null
                    ? $"{timeText} — {nearest.StatusCode} ({nearest.ResponseTimeMs}ms)\n{nearest.ErrorMessage}"
                    : $"{timeText} — {nearest.StatusCode} ({nearest.ResponseTimeMs}ms)";

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
