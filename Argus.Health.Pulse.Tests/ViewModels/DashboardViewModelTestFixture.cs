// -------------------------------------------------------------------------------------------------
//  <copyright file="DashboardViewModelTestFixture.cs">
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

namespace Argus.Health.Pulse.Tests.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Services;
    using Argus.Health.Pulse.ViewModels;

    using ArgusTransfer.Client;

    using Avalonia.Headless.NUnit;

    using Microsoft.Extensions.Logging.Abstractions;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    /// <summary>
    /// Suite of tests for the sorting behaviour of <see cref="DashboardViewModel"/>
    /// </summary>
    [TestFixture]
    public class DashboardViewModelTestFixture
    {
        [AvaloniaTest]
        public async Task Verify_that_default_sort_is_Name_ascending()
        {
            using var fixture = CreateFixture();

            fixture.PublishEndpoints(
                MakeEndpoint("zebra", "https://z.example.com"),
                MakeEndpoint("alpha", "https://a.example.com"),
                MakeEndpoint("mike", "https://m.example.com"));

            await fixture.WaitForCount(3);

            Assert.That(fixture.ViewModel.SortColumn, Is.EqualTo(EndpointSortColumn.Name));
            Assert.That(fixture.ViewModel.SortDirection, Is.EqualTo(ListSortDirection.Ascending));

            var names = fixture.ViewModel.FilteredEndpoints.Select(e => e.Name).ToList();

            Assert.That(names, Is.EqualTo(new[] { "alpha", "mike", "zebra" }));
        }

        [AvaloniaTest]
        public async Task Verify_that_toggling_SortDirection_reverses_the_order()
        {
            using var fixture = CreateFixture();

            fixture.PublishEndpoints(
                MakeEndpoint("alpha", "https://a.example.com"),
                MakeEndpoint("mike", "https://m.example.com"),
                MakeEndpoint("zebra", "https://z.example.com"));

            await fixture.WaitForCount(3);

            fixture.ViewModel.SortDirection = ListSortDirection.Descending;

            await Task.Delay(50);

            var names = fixture.ViewModel.FilteredEndpoints.Select(e => e.Name).ToList();

            Assert.That(names, Is.EqualTo(new[] { "zebra", "mike", "alpha" }));
        }

        [AvaloniaTest]
        public async Task Verify_that_sorting_by_ResponseTime_descending_orders_by_ResponseTimeMs()
        {
            using var fixture = CreateFixture();

            var fast = MakeEndpoint("fast", "https://fast.example.com");
            var medium = MakeEndpoint("medium", "https://medium.example.com");
            var slow = MakeEndpoint("slow", "https://slow.example.com");

            fixture.PublishEndpoints(fast, medium, slow);

            await fixture.WaitForCount(3);

            fixture.PublishResult(fast.Identifier, statusCode: 200, responseTimeMs: 50);
            fixture.PublishResult(medium.Identifier, statusCode: 200, responseTimeMs: 250);
            fixture.PublishResult(slow.Identifier, statusCode: 200, responseTimeMs: 1500);

            await Task.Delay(100);

            fixture.ViewModel.SortColumn = EndpointSortColumn.ResponseTime;
            fixture.ViewModel.SortDirection = ListSortDirection.Descending;

            await Task.Delay(50);

            var names = fixture.ViewModel.FilteredEndpoints.Select(e => e.Name).ToList();

            Assert.That(names, Is.EqualTo(new[] { "slow", "medium", "fast" }));
        }

        [AvaloniaTest]
        public async Task Verify_that_sorting_by_Status_orders_by_StatusCode_numerically()
        {
            using var fixture = CreateFixture();

            var ok = MakeEndpoint("ok", "https://ok.example.com");
            var teapot = MakeEndpoint("teapot", "https://teapot.example.com");
            var serverError = MakeEndpoint("server-error", "https://err.example.com");

            fixture.PublishEndpoints(ok, teapot, serverError);

            await fixture.WaitForCount(3);

            fixture.PublishResult(ok.Identifier, statusCode: 200, responseTimeMs: 100);
            fixture.PublishResult(teapot.Identifier, statusCode: 418, responseTimeMs: 100);
            fixture.PublishResult(serverError.Identifier, statusCode: 500, responseTimeMs: 100);

            await Task.Delay(100);

            fixture.ViewModel.SortColumn = EndpointSortColumn.Status;
            fixture.ViewModel.SortDirection = ListSortDirection.Ascending;

            await Task.Delay(50);

            var statusCodes = fixture.ViewModel.FilteredEndpoints.Select(e => e.StatusCode).ToList();

            Assert.That(statusCodes, Is.EqualTo(new[] { 200, 418, 500 }));
        }

        [AvaloniaTest]
        public async Task Verify_that_sorting_by_LastChecked_places_nulls_last_in_both_directions()
        {
            using var fixture = CreateFixture();

            var withResult = MakeEndpoint("with-result", "https://x.example.com");
            var pendingOne = MakeEndpoint("pending-one", "https://y.example.com");
            var pendingTwo = MakeEndpoint("pending-two", "https://z.example.com");

            fixture.PublishEndpoints(withResult, pendingOne, pendingTwo);

            await fixture.WaitForCount(3);

            fixture.PublishResult(withResult.Identifier, statusCode: 200, responseTimeMs: 100);

            await Task.Delay(100);

            fixture.ViewModel.SortColumn = EndpointSortColumn.LastChecked;
            fixture.ViewModel.SortDirection = ListSortDirection.Ascending;

            await Task.Delay(50);

            Assert.That(fixture.ViewModel.FilteredEndpoints[0].Name, Is.EqualTo("with-result"),
                "non-null LastChecked should sort first ascending");
            Assert.That(fixture.ViewModel.FilteredEndpoints[1].LastChecked, Is.Null);
            Assert.That(fixture.ViewModel.FilteredEndpoints[2].LastChecked, Is.Null);

            fixture.ViewModel.SortDirection = ListSortDirection.Descending;

            await Task.Delay(50);

            Assert.That(fixture.ViewModel.FilteredEndpoints[0].Name, Is.EqualTo("with-result"),
                "non-null LastChecked should also sort first descending (nulls always last)");
            Assert.That(fixture.ViewModel.FilteredEndpoints[1].LastChecked, Is.Null);
            Assert.That(fixture.ViewModel.FilteredEndpoints[2].LastChecked, Is.Null);
        }

        [AvaloniaTest]
        public async Task Verify_that_live_ResponseTimeMs_update_repositions_the_row()
        {
            using var fixture = CreateFixture();

            var first = MakeEndpoint("first", "https://1.example.com");
            var second = MakeEndpoint("second", "https://2.example.com");
            var third = MakeEndpoint("third", "https://3.example.com");

            fixture.PublishEndpoints(first, second, third);

            await fixture.WaitForCount(3);

            fixture.PublishResult(first.Identifier, statusCode: 200, responseTimeMs: 100);
            fixture.PublishResult(second.Identifier, statusCode: 200, responseTimeMs: 200);
            fixture.PublishResult(third.Identifier, statusCode: 200, responseTimeMs: 300);

            await Task.Delay(100);

            fixture.ViewModel.SortColumn = EndpointSortColumn.ResponseTime;
            fixture.ViewModel.SortDirection = ListSortDirection.Ascending;

            await Task.Delay(50);

            Assert.That(
                fixture.ViewModel.FilteredEndpoints.Select(e => e.Name).ToList(),
                Is.EqualTo(new[] { "first", "second", "third" }));

            fixture.PublishResult(first.Identifier, statusCode: 200, responseTimeMs: 5000);

            await Task.Delay(100);

            Assert.That(
                fixture.ViewModel.FilteredEndpoints.Last().Name,
                Is.EqualTo("first"),
                "first should now be last because its ResponseTimeMs jumped to 5000");
        }

        /// <summary>
        /// Builds a <see cref="HealthEndPoint"/> with sensible defaults
        /// </summary>
        /// <param name="name">The endpoint name</param>
        /// <param name="url">The endpoint URL</param>
        /// <returns>A new <see cref="HealthEndPoint"/></returns>
        private static HealthEndPoint MakeEndpoint(string name, string url)
        {
            return new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = name,
                Url = url,
                Frequency = 30,
                Timeout = 5,
                RetryCount = 1,
                IsActive = true
            };
        }

        /// <summary>
        /// Constructs a fully-wired <see cref="DashboardViewModel"/> backed by mocked services
        /// and an unconnected <see cref="HealthEndPointClient"/>. The bootstrap path runs against
        /// a non-existent named pipe and fails silently inside the VM
        /// </summary>
        /// <returns>A disposable <see cref="Fixture"/> wrapping the view model and its emit subjects</returns>
        private static Fixture CreateFixture()
        {
            var endpointsSubject = new Subject<IList<HealthEndPoint>>();
            var resultsSubject = new Subject<HealthEndPointCheckResult>();

            var syncService = new Mock<IEndpointSyncService>();
            syncService.SetupGet(s => s.EndpointsObservable).Returns(endpointsSubject);
            syncService.SetupGet(s => s.ConnectionErrorObservable).Returns(Observable.Never<bool>());
            syncService.SetupGet(s => s.IsRunningObservable).Returns(Observable.Never<bool>());

            var healthCheckService = new Mock<IHealthCheckService>();
            healthCheckService.SetupGet(h => h.ResultsObservable).Returns(resultsSubject);
            healthCheckService.SetupGet(h => h.FailureObservable).Returns(Observable.Never<HealthEndPointCheckResult>());

            var argusClient = new ArgusClient($"argus-test-dead-{Guid.NewGuid()}");

            var client = new HealthEndPointClient(
                argusClient,
                NullLogger<HealthEndPointClient>.Instance);

            var viewModel = new DashboardViewModel(
                client,
                syncService.Object,
                healthCheckService.Object,
                NullLogger<DashboardViewModel>.Instance);

            return new Fixture(viewModel, endpointsSubject, resultsSubject, argusClient);
        }

        /// <summary>
        /// Wraps a <see cref="DashboardViewModel"/> under test together with the subjects used
        /// to feed endpoint and check-result events
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            /// <summary>
            /// The endpoints emit subject standing in for <see cref="IEndpointSyncService.EndpointsObservable"/>
            /// </summary>
            private readonly Subject<IList<HealthEndPoint>> endpointsSubject;

            /// <summary>
            /// The results emit subject standing in for <see cref="IHealthCheckService.ResultsObservable"/>
            /// </summary>
            private readonly Subject<HealthEndPointCheckResult> resultsSubject;

            /// <summary>
            /// The unconnected <see cref="ArgusClient"/> kept alive for the lifetime of the fixture
            /// </summary>
            private readonly ArgusClient argusClient;

            /// <summary>
            /// Initializes a new instance of the <see cref="Fixture"/> class
            /// </summary>
            /// <param name="viewModel">The dashboard view model under test</param>
            /// <param name="endpointsSubject">The subject used to publish endpoint lists</param>
            /// <param name="resultsSubject">The subject used to publish check results</param>
            /// <param name="argusClient">The Argus client to keep alive and dispose with the fixture</param>
            public Fixture(
                DashboardViewModel viewModel,
                Subject<IList<HealthEndPoint>> endpointsSubject,
                Subject<HealthEndPointCheckResult> resultsSubject,
                ArgusClient argusClient)
            {
                this.ViewModel = viewModel;
                this.endpointsSubject = endpointsSubject;
                this.resultsSubject = resultsSubject;
                this.argusClient = argusClient;
            }

            /// <summary>
            /// Gets the dashboard view model under test
            /// </summary>
            public DashboardViewModel ViewModel { get; }

            /// <summary>
            /// Publishes a set of endpoints to the synced observable, simulating a sync poll
            /// </summary>
            /// <param name="endpoints">The endpoints to publish</param>
            public void PublishEndpoints(params HealthEndPoint[] endpoints)
            {
                this.endpointsSubject.OnNext(endpoints);
            }

            /// <summary>
            /// Publishes a check result for the given endpoint, simulating a health-check tick
            /// </summary>
            /// <param name="endpointIdentifier">The endpoint the result belongs to</param>
            /// <param name="statusCode">The HTTP status code recorded</param>
            /// <param name="responseTimeMs">The response time in milliseconds</param>
            public void PublishResult(Guid endpointIdentifier, int statusCode, long responseTimeMs)
            {
                this.resultsSubject.OnNext(new HealthEndPointCheckResult
                {
                    HealthEndPoint = endpointIdentifier,
                    StatusCode = statusCode,
                    ResponseTimeMs = responseTimeMs,
                    Timestamp = DateTime.UtcNow
                });
            }

            /// <summary>
            /// Waits asynchronously until <see cref="DashboardViewModel.FilteredEndpoints"/> reaches
            /// the requested count, or throws on timeout
            /// </summary>
            /// <param name="count">The expected count</param>
            /// <returns>A task that completes when the count is reached</returns>
            public async Task WaitForCount(int count)
            {
                await this.ViewModel
                    .WhenAnyValue(x => x.TotalEndpoints)
                    .Where(_ => this.ViewModel.FilteredEndpoints.Count == count)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .FirstAsync();
            }

            /// <summary>
            /// Disposes the view model and its subjects
            /// </summary>
            public void Dispose()
            {
                this.ViewModel.Dispose();
                this.endpointsSubject.Dispose();
                this.resultsSubject.Dispose();
                this.argusClient.Dispose();
            }
        }
    }
}
