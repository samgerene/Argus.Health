// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointModuleTestFixture.cs">
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

namespace Argus.Health.Service.Tests.Modules
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;

    using Argus.Health.Common.Model;
    using Argus.Health.Common.Serialization;
    using Argus.Health.Service.Modules;
    using Argus.Health.Service.Repository;

    using ArgusTransfer.Extensions;
    using ArgusTransfer.Protocol;
    using ArgusTransfer.Routing;

    using FluentResults;

    using Microsoft.Extensions.Logging;

    using Moq;

    /// <summary>
    /// Suite of tests for the <see cref="HealthEndPointModule"/> class
    /// </summary>
    [TestFixture]
    public class HealthEndPointModuleTestFixture
    {
        private HealthEndPointModule module;

        private Mock<ILogger<HealthEndPointModule>> mockLogger;

        private Mock<IHealthEndPointRepository> mockRepository;

        [SetUp]
        public void SetUp()
        {
            this.mockLogger = new Mock<ILogger<HealthEndPointModule>>();
            this.mockRepository = new Mock<IHealthEndPointRepository>();

            var mockCheckResultRepository = new Mock<IHealthEndPointCheckResultRepository>();
            this.module = new HealthEndPointModule(this.mockLogger.Object, this.mockRepository.Object, mockCheckResultRepository.Object);
        }

        [Test]
        public async Task Verify_that_AddRoutes_registers_expected_routes()
        {
            var router = new ArgusRouter();

            this.module.AddRoutes(router);

            // Verify by routing a POST request — it should reach the handler (not return NotFound)
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/healthendpoint",
                Body = ""
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await router.RouteAsync(context);

            // The handler returns BadRequest for empty body, proving the route was registered
            Assert.That(context.Response.StatusCode, Is.EqualTo(ArgusStatusCode.BadRequest));
        }

        [Test]
        public async Task Verify_that_HandleCreate_with_valid_body_returns_Created()
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

            var context = new ArgusContext(request, CancellationToken.None);
            await this.module.HandleCreateAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Created));

            this.mockRepository.Verify(r => r.CreateAsync(It.Is<HealthEndPoint>(
                ep => ep.Name == "test-endpoint"
                    && ep.Url == "https://example.com/healthz")), Times.Once);
        }

        [Test]
        public async Task Verify_that_HandleCreate_with_empty_body_returns_BadRequest()
        {
            var request = new ArgusRequest
            {
                Verb = ArgusVerb.POST,
                Route = "/healthendpoint",
                Body = ""
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await this.module.HandleCreateAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.BadRequest));

            this.mockRepository.Verify(r => r.CreateAsync(It.IsAny<HealthEndPoint>()), Times.Never);
        }

        [Test]
        public async Task Verify_that_HandleCreate_with_failed_repository_returns_InternalServerError()
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

            var context = new ArgusContext(request, CancellationToken.None);
            await this.module.HandleCreateAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.InternalServerError));
        }

        [Test]
        public async Task Verify_that_HandleGetAll_returns_Ok_with_endpoints()
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

            var context = new ArgusContext(request, CancellationToken.None);
            await this.module.HandleGetAllAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));
            Assert.That(response.Body, Is.Not.Null.And.Not.Empty);

            var deserialized = HealthEndPointReader.ReadArray(response.Body);
            Assert.That(deserialized, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task Verify_that_HandleGetById_returns_Ok_with_endpoint()
        {
            var identifier = Guid.NewGuid();

            var endpoints = ImmutableList.Create(
                new HealthEndPoint { Identifier = identifier, Name = "ep1", Url = "https://example.com/1" }
            );

            this.mockRepository
                .Setup(r => r.ReadAsync(It.Is<Guid[]>(ids => ids.Length == 1 && ids[0] == identifier)))
                .ReturnsAsync(endpoints);

            var router = new ArgusRouter();
            this.module.AddRoutes(router);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await router.RouteAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));
            Assert.That(response.Body, Is.Not.Null.And.Not.Empty);

            var deserialized = HealthEndPointReader.Read(response.Body);
            Assert.That(deserialized.Identifier, Is.EqualTo(identifier));
        }

        [Test]
        public async Task Verify_that_HandleGetById_returns_NotFound_when_not_exists()
        {
            var identifier = Guid.NewGuid();

            this.mockRepository
                .Setup(r => r.ReadAsync(It.IsAny<Guid[]>()))
                .ReturnsAsync(ImmutableList<HealthEndPoint>.Empty);

            var router = new ArgusRouter();
            this.module.AddRoutes(router);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.GET,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await router.RouteAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.NotFound));
        }

        [Test]
        public async Task Verify_that_HandleUpdate_with_valid_body_returns_Ok()
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

            var router = new ArgusRouter();
            this.module.AddRoutes(router);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.PUT,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}",
                Body = bodyJson
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await router.RouteAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));
            Assert.That(response.Body, Is.Not.Null.And.Not.Empty);

            this.mockRepository.Verify(r => r.UpdateAsync(It.Is<HealthEndPoint>(
                ep => ep.Identifier == identifier
                    && ep.Name == "updated-endpoint")), Times.Once);
        }

        [Test]
        public async Task Verify_that_HandleUpdate_with_empty_body_returns_BadRequest()
        {
            var identifier = Guid.NewGuid();

            var router = new ArgusRouter();
            this.module.AddRoutes(router);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.PUT,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}",
                Body = ""
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await router.RouteAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.BadRequest));

            this.mockRepository.Verify(r => r.UpdateAsync(It.IsAny<HealthEndPoint>()), Times.Never);
        }

        [Test]
        public async Task Verify_that_HandleDelete_returns_Ok()
        {
            var identifier = Guid.NewGuid();

            this.mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<HealthEndPoint>()))
                .ReturnsAsync(Result.Ok());

            var router = new ArgusRouter();
            this.module.AddRoutes(router);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.DELETE,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await router.RouteAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.Ok));

            this.mockRepository.Verify(r => r.DeleteAsync(It.Is<HealthEndPoint>(
                ep => ep.Identifier == identifier)), Times.Once);
        }

        [Test]
        public async Task Verify_that_HandleDelete_with_failed_repository_returns_InternalServerError()
        {
            var identifier = Guid.NewGuid();

            this.mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<HealthEndPoint>()))
                .ReturnsAsync(Result.Fail("Database error"));

            var router = new ArgusRouter();
            this.module.AddRoutes(router);

            var request = new ArgusRequest
            {
                Verb = ArgusVerb.DELETE,
                Route = $"/healthendpoint/{identifier.ToShortGuid()}"
            };

            var context = new ArgusContext(request, CancellationToken.None);
            await router.RouteAsync(context);
            var response = context.Response;

            Assert.That(response.StatusCode, Is.EqualTo(ArgusStatusCode.InternalServerError));
        }
    }
}
