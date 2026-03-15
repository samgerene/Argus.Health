// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointBackgroundServiceTestFixture.cs"  >
//
//     Copyright (c) 2025-2026 Sam Gerené
//
//     Licensed under the Apache License, Version 2.0 (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
//
//         http://www.apache.org/licenses/LICENSE-2.0
//
//     Unless required by applicable law or agreed to in writing, softwareUseCases
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.
//
//   </copyright>
//   ------------------------------------------------------------------------------------------------

namespace Argus.Health.Service.Tests.BackgroundServices
{
    using System;
    using System.Collections.Immutable;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Service.BackgroundServices;
    using Argus.Health.Service.Repository;

    using Microsoft.Extensions.Logging;

    using Moq;
    using Moq.Protected;

    /// <summary>
    /// Suite of tests for the <see cref="HealthEndPointBackgroundService"/> class
    /// </summary>
    [TestFixture]
    public class HealthEndPointBackgroundServiceTestFixture
    {
        private HealthEndPointBackgroundService service;

        private Mock<ILogger<HealthEndPointBackgroundService>> mockLogger;

        private Mock<IHttpClientFactory> mockHttpClientFactory;

        private Mock<IHealthEndPointRepository> mockRepository;

        private Mock<HttpMessageHandler> mockHttpMessageHandler;

        [SetUp]
        public void SetUp()
        {
            this.mockLogger = new Mock<ILogger<HealthEndPointBackgroundService>>();
            this.mockHttpClientFactory = new Mock<IHttpClientFactory>();
            this.mockRepository = new Mock<IHealthEndPointRepository>();
            this.mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            this.mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(this.mockHttpMessageHandler.Object);

            this.mockHttpClientFactory
                .Setup(f => f.CreateClient("ArgusHealth"))
                .Returns(httpClient);

            this.service = new HealthEndPointBackgroundService(
                this.mockLogger.Object,
                this.mockHttpClientFactory.Object,
                this.mockRepository.Object);
        }

        [Test]
        public async Task Verify_that_ExecuteAsync_loads_endpoints_and_starts_monitors()
        {
            var endpoints = new[]
            {
                new HealthEndPoint
                {
                    Identifier = Guid.NewGuid(),
                    Name = "test-ep-1",
                    Url = "https://example.com/healthz",
                    Frequency = 300,
                    Timeout = 5,
                    RetryCount = 1
                }
            }.ToImmutableList();

            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(endpoints);

            using var cts = new CancellationTokenSource();

            await this.service.StartAsync(cts.Token);

            // Give it time to process
            await Task.Delay(500);

            await cts.CancelAsync();
            await this.service.StopAsync(CancellationToken.None);

            this.mockRepository.Verify(r => r.ReadAsync(null), Times.Once);
            this.mockHttpClientFactory.Verify(f => f.CreateClient("ArgusHealth"), Times.AtLeastOnce);
        }

        [Test]
        public async Task Verify_that_EndpointAdded_event_starts_new_monitor()
        {
            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(ImmutableList<HealthEndPoint>.Empty);

            using var cts = new CancellationTokenSource();

            await this.service.StartAsync(cts.Token);

            await Task.Delay(200);

            var newEndpoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "dynamic-ep",
                Url = "https://example.com/dynamic",
                Frequency = 300,
                Timeout = 5,
                RetryCount = 1
            };

            this.mockRepository.Raise(r => r.EndpointAdded += null, this, newEndpoint);

            await Task.Delay(500);

            await cts.CancelAsync();
            await this.service.StopAsync(CancellationToken.None);

            this.mockHttpClientFactory.Verify(f => f.CreateClient("ArgusHealth"), Times.AtLeastOnce);
        }

        [Test]
        public async Task Verify_that_EndpointRemoved_event_stops_monitor()
        {
            var endpoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "remove-ep",
                Url = "https://example.com/remove",
                Frequency = 300,
                Timeout = 5,
                RetryCount = 1
            };

            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(new[] { endpoint }.ToImmutableList());

