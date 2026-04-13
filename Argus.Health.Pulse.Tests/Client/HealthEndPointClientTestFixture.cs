// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointClientTestFixture.cs">
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

namespace Argus.Health.Pulse.Client.Tests
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;

    using ArgusTransfer.Client;
    using ArgusTransfer.Extensions;
    using ArgusTransfer.Protocol;
    using ArgusTransfer.Serialization;

    using Microsoft.Extensions.Logging;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="HealthEndPointClient"/> class
    /// </summary>
    [TestFixture]
    public class HealthEndPointClientTestFixture
    {
        [Test]
        public async Task Verify_that_GetAllAsync_returns_list_of_endpoints()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";

            var ep1 = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "ep1",
                Url = "https://example.com/1",
                Frequency = 30,
                Timeout = 5,
                RetryCount = 3
            };

            var ep2 = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "ep2",
                Url = "https://example.com/2",
                Frequency = 60,
                Timeout = 10,
                RetryCount = 1
            };

            var responseBody = HealthEndPointWriter.Write(new[] { ep1, ep2 });

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, "/healthendpoint",
                ArgusStatusCode.Ok, responseBody);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            var result = await client.GetAllAsync();

            await serverTask;

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("ep1"));
            Assert.That(result[1].Name, Is.EqualTo("ep2"));
        }

        [Test]
        public async Task Verify_that_GetByIdAsync_returns_single_endpoint()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var identifier = Guid.NewGuid();

            var ep = new HealthEndPoint
            {
                Identifier = identifier,
                Name = "my-endpoint",
                Url = "https://example.com/healthz",
                Frequency = 15,
                Timeout = 3,
                RetryCount = 2
            };

            var responseBody = HealthEndPointWriter.Write(ep);

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, $"/healthendpoint/{identifier.ToShortGuid()}",
                ArgusStatusCode.Ok, responseBody);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            var result = await client.GetByIdAsync(identifier);

            await serverTask;

            Assert.That(result.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Name, Is.EqualTo("my-endpoint"));
            Assert.That(result.Url, Is.EqualTo("https://example.com/healthz"));
        }

        [Test]
        public async Task Verify_that_CreateAsync_sends_endpoint_and_returns_created()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";

            var ep = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "new-endpoint",
                Url = "https://example.com/new",
                Frequency = 30,
                Timeout = 5,
                RetryCount = 3
            };

            var responseBody = HealthEndPointWriter.Write(ep);

            var serverTask = CreateServerTask(pipeName, ArgusVerb.POST, "/healthendpoint",
                ArgusStatusCode.Created, responseBody);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            var result = await client.CreateAsync(ep);

            await serverTask;

            Assert.That(result.Name, Is.EqualTo("new-endpoint"));
            Assert.That(result.Url, Is.EqualTo("https://example.com/new"));
        }

        [Test]
        public async Task Verify_that_UpdateAsync_sends_endpoint_and_returns_updated()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var identifier = Guid.NewGuid();

            var ep = new HealthEndPoint
            {
                Identifier = identifier,
                Name = "updated-endpoint",
                Url = "https://example.com/updated",
                Frequency = 60,
                Timeout = 10,
                RetryCount = 5
            };

            var responseBody = HealthEndPointWriter.Write(ep);

            var serverTask = CreateServerTask(pipeName, ArgusVerb.PUT, $"/healthendpoint/{identifier.ToShortGuid()}",
                ArgusStatusCode.Ok, responseBody);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            var result = await client.UpdateAsync(ep);

            await serverTask;

            Assert.That(result.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Name, Is.EqualTo("updated-endpoint"));
        }

        [Test]
        public async Task Verify_that_DeleteAsync_sends_delete_request()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var identifier = Guid.NewGuid();

            var serverTask = CreateServerTask(pipeName, ArgusVerb.DELETE, $"/healthendpoint/{identifier.ToShortGuid()}",
                ArgusStatusCode.Ok, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            await client.DeleteAsync(identifier);

            await serverTask;
        }

        [Test]
        public void Verify_that_GetAllAsync_throws_on_error_status()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, "/healthendpoint",
                ArgusStatusCode.InternalServerError, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetAllAsync());

            serverTask.Wait();
        }

        /// <summary>
        /// Creates an in-process named pipe server that reads the request, verifies verb and route,
        /// and writes a canned response. An optional assertion callback can inspect the request
        /// (e.g. to verify query parameters or body content).
        /// </summary>
        private static Task CreateServerTask(string pipeName, ArgusVerb expectedVerb, string expectedRoute,
            ArgusStatusCode statusCode, string responseBody, Action<ArgusRequest> requestAssert = null)
        {
            var requestSerializer = new ArgusRequestSerializer();
            var responseSerializer = new ArgusResponseSerializer();

            return Task.Run(async () =>
            {
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
                await server.WaitForConnectionAsync();

                var reader = new StreamReader(server, new UTF8Encoding(false));
                var writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = false };

                var request = await requestSerializer.ReadAsync(reader, CancellationToken.None);

                Assert.That(request.Verb, Is.EqualTo(expectedVerb));
                Assert.That(request.Route, Is.EqualTo(expectedRoute));

                requestAssert?.Invoke(request);

                var response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = statusCode,
                    Body = responseBody
                };

                responseSerializer.Write(writer, response);
            });
        }

        [Test]
        public async Task Verify_that_GetCheckResultsAsync_returns_list_of_check_results()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var endpointId = Guid.NewGuid();

            var r1 = new HealthEndPointCheckResult
            {
                Identifier = Guid.NewGuid(),
                Timestamp = new DateTime(2026, 4, 13, 10, 0, 0, DateTimeKind.Utc),
                StatusCode = 200,
                ResponseTimeMs = 42,
                HealthEndPoint = endpointId
            };

            var r2 = new HealthEndPointCheckResult
            {
                Identifier = Guid.NewGuid(),
                Timestamp = new DateTime(2026, 4, 13, 10, 1, 0, DateTimeKind.Utc),
                StatusCode = 500,
                ErrorMessage = "boom",
                ResponseTimeMs = 123,
                HealthEndPoint = endpointId
            };

            var responseBody = HealthEndPointCheckResultWriter.WriteArray(new[] { r1, r2 });

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, $"/healthendpoint/{endpointId.ToShortGuid()}/results",
                ArgusStatusCode.Ok, responseBody);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            var result = await client.GetCheckResultsAsync(endpointId);

            await serverTask;

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].StatusCode, Is.EqualTo(200));
            Assert.That(result[0].ResponseTimeMs, Is.EqualTo(42));
            Assert.That(result[1].StatusCode, Is.EqualTo(500));
            Assert.That(result[1].ErrorMessage, Is.EqualTo("boom"));
        }

        [Test]
        public async Task Verify_that_GetUptimeSummaryAsync_returns_summaries_with_default_params()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var endpointId = Guid.NewGuid();

            var s1 = new UptimeSummary
            {
                PeriodStart = new DateTime(2026, 4, 13, 10, 0, 0, DateTimeKind.Utc),
                TotalChecks = 60,
                HealthyChecks = 59,
                AverageResponseTimeMs = 45.5,
                MaxResponseTimeMs = 200
            };

            var s2 = new UptimeSummary
            {
                PeriodStart = new DateTime(2026, 4, 13, 11, 0, 0, DateTimeKind.Utc),
                TotalChecks = 60,
                HealthyChecks = 60,
                AverageResponseTimeMs = 38.0,
                MaxResponseTimeMs = 150
            };

            var responseBody = UptimeSummaryWriter.WriteArray(new[] { s1, s2 });

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, $"/healthendpoint/{endpointId.ToShortGuid()}/uptime",
                ArgusStatusCode.Ok, responseBody);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            var result = await client.GetUptimeSummaryAsync(endpointId);

            await serverTask;

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].TotalChecks, Is.EqualTo(60));
            Assert.That(result[0].HealthyChecks, Is.EqualTo(59));
            Assert.That(result[1].HealthyChecks, Is.EqualTo(60));
        }

        [Test]
        public async Task Verify_that_GetUptimeSummaryAsync_forwards_days_and_resolution_query_parameters()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var endpointId = Guid.NewGuid();

            var responseBody = UptimeSummaryWriter.WriteArray(Array.Empty<UptimeSummary>());

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, $"/healthendpoint/{endpointId.ToShortGuid()}/uptime",
                ArgusStatusCode.Ok, responseBody,
                request =>
                {
                    Assert.That(request.QueryParameters["days"], Is.EqualTo("30"));
                    Assert.That(request.QueryParameters["resolution"], Is.EqualTo("day"));
                });

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            var result = await client.GetUptimeSummaryAsync(endpointId, days: 30, resolution: UptimeResolution.Day);

            await serverTask;

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Verify_that_GetByIdAsync_throws_on_error_status()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var identifier = Guid.NewGuid();

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, $"/healthendpoint/{identifier.ToShortGuid()}",
                ArgusStatusCode.NotFound, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetByIdAsync(identifier));

            serverTask.Wait();
        }

        [Test]
        public void Verify_that_CreateAsync_throws_on_error_status()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";

            var ep = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "bad",
                Url = "not-a-url",
                Frequency = 30,
                Timeout = 5,
                RetryCount = 3
            };

            var serverTask = CreateServerTask(pipeName, ArgusVerb.POST, "/healthendpoint",
                ArgusStatusCode.BadRequest, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.CreateAsync(ep));

            serverTask.Wait();
        }

        [Test]
        public void Verify_that_UpdateAsync_throws_on_error_status()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var identifier = Guid.NewGuid();

            var ep = new HealthEndPoint
            {
                Identifier = identifier,
                Name = "missing",
                Url = "https://example.com/missing",
                Frequency = 30,
                Timeout = 5,
                RetryCount = 3
            };

            var serverTask = CreateServerTask(pipeName, ArgusVerb.PUT, $"/healthendpoint/{identifier.ToShortGuid()}",
                ArgusStatusCode.NotFound, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.UpdateAsync(ep));

            serverTask.Wait();
        }

        [Test]
        public void Verify_that_DeleteAsync_throws_on_error_status()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var identifier = Guid.NewGuid();

            var serverTask = CreateServerTask(pipeName, ArgusVerb.DELETE, $"/healthendpoint/{identifier.ToShortGuid()}",
                ArgusStatusCode.NotFound, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.DeleteAsync(identifier));

            serverTask.Wait();
        }

        [Test]
        public void Verify_that_GetCheckResultsAsync_throws_on_error_status()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var endpointId = Guid.NewGuid();

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, $"/healthendpoint/{endpointId.ToShortGuid()}/results",
                ArgusStatusCode.InternalServerError, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetCheckResultsAsync(endpointId));

            serverTask.Wait();
        }

        [Test]
        public void Verify_that_GetUptimeSummaryAsync_throws_on_error_status()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";
            var endpointId = Guid.NewGuid();

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, $"/healthendpoint/{endpointId.ToShortGuid()}/uptime",
                ArgusStatusCode.InternalServerError, null);

            using var argusClient = new ArgusClient(pipeName);
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetUptimeSummaryAsync(endpointId));

            serverTask.Wait();
        }

        [Test]
        public void Verify_that_CreateAsync_throws_on_null_endpoint()
        {
            using var argusClient = new ArgusClient("unused");
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<ArgumentNullException>(async () => await client.CreateAsync(null));
        }

        [Test]
        public void Verify_that_UpdateAsync_throws_on_null_endpoint()
        {
            using var argusClient = new ArgusClient("unused");
            var client = new HealthEndPointClient(argusClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<HealthEndPointClient>.Instance);

            Assert.ThrowsAsync<ArgumentNullException>(async () => await client.UpdateAsync(null));
        }
    }
}
