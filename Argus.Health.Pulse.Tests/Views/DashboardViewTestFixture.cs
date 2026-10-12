// -------------------------------------------------------------------------------------------------
//  <copyright file="DashboardViewTestFixture.cs">
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

namespace Argus.Health.Pulse.Tests.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Services;
    using Argus.Health.Pulse.ViewModels;
    using Argus.Health.Pulse.Views;

    using ArgusTransfer.Client;

    using Avalonia.Controls;
    using Avalonia.Headless.NUnit;
    using Avalonia.Threading;

    using Microsoft.Extensions.Logging.Abstractions;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI.Primitives.Signals;

    /// <summary>
    /// Suite of tests for the layout of the <see cref="DashboardView"/>
    /// </summary>
    [TestFixture]
    public class DashboardViewTestFixture
    {
        [AvaloniaTest]
        public async Task Verify_that_the_incidents_grid_shows_its_columns_at_their_declared_width()
        {
            var endpoints = new Signal<IList<HealthEndPoint>>();
            var results = new Signal<HealthEndPointCheckResult>();

            var syncService = new Mock<IEndpointSyncService>();
            syncService.SetupGet(s => s.EndpointsObservable).Returns(endpoints);

            var healthCheckService = new Mock<IHealthCheckService>();
            healthCheckService.SetupGet(h => h.ResultsObservable).Returns(results);

            using var argusClient = new ArgusClient($"argus-test-dead-{Guid.NewGuid()}");
            var client = new HealthEndPointClient(argusClient, NullLogger<HealthEndPointClient>.Instance);

            using var viewModel = new DashboardViewModel(client, syncService.Object, healthCheckService.Object, NullLogger<DashboardViewModel>.Instance);

            var view = new DashboardView { ViewModel = viewModel };
            var window = new Window { Width = 1040, Height = 688, Content = view };

            window.Show();
            Dispatcher.UIThread.RunJobs();

            var endpoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "failing",
                Url = "https://failing.example.com",
                Frequency = 30,
                Timeout = 5,
                RetryCount = 1,
                IsActive = true
            };

            endpoints.OnNext(new[] { endpoint });

            await Task.Delay(100);

            results.OnNext(new HealthEndPointCheckResult
            {
                HealthEndPoint = endpoint.Identifier,
                StatusCode = 500,
                ResponseTimeMs = 12,
                Timestamp = DateTime.UtcNow
            });

            await Task.Delay(100);

            viewModel.ActiveViewMode = DashboardViewMode.Incidents;

            await Task.Delay(100);
            Dispatcher.UIThread.RunJobs();

            var incidentsGrid = view.FindControl<DataGrid>("IncidentsGrid");

            Assert.That(incidentsGrid, Is.Not.Null);
            Assert.That(incidentsGrid.IsVisible, Is.True);
            Assert.That(viewModel.IncidentResults, Has.Count.EqualTo(1));

            var widths = incidentsGrid.Columns.Select(column => column.ActualWidth).ToList();

            Assert.That(widths[0], Is.EqualTo(180).Within(1), "Endpoint column");
            Assert.That(widths[1], Is.EqualTo(160).Within(1), "Time column");
            Assert.That(widths[2], Is.EqualTo(80).Within(1), "Status column");
            Assert.That(widths[3], Is.EqualTo(80).Within(1), "Response column");
            Assert.That(widths[4], Is.GreaterThan(100), "Error column takes the remaining width");

            window.Close();
        }
    }
}
