// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeStatusBarControl.cs">
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

    using Argus.Health.Common.Model;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Media;

    /// <summary>
    /// Renders a GitHub/Atlassian-style uptime status bar where each segment represents
    /// one time window, colored by uptime status
    /// </summary>
    public class UptimeStatusBarControl : Control
    {
        /// <summary>
        /// Gap between segments in pixels
        /// </summary>
        private const double SegmentGap = 1;

        /// <summary>
        /// Bottom margin for date labels
        /// </summary>
        private const double BottomMargin = 16;

        /// <summary>
        /// Hit test distance threshold for tooltip activation
        /// </summary>
        private const double HitThreshold = 2;

        /// <summary>
        /// Defines the <see cref="Data"/> styled property
        /// </summary>
        public static readonly StyledProperty<IReadOnlyList<UptimeSummary>?> DataProperty =
            AvaloniaProperty.Register<UptimeStatusBarControl, IReadOnlyList<UptimeSummary>?>(nameof(Data));

        /// <summary>
        /// Cached segment positions and associated summaries for hit testing
        /// </summary>
        private readonly List<(Rect Bounds, UptimeSummary Summary)> segmentPositions = new();

        /// <summary>
        /// Initializes static members of the <see cref="UptimeStatusBarControl"/> class
        /// </summary>
        static UptimeStatusBarControl()
        {
            AffectsRender<UptimeStatusBarControl>(DataProperty);
        }

        /// <summary>
        /// Gets or sets the uptime summary data to display
        /// </summary>
        public IReadOnlyList<UptimeSummary>? Data
        {
            get => this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
        }

        /// <summary>
        /// Renders the uptime status bar with colored segments
        /// </summary>
        /// <param name="context">The drawing context</param>
        public override void Render(DrawingContext context)
        {
            this.segmentPositions.Clear();

            var data = this.Data;

            if (data == null || data.Count == 0)
            {
                var noData = new FormattedText("No uptime data", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default, 12, DashboardColors.Gray);

                context.DrawText(noData, new Point(this.Bounds.Width / 2 - noData.Width / 2, this.Bounds.Height / 2));
                return;
            }

            var barHeight = this.Bounds.Height - BottomMargin;

            if (barHeight <= 0)
            {
                return;
            }

            var totalGap = SegmentGap * (data.Count - 1);
            var segmentWidth = (this.Bounds.Width - totalGap) / data.Count;

            if (segmentWidth < 1)
            {
                segmentWidth = 1;
            }

            for (var i = 0; i < data.Count; i++)
            {
                var summary = data[i];
                var x = i * (segmentWidth + SegmentGap);
                var rect = new Rect(x, 0, segmentWidth, barHeight);

                var brush = UptimeStatusBarControl.GetSegmentBrush(summary);
                context.FillRectangle(brush, rect, 1);

                this.segmentPositions.Add((rect, summary));
            }

            // Draw date labels at start and end
            if (data.Count > 1)
            {
                var startLabel = new FormattedText(
                    data[0].PeriodStart.ToLocalTime().ToString("MMM dd", CultureInfo.CurrentCulture),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 9, DashboardColors.Gray);

                context.DrawText(startLabel, new Point(0, barHeight + 2));

                var endLabel = new FormattedText(
                    data[^1].PeriodStart.ToLocalTime().ToString("MMM dd", CultureInfo.CurrentCulture),
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 9, DashboardColors.Gray);

                context.DrawText(endLabel, new Point(this.Bounds.Width - endLabel.Width, barHeight + 2));
            }
        }

        /// <summary>
        /// Handles pointer movement to show tooltips on hovered segments
        /// </summary>
        /// <param name="e">The pointer event args</param>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            var position = e.GetPosition(this);
            var nearest = this.FindSegment(position);

            if (nearest != null)
            {
                var timeText = nearest.PeriodStart.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
                var uptimeText = nearest.TotalChecks > 0
                    ? $"{nearest.UptimePercent:F1}%"
                    : "No data";

                var tooltip = $"{timeText}\nUptime: {uptimeText}\nAvg: {nearest.AverageResponseTimeMs:F0}ms\nChecks: {nearest.TotalChecks}";

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
        /// Returns the brush color for a segment based on its uptime percentage
        /// </summary>
        /// <param name="summary">The uptime summary for the segment</param>
        /// <returns>Green for 100%, amber for partial, red for 0%, gray for no data</returns>
        private static IBrush GetSegmentBrush(UptimeSummary summary)
        {
            if (summary.TotalChecks == 0)
            {
                return DashboardColors.Gray;
            }

            if (summary.HealthyChecks == summary.TotalChecks)
            {
                return DashboardColors.Green;
            }

            return summary.HealthyChecks == 0 ? DashboardColors.Red : DashboardColors.Amber;
        }

        /// <summary>
        /// Finds the segment at the given pointer position
        /// </summary>
        /// <param name="position">The pointer position</param>
        /// <returns>The uptime summary at the position, or null if none found</returns>
        private UptimeSummary? FindSegment(Point position)
        {
            foreach (var (bounds, summary) in this.segmentPositions)
            {
                var inflated = bounds.Inflate(HitThreshold);

                if (inflated.Contains(position))
                {
                    return summary;
                }
            }

            return null;
        }
    }
}
