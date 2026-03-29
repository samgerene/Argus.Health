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

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.ViewModels;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media;

    /// <summary>
    /// Renders a horizontal uptime timeline bar with colored segments
    /// </summary>
    public class UptimeTimelineControl : Control
    {
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
        /// Renders the uptime timeline
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

            var width = this.Bounds.Width;
            var barHeight = Math.Min(this.Bounds.Height - 20, 24);
            var barY = (this.Bounds.Height - barHeight) / 2;

            var totalSpan = this.SelectedTimeRange.ToTimeSpan();
            var rangeStart = DateTime.UtcNow - totalSpan;

            // Draw background
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#1a1a1a")), null,
                new Rect(0, barY, width, barHeight), 4, 4);

            // Draw segments for each result
            for (var i = 0; i < data.Count; i++)
            {
                var result = data[i];
                var segmentStart = (result.Timestamp - rangeStart).TotalSeconds / totalSpan.TotalSeconds;

                double segmentEnd;

                if (i + 1 < data.Count)
                {
                    segmentEnd = (data[i + 1].Timestamp - rangeStart).TotalSeconds / totalSpan.TotalSeconds;
                }
                else
                {
                    segmentEnd = (DateTime.UtcNow - rangeStart).TotalSeconds / totalSpan.TotalSeconds;
                }

                segmentStart = Math.Max(0, Math.Min(1, segmentStart));
                segmentEnd = Math.Max(0, Math.Min(1, segmentEnd));

                if (segmentEnd <= segmentStart)
                {
                    continue;
                }

                var x = segmentStart * width;
                var segWidth = (segmentEnd - segmentStart) * width;

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

                context.DrawRectangle(brush, null, new Rect(x, barY, segWidth, barHeight));
            }

            // Draw time labels
            var startLabel = new FormattedText(rangeStart.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture),
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, DashboardColors.Gray);

            context.DrawText(startLabel, new Point(0, barY + barHeight + 2));

            var endLabel = new FormattedText(DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture),
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10, DashboardColors.Gray);

            context.DrawText(endLabel, new Point(width - endLabel.Width, barY + barHeight + 2));
        }
    }
}
