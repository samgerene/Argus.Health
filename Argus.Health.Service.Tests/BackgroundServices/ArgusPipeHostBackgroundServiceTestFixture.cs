// -------------------------------------------------------------------------------------------------
//   <copyright file="ArgusPipeHostBackgroundServiceTestFixture.cs">
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
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;
    using Argus.Health.Service.Modules;
    using Argus.Health.Service.Repository;

    using ArgusTransfer.Extensions;
    using ArgusTransfer.Protocol;
    using ArgusTransfer.Routing;
    using ArgusTransfer.Serialization;
    using ArgusTransfer.Server;

    using FluentResults;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Moq;

    /// <summary>
    /// Suite of tests for the <see cref="ArgusPipeHostBackgroundService"/> class
    /// integrated with the <see cref="HealthEndPointModule"/>
    /// </summary>
    [TestFixture]
    public class ArgusPipeHostBackgroundServiceTestFixture
    {
        private ArgusPipeHostBackgroundService service;

        private Mock<ILogger<ArgusPipeHostBackgroundService>> mockLogger;

        private Mock<ILogger<HealthEndPointModule>> mockModuleLogger;

        private Mock<IHealthEndPointRepository> mockRepository;

        [SetUp]
        public void SetUp()
        {
            this.mockLogger = new Mock<ILogger<ArgusPipeHostBackgroundService>>();
            this.mockModuleLogger = new Mock<ILogger<HealthEndPointModule>>();
            this.mockRepository = new Mock<IHealthEndPointRepository>();

            var module = new HealthEndPointModule(this.mockModuleLogger.Object, this.mockRepository.Object);
            var router = new ArgusRouter();
            module.AddRoutes(router);

            var options = Options.Create(new ArgusPipeHostOptions());

            this.service = new ArgusPipeHostBackgroundService(
                this.mockLogger.Object,
                router,
                options,
                new PlainTextArgusBodySerializer());
        }

        [Test]
        public async Task Verify_that_POST_request_with_valid_HealthEndPoint_creates_endpoint()
        {
            var healthEndPoint = new HealthEndPoint
            {
                Identifier = Guid.NewGuid(),
                Name = "test-endpoint",
                Url = "https://example.com/healthz",
                Frequency = 15,
                Timeout = 3,
                RetryCount = 2
            };

            var bodyJson = HealthEndPointWriter.Write(healthEndPoint);

            this.mockRepository
                .Setup(r => r.CreateAsync(It.IsAny<HealthEndPoint>()))
                .ReturnsAsync(Result.Ok());

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/healthendpoint",
                Body = bodyJson
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Created));

            this.mockRepository.Verify(r => r.CreateAsync(It.Is<HealthEndPoint>(
                ep => ep.Name == "test-endpoint"
                    && ep.Url == "https://example.com/healthz"
                    && ep.Frequency == 15
                    && ep.Timeout == 3
                    && ep.RetryCount == 2)), Times.Once);
        }

        [Test]
        public async Task Verify_that_POST_request_with_empty_body_returns_BadRequest()
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/healthendpoint",
                Body = ""
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.BadRequest));

            this.mockRepository.Verify(r => r.CreateAsync(It.IsAny<HealthEndPoint>()), Times.Never);
        }

        [Test]
        public async Task Verify_that_POST_request_with_failed_repository_returns_InternalServerError()
        {
            var healthEndPoint = new HealthEndPoint
            {
                Name = "fail-endpoint",
                Url = "https://example.com/healthz"
            };

            var bodyJson = HealthEndPointWriter.Write(healthEndPoint);

            this.mockRepository
                .Setup(r => r.CreateAsync(It.IsAny<HealthEndPoint>()))
                .ReturnsAsync(Result.Fail("Database error"));

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/healthendpoint",
                Body = bodyJson
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.InternalServerError));
        }

        [Test]
        public async Task Verify_that_unsupported_verb_returns_NotImplemented()
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.PATCH,
                Route = "/healthendpoint"
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.NotImplemented));
        }

        [Test]
        public async Task Verify_that_unknown_route_returns_NotFound()
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/unknown"
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.NotFound));
        }

        [Test]
        public async Task Verify_that_response_preserves_CorrelationToken()
        {
            var correlationToken = Guid.NewGuid();

            var request = new ArgusRequest
            {
                CorrelationToken = correlationToken,
                Verb = ArgusVerb.POST,
                Route = "/unknown"
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.CorrelationToken, Is.EqualTo(correlationToken));
        }

        [Test]
        public async Task Verify_that_GET_request_returns_Ok_with_all_endpoints()
        {
            var endpoints = ImmutableList.Create(
                new HealthEndPoint { Identifier = Guid.NewGuid(), Name = "ep1", Url = "https://example.com/1" },
                new HealthEndPoint { Identifier = Guid.NewGuid(), Name = "ep2", Url = "https://example.com/2" }
            );

            this.mockRepository
                .Setup(r => r.ReadAsync(null))
                .ReturnsAsync(endpoints);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = "/healthendpoint"
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));
            Assert.That(response.Body, Is.Not.Null.And.Not.Empty);

            var deserialized = HealthEndPointReader.ReadArray(response.Body);
            Assert.That(deserialized, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task Verify_that_GET_request_with_identifier_returns_single_endpoint()
        {
            var identifier = Guid.NewGuid();

            var endpoints = ImmutableList.Create(
                new HealthEndPoint { Identifier = identifier, Name = "ep1", Url = "https://example.com/1" }
            );

            this.mockRepository
                .Setup(r => r.ReadAsync(It.Is<Guid[]>(ids => ids.Length == 1 && ids[0] == identifier)))
                .ReturnsAsync(endpoints);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));
            Assert.That(response.Body, Is.Not.Null.And.Not.Empty);

            var deserialized = HealthEndPointReader.Read(response.Body);
            Assert.That(deserialized.Identifier, Is.EqualTo(identifier));
        }

        [Test]
        public async Task Verify_that_PUT_request_with_valid_body_updates_endpoint()
        {
            var identifier = Guid.NewGuid();

            var healthEndPoint = new HealthEndPoint
            {
                Identifier = identifier,
                Name = "updated-endpoint",
                Url = "https://example.com/updated",
                Frequency = 60,
                Timeout = 10,
                RetryCount = 5
            };

            var bodyJson = HealthEndPointWriter.Write(healthEndPoint);

            this.mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<HealthEndPoint>()))
                .ReturnsAsync(Result.Ok());

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.PUT,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}",
                Body = bodyJson
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));

            this.mockRepository.Verify(r => r.UpdateAsync(It.Is<HealthEndPoint>(
                ep => ep.Identifier == identifier
                    && ep.Name == "updated-endpoint")), Times.Once);
        }

        [Test]
        public async Task Verify_that_DELETE_request_deletes_endpoint()
        {
            var identifier = Guid.NewGuid();

            this.mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<HealthEndPoint>()))
                .ReturnsAsync(Result.Ok());

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.DELETE,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            var response = await this.service.HandleRequestAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));

            this.mockRepository.Verify(r => r.DeleteAsync(It.Is<HealthEndPoint>(
                ep => ep.Identifier == identifier)), Times.Once);
        }
    }
}
