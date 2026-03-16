// -------------------------------------------------------------------------------------------------
//   <copyright file="HealthEndPointCheckResultRepositoryTestFixture.cs">
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
    /// Suite of tests for the <see cref="HealthEndPointCheckResultRepository"/> class
    /// </summary>
    [TestFixture]
    public class HealthEndPointCheckResultRepositoryTestFixture
    {
        private HealthEndPointCheckResultRepository checkResultRepository;

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

            File.Copy(originalFile, this.databaseFilePath, true);

            // Initialize the database to ensure the HealthEndPointCheckResults table exists
            var endpointRepoLogger = this.loggerFactory.CreateLogger<HealthEndPointRepository>();
            var endpointRepo = new HealthEndPointRepository(endpointRepoLogger, this.databaseFolderPath);
            endpointRepo.InitializeDatabase();

            var logger = this.loggerFactory.CreateLogger<HealthEndPointCheckResultRepository>();
            this.checkResultRepository = new HealthEndPointCheckResultRepository(logger, this.databaseFolderPath);
        }

        [Test]
        public async Task Verify_that_HealthEndPointCheckResult_can_be_created_and_read()
        {
            var knownEndpointIdentifier = Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da");

            var checkResult = new HealthEndPointCheckResult
            {
                Timestamp = DateTime.UtcNow,
                StatusCode = 200,
                ErrorMessage = null,
                HealthEndPoint = knownEndpointIdentifier
            };

            var result = await this.checkResultRepository.CreateAsync(checkResult);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(checkResult.Identifier, Is.Not.EqualTo(Guid.Empty));

            var checkResults = await this.checkResultRepository.ReadAsync();
            Assert.That(checkResults.Any(x => x.Identifier == checkResult.Identifier), Is.True);

            var readBack = checkResults.Single(x => x.Identifier == checkResult.Identifier);
            Assert.That(readBack.StatusCode, Is.EqualTo(200));
            Assert.That(readBack.ErrorMessage, Is.Null);
            Assert.That(readBack.HealthEndPoint, Is.EqualTo(knownEndpointIdentifier));
        }

        [Test]
        public async Task Verify_that_HealthEndPointCheckResult_can_be_read_filtered_by_endpoint()
        {
            var endpointA = Guid.Parse("cfb2e590-eed6-4223-b2ba-271ed0cb06da");
            var endpointB = Guid.Parse("fe08550c-d926-4758-808f-a9a991e0ff37");

            var resultA = new HealthEndPointCheckResult
            {
                Timestamp = DateTime.UtcNow,
                StatusCode = 200,
                HealthEndPoint = endpointA
            };

            var resultB = new HealthEndPointCheckResult
            {
                Timestamp = DateTime.UtcNow,
                StatusCode = 503,
                ErrorMessage = "Service Unavailable",
                HealthEndPoint = endpointB
            };

            await this.checkResultRepository.CreateAsync(resultA);
            await this.checkResultRepository.CreateAsync(resultB);

            var filteredResults = await this.checkResultRepository.ReadAsync(endpointA);
            Assert.That(filteredResults.Count, Is.EqualTo(1));
            Assert.That(filteredResults.Single().HealthEndPoint, Is.EqualTo(endpointA));
            Assert.That(filteredResults.Single().StatusCode, Is.EqualTo(200));

            filteredResults = await this.checkResultRepository.ReadAsync(endpointB);
            Assert.That(filteredResults.Count, Is.EqualTo(1));
            Assert.That(filteredResults.Single().HealthEndPoint, Is.EqualTo(endpointB));
            Assert.That(filteredResults.Single().StatusCode, Is.EqualTo(503));
        }

        [Test]
        public void Verify_that_CreateAsync_throws_ArgumentNullException_when_null()
        {
            Assert.That(async () => await this.checkResultRepository.CreateAsync(null),
                Throws.TypeOf<ArgumentNullException>());
        }
    }
}
