// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointListViewModelTestFixture.cs">
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
    using System.IO;
    using System.IO.Pipes;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;
    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.ViewModels;

    using ArgusTransfer.Client;
    using ArgusTransfer.Protocol;
    using ArgusTransfer.Serialization;

    using Avalonia.Headless.NUnit;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    using NUnit.Framework;

    using ReactiveUI;

    /// <summary>
    /// Suite of tests for the <see cref="EndpointListViewModel"/> class
    /// </summary>
    [TestFixture]
    public class EndpointListViewModelTestFixture
    {
        [AvaloniaTest]
        public async Task Verify_that_HasEndpoints_is_true_when_endpoints_are_loaded()
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

            var client = new HealthEndPointClient(
                argusClient,
                NullLogger<HealthEndPointClient>.Instance);

            using var viewModel = new EndpointListViewModel(
                client,
                _ => { },
                NullLogger<EndpointListViewModel>.Instance,
                NullLoggerFactory.Instance);

            await serverTask;

            await viewModel
                .WhenAnyValue(x => x.HasEndpoints)
                .Where(h => h)
                .Timeout(TimeSpan.FromSeconds(5))
                .FirstAsync();

            Assert.That(viewModel.HasEndpoints, Is.True);
            Assert.That(viewModel.Endpoints, Has.Count.EqualTo(2));
        }

        [AvaloniaTest]
        public async Task Verify_that_HasEndpoints_is_false_when_no_endpoints_exist()
        {
            var pipeName = $"argus-test-{Guid.NewGuid()}";

            var responseBody = HealthEndPointWriter.Write(Array.Empty<HealthEndPoint>());

            var serverTask = CreateServerTask(pipeName, ArgusVerb.GET, "/healthendpoint",
                ArgusStatusCode.Ok, responseBody);

            using var argusClient = new ArgusClient(pipeName);

            var client = new HealthEndPointClient(
                argusClient,
                NullLogger<HealthEndPointClient>.Instance);

            using var viewModel = new EndpointListViewModel(
                client,
                _ => { },
                NullLogger<EndpointListViewModel>.Instance,
                NullLoggerFactory.Instance);

            await serverTask;

            // allow the observable pipeline to settle
            await Task.Delay(200);

            Assert.That(viewModel.HasEndpoints, Is.False);
            Assert.That(viewModel.Endpoints, Has.Count.EqualTo(0));
        }

        /// <summary>
        /// Creates an in-process named pipe server that reads the request, verifies verb and route,
        /// and writes a canned response
        /// </summary>
        /// <param name="pipeName">
        /// The name of the named pipe
        /// </param>
        /// <param name="expectedVerb">
        /// The expected <see cref="ArgusVerb"/> of the incoming request
        /// </param>
        /// <param name="expectedRoute">
        /// The expected route of the incoming request
        /// </param>
        /// <param name="statusCode">
        /// The <see cref="ArgusStatusCode"/> to return in the response
        /// </param>
        /// <param name="responseBody">
        /// The response body to return, or null for an empty body
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the server operation
        /// </returns>
        private static Task CreateServerTask(string pipeName, ArgusVerb expectedVerb, string expectedRoute,
            ArgusStatusCode statusCode, string responseBody)
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

                var response = new ArgusResponse
                {
                    CorrelationToken = request.CorrelationToken,
                    StatusCode = statusCode,
                    Body = responseBody
                };

                responseSerializer.Write(writer, response);
            });
        }
    }
}