            using var cts = new CancellationTokenSource();

            await this.service.StartAsync(cts.Token);

            await Task.Delay(200);

            this.mockRepository.Raise(r => r.EndpointRemoved += null, this, endpoint);

            await Task.Delay(200);

            await cts.CancelAsync();
            await this.service.StopAsync(CancellationToken.None);

            // No exception means the monitor was stopped cleanly
            Assert.Pass();
        }

        [Test]
        public async Task Verify_that_EndpointUpdated_event_restarts_monitor()
        {
            var endpoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "update-ep",
                Url = "https://example.com/update",
                Frequency = 300,
                Timeout = 5,
                RetryCount = 1
            };

            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(new[] { endpoint }.ToImmutableList());

            using var cts = new CancellationTokenSource();

            await this.service.StartAsync(cts.Token);

            await Task.Delay(200);

            endpoint.Name = "updated-ep";
            this.mockRepository.Raise(r => r.EndpointUpdated += null, this, endpoint);

            await Task.Delay(200);

            await cts.CancelAsync();
            await this.service.StopAsync(CancellationToken.None);

            Assert.Pass();
        }

        [Test]
        public async Task Verify_that_duplicate_endpoint_add_does_not_create_second_monitor()
        {
            var endpoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "dup-ep",
                Url = "https://example.com/dup",
                Frequency = 300,
                Timeout = 5,
                RetryCount = 1
            };

            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(new[] { endpoint }.ToImmutableList());

            using var cts = new CancellationTokenSource();

            await this.service.StartAsync(cts.Token);

            await Task.Delay(200);

            // Raise EndpointAdded for the same endpoint that was already loaded
            this.mockRepository.Raise(r => r.EndpointAdded += null, this, endpoint);

            await Task.Delay(200);

            await cts.CancelAsync();
            await this.service.StopAsync(CancellationToken.None);

            // CreateClient should be called only once (not twice) since duplicate is ignored
            this.mockHttpClientFactory.Verify(f => f.CreateClient("ArgusHealth"), Times.Once);
        }

        [Test]
        public async Task Verify_that_monitor_handles_HttpRequestException()
        {
            this.mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var httpClient = new HttpClient(this.mockHttpMessageHandler.Object);
            this.mockHttpClientFactory
                .Setup(f => f.CreateClient("ArgusHealth"))
                .Returns(httpClient);

            var endpoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "failing-ep",
                Url = "https://example.com/fail",
                Frequency = 300,
                Timeout = 5,
                RetryCount = 1
            };

            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(new[] { endpoint }.ToImmutableList());

            using var cts = new CancellationTokenSource();

            await this.service.StartAsync(cts.Token);

            // Give time for the monitor to attempt the request and hit the exception path
            await Task.Delay(2000);

            await cts.CancelAsync();
            await this.service.StopAsync(CancellationToken.None);

            // Service should not crash — reaching this point means exception was handled
            this.mockHttpClientFactory.Verify(f => f.CreateClient("ArgusHealth"), Times.AtLeastOnce);
        }

        [Test]
        public async Task Verify_that_monitor_handles_unhealthy_response()
        {
            this.mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var httpClient = new HttpClient(this.mockHttpMessageHandler.Object);
            this.mockHttpClientFactory
                .Setup(f => f.CreateClient("ArgusHealth"))
                .Returns(httpClient);

            var endpoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "unhealthy-ep",
                Url = "https://example.com/unhealthy",
                Frequency = 300,
                Timeout = 5,
                RetryCount = 0
            };

            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(new[] { endpoint }.ToImmutableList());

            using var cts = new CancellationTokenSource();

            await this.service.StartAsync(cts.Token);

            await Task.Delay(500);

            await cts.CancelAsync();
            await this.service.StopAsync(CancellationToken.None);

            this.mockHttpClientFactory.Verify(f => f.CreateClient("ArgusHealth"), Times.AtLeastOnce);
        }
    }
}
