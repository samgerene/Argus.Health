// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointRepositoryTestFixture.cs">
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

namespace Argus.Health.Service.Tests.Repository
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    
    using Argus.Health.Common.Model;
    using Argus.Health.Service.Repository;

    using Microsoft.Extensions.Logging;

    using Serilog;

    /// <summary>
    /// Suite of tests for the <see cref="HealthEndPointRepository"/> class
    /// </summary>
    [TestFixture]
    public class HealthEndPointRepositoryTestFixture
    {
        private HealthEndPointRepository healthEndPointRepository;

        private ILoggerFactory loggerFactory;

        private string databaseFolderPath;
        private string databaseFilePath;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            this.loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });

            this.databaseFolderPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDataCopy");
            this.databaseFilePath = Path.Combine(this.databaseFolderPath, "ArgusHealth.sqlite");
            Directory.CreateDirectory(this.databaseFolderPath);
        }

        [SetUp]
        public void SetUp()
        {
            var originalFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ArgusHealth.sqlite");

            File.Copy(originalFile, this.databaseFilePath,true);  

            var logger = this.loggerFactory.CreateLogger<HealthEndPointRepository>();

            this.healthEndPointRepository = new HealthEndPointRepository(logger, this.databaseFolderPath);
        }
        
        [Test]
        public async Task Verify_that_HealthEndPoint_can_be_read_and_created()
        {
            var healthEndPoint = new HealthEndPoint
            {
                Name = "some-endpoint",
                Url = "https://cdp4services-public.cdp4.org/healthz",
                Frequency = 19,
                RetryCount = 2,
                Timeout = 4
            };

            var eventRaised = false;
            HealthEndPoint? addedEndpoint = null;

            this.healthEndPointRepository.EndpointAdded+= (sender, e) =>
            {
                eventRaised = true;
                addedEndpoint = e;
            };

            // Act & Assert create succeeds
            Assert.That(async () => await this.healthEndPointRepository.CreateAsync(healthEndPoint), Throws.Nothing);

            // Assert event was raised
            Assert.That(eventRaised, Is.True, "Expected EndpointAdded event to be raised.");
            Assert.That(addedEndpoint, Is.Not.Null);
            Assert.That(addedEndpoint?.Identifier, Is.EqualTo(healthEndPoint.Identifier));

            // Confirm endpoint is added to database
            var healthEndPoints = await this.healthEndPointRepository.ReadAsync();

            Assert.That(healthEndPoints.Any(x => x.Identifier == healthEndPoint.Identifier), Is.True);
        }

        [Test]
        public async Task Verify_that_HealthEndPoint_can_be_deleted()
        {
            var healthEndPoint = new HealthEndPoint { Identifier = Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da") };

            var eventRaised = false;
            HealthEndPoint? removedEndpoint = null;

            this.healthEndPointRepository.EndpointRemoved += (sender, e) =>
            {
                eventRaised = true;
                removedEndpoint = e;
            };

            // Act & Assert delete succeeds
            Assert.That(async () => await this.healthEndPointRepository.DeleteAsync(healthEndPoint), Throws.Nothing);

            // Assert event was raised
            Assert.That(eventRaised, Is.True, "Expected EndpointRemoved event to be raised.");
            Assert.That(removedEndpoint, Is.Not.Null);
            Assert.That(removedEndpoint?.Identifier, Is.EqualTo(healthEndPoint.Identifier));

            // Confirm endpoint is gone from database
            var healthEndPoints = await this.healthEndPointRepository.ReadAsync();
            Assert.That(healthEndPoints.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Verify_that_HealthEndPoint_can_be_read()
        {
            var healthEndPoints = await this.healthEndPointRepository.ReadAsync();

            Assert.That(healthEndPoints.Count, Is.EqualTo(2));

            var knownIdentifiers = new[]
            {
                Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da"), 
                Guid.Parse("fe08550c-d926-4758-808f-a9a991e0ff37")
            };

            healthEndPoints = await this.healthEndPointRepository.ReadAsync(knownIdentifiers);

            Assert.That(healthEndPoints.Count, Is.EqualTo(2));

            knownIdentifiers = new[]
            {
                Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da"),
                Guid.Parse("fe08550c-d926-4758-808f-a9a991e0ff37"),
                Guid.NewGuid()
            };
            healthEndPoints = await this.healthEndPointRepository.ReadAsync(knownIdentifiers);

            Assert.That(healthEndPoints.Count, Is.EqualTo(2));

            healthEndPoints = await this.healthEndPointRepository.ReadAsync(new [] {Guid.NewGuid(), Guid.NewGuid() });
            Assert.That(healthEndPoints, Is.Empty);
        }

        [Test]
        public async Task Verify_that_InitializeDatabase_creates_tables()
        {
            var emptyFolderPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDataEmpty");
            Directory.CreateDirectory(emptyFolderPath);

            var emptyDbPath = Path.Combine(emptyFolderPath, "ArgusHealth.sqlite");
            if (File.Exists(emptyDbPath))
            {
                File.Delete(emptyDbPath);
            }

            var logger = this.loggerFactory.CreateLogger<HealthEndPointRepository>();
            var repo = new HealthEndPointRepository(logger, emptyFolderPath);

            repo.InitializeDatabase();

            var healthEndPoints = await repo.ReadAsync();
            Assert.That(healthEndPoints, Is.Empty);
        }

        [Test]
        public async Task Verify_that_InitializeDatabase_with_force_recreates_tables()
        {
            var healthEndPoints = await this.healthEndPointRepository.ReadAsync();
            Assert.That(healthEndPoints.Count, Is.EqualTo(2));

            this.healthEndPointRepository.InitializeDatabase(force: true);

            healthEndPoints = await this.healthEndPointRepository.ReadAsync();
            Assert.That(healthEndPoints, Is.Empty);
        }

        [Test]
        public void Verify_that_CreateAsync_throws_ArgumentNullException_when_null()
        {
            Assert.That(async () => await this.healthEndPointRepository.CreateAsync(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Verify_that_UpdateAsync_throws_ArgumentNullException_when_null()
        {
            Assert.That(async () => await this.healthEndPointRepository.UpdateAsync(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Verify_that_DeleteAsync_throws_ArgumentNullException_when_null()
        {
            Assert.That(async () => await this.healthEndPointRepository.DeleteAsync(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public async Task Verify_that_HealthEndPoint_can_be_read_and_updated()
        {
            var healthEndPoints =
                await this.healthEndPointRepository.ReadAsync(new[] { Guid.Parse("fe08550c-d926-4758-808f-a9a991e0ff37") });

            var eventRaised = false;
            HealthEndPoint updatedEndpoint = null;

            this.healthEndPointRepository.EndpointUpdated += (sender, e) =>
            {
                eventRaised = true;
                updatedEndpoint = e;
            };

            var hep = healthEndPoints.Single();

            hep.Name = "updated";
            hep.Frequency = 6;
            hep.Url = "http://some-other-url";
            hep.RetryCount = 66;
            hep.Timeout = 666;

            await this.healthEndPointRepository.UpdateAsync(hep);

            // Assert event was raised
            Assert.That(eventRaised, Is.True, "Expected EndpointUpdated event to be raised.");
            Assert.That(updatedEndpoint, Is.Not.Null);
            Assert.That(updatedEndpoint?.Identifier, Is.EqualTo(hep.Identifier));
            Assert.That(updatedEndpoint?.Name, Is.EqualTo(hep.Name));
            Assert.That(updatedEndpoint?.Frequency, Is.EqualTo(hep.Frequency));
            Assert.That(updatedEndpoint?.RetryCount, Is.EqualTo(hep.RetryCount));
            Assert.That(updatedEndpoint?.Timeout, Is.EqualTo(hep.Timeout));
            Assert.That(updatedEndpoint?.Url, Is.EqualTo(hep.Url));
        }
    }
}
