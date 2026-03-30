// -------------------------------------------------------------------------------------------------
//  <copyright file="UptimeHeatmapControl.cs">
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
    /// Renders a heatmap grid where each cell represents one day, colored by uptime percentage
    /// </summary>
    public class UptimeHeatmapControl : Control
    {
        /// <summary>
        /// Gap between cells in pixels
        /// </summary>
        private const double CellGap = 2;

        /// <summary>
        /// Bottom margin for month labels
        /// </summary>
        private const double BottomMargin = 16;

        /// <summary>
        /// Defines the <see cref="Data"/> styled property
        /// </summary>
        public static readonly StyledProperty<IReadOnlyList<UptimeSummary>?> DataProperty =
            AvaloniaProperty.Register<UptimeHeatmapControl, IReadOnlyList<UptimeSummary>?>(nameof(Data));

        /// <summary>
        /// Cached cell positions and associated summaries for hit testing
        /// </summary>
        private readonly List<(Rect Bounds, UptimeSummary Summary)> cellPositions = new();

        /// <summary>
        /// Brush for 100% uptime cells
        /// </summary>
        private static readonly SolidColorBrush UptimePerfect = new(Color.Parse("#22c55e"));

        /// <summary>
        /// Brush for 99-100% uptime cells
        /// </summary>
        private static readonly SolidColorBrush UptimeHigh = new(Color.Parse("#4ade80"));

        /// <summary>
        /// Brush for 95-99% uptime cells
        /// </summary>
        private static readonly SolidColorBrush UptimeMedium = new(Color.Parse("#fbbf24"));

        /// <summary>
        /// Brush for less than 95% uptime cells
        /// </summary>
        private static readonly SolidColorBrush UptimeLow = new(Color.Parse("#ef4444"));

        /// <summary>
        /// Initializes static members of the <see cref="UptimeHeatmapControl"/> class
        /// </summary>
        static UptimeHeatmapControl()
        {
            AffectsRender<UptimeHeatmapControl>(DataProperty);
        }

        /// <summary>
        /// Gets or sets the daily uptime summary data to display
        /// </summary>
        public IReadOnlyList<UptimeSummary>? Data
        {
            get => this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
        }

        /// <summary>
        /// Renders the heatmap grid with colored day cells
        /// </summary>
        /// <param name="context">The drawing context</param>
        public override void Render(DrawingContext context)
        {
            this.cellPositions.Clear();

            var data = this.Data;

            if (data == null || data.Count == 0)
            {
                var noData = new FormattedText("No uptime data", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default, 12, DashboardColors.Gray);

                context.DrawText(noData, new Point(this.Bounds.Width / 2 - noData.Width / 2, this.Bounds.Height / 2));
                return;
            }

            var cellHeight = this.Bounds.Height - BottomMargin;

            if (cellHeight <= 0)
            {
                return;
            }

            var totalGap = CellGap * (data.Count - 1);
            var cellWidth = (this.Bounds.Width - totalGap) / data.Count;

            if (cellWidth < 2)
            {
                cellWidth = 2;
            }

            var lastMonthLabel = string.Empty;

            for (var i = 0; i < data.Count; i++)
            {
                var summary = data[i];
                var x = i * (cellWidth + CellGap);
                var rect = new Rect(x, 0, cellWidth, cellHeight);

                var brush = UptimeHeatmapControl.GetCellBrush(summary);
                context.FillRectangle(brush, rect, 2);

                this.cellPositions.Add((rect, summary));

                // Draw month label when month changes
                var monthLabel = summary.PeriodStart.ToLocalTime().ToString("MMM", CultureInfo.CurrentCulture);

                if (monthLabel != lastMonthLabel)
                {
                    var label = new FormattedText(monthLabel, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        Typeface.Default, 9, DashboardColors.Gray);

                    if (x + label.Width < this.Bounds.Width)
                    {
                        context.DrawText(label, new Point(x, cellHeight + 2));
                    }

                    lastMonthLabel = monthLabel;
                }
            }
        }

        /// <summary>
        /// Handles pointer movement to show tooltips on hovered cells
        /// </summary>
        /// <param name="e">The pointer event args</param>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            var position = e.GetPosition(this);
            var nearest = this.FindCell(position);

            if (nearest != null)
            {
                var dateText = nearest.PeriodStart.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
                var uptimeText = nearest.TotalChecks > 0
                    ? $"{nearest.UptimePercent:F1}%"
                    : "No data";

                var tooltip = $"{dateText}\nUptime: {uptimeText}\nChecks: {nearest.TotalChecks}";

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
        /// Returns the brush color for a cell based on its uptime percentage
        /// </summary>
        /// <param name="summary">The uptime summary for the cell</param>
        /// <returns>A color ranging from dark green (100%) to red (less than 95%) or gray (no data)</returns>
        private static IBrush GetCellBrush(UptimeSummary summary)
        {
            if (summary.TotalChecks == 0)
            {
                return DashboardColors.Gray;
            }

            var percent = summary.UptimePercent;

            if (percent >= 100.0)
            {
                return UptimePerfect;
            }

            if (percent >= 99.0)
            {
                return UptimeHigh;
            }

            return percent >= 95.0 ? UptimeMedium : UptimeLow;
        }

        /// <summary>
        /// Finds the cell at the given pointer position
        /// </summary>
        /// <param name="position">The pointer position</param>
        /// <returns>The uptime summary at the position, or null if none found</returns>
        private UptimeSummary? FindCell(Point position)
        {
            foreach (var (bounds, summary) in this.cellPositions)
            {
                if (bounds.Contains(position))
                {
                    return summary;
                }
            }

            return null;
        }
    }
}
