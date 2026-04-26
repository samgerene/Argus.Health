// -------------------------------------------------------------------------------------------------
//  <copyright file="EndpointStatusViewModelTestFixture.cs">
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
    using System.Linq;

    using Argus.Health.Common.Model;
    using Argus.Health.Pulse.ViewModels;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="EndpointStatusViewModel"/> class
    /// </summary>
    [TestFixture]
    public class EndpointStatusViewModelTestFixture
    {
        private const int MaxHistorySize = 20160;

        [Test]
        public void Verify_that_constructor_assigns_identifier_name_url_and_active_state()
        {
            var identifier = Guid.NewGuid();

            var viewModel = new EndpointStatusViewModel(identifier, "api", "https://example.com", true);

            Assert.That(viewModel.Identifier, Is.EqualTo(identifier));
            Assert.That(viewModel.Name, Is.EqualTo("api"));
            Assert.That(viewModel.Url, Is.EqualTo("https://example.com"));
            Assert.That(viewModel.IsActive, Is.True);
            Assert.That(viewModel.History, Is.Empty);
        }

        [Test]
        public void Verify_that_StatusCode_change_drives_IsHealthy_via_observable()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);

            Assert.That(viewModel.IsHealthy, Is.False);

            viewModel.StatusCode = 200;
            Assert.That(viewModel.IsHealthy, Is.True);

            viewModel.StatusCode = 503;
            Assert.That(viewModel.IsHealthy, Is.False);
        }

        [Test]
        public void Verify_that_StatusDisplay_returns_Pending_when_no_status_and_no_error()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);

            Assert.That(viewModel.StatusDisplay, Is.EqualTo("Pending"));
        }

        [Test]
        public void Verify_that_StatusDisplay_returns_error_message_when_status_is_zero_and_error_set()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true)
            {
                StatusCode = 0,
                ErrorMessage = "connection refused"
            };

            Assert.That(viewModel.StatusDisplay, Is.EqualTo("connection refused"));
        }

        [Test]
        public void Verify_that_StatusDisplay_returns_status_code_string_when_status_is_non_zero()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true)
            {
                StatusCode = 200
            };

            Assert.That(viewModel.StatusDisplay, Is.EqualTo("200"));
        }

        [Test]
        public void Verify_that_AddCheckResult_appends_history_and_updates_reactive_properties()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);
            var timestamp = DateTime.UtcNow;

            var result = new HealthEndPointCheckResult
            {
                StatusCode = 200,
                Timestamp = timestamp,
                ErrorMessage = null,
                ResponseTimeMs = 42
            };

            viewModel.AddCheckResult(result);

            Assert.That(viewModel.History.Count, Is.EqualTo(1));
            Assert.That(viewModel.StatusCode, Is.EqualTo(200));
            Assert.That(viewModel.LastChecked, Is.EqualTo(timestamp));
            Assert.That(viewModel.ErrorMessage, Is.Null);
            Assert.That(viewModel.ResponseTimeMs, Is.EqualTo(42));
        }

        [Test]
        public void Verify_that_AddCheckResult_trims_history_when_max_size_exceeded()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);

            for (var i = 0; i < MaxHistorySize + 1; i++)
            {
                viewModel.AddCheckResult(new HealthEndPointCheckResult
                {
                    StatusCode = 200,
                    Timestamp = DateTime.UtcNow.AddMilliseconds(i),
                    ResponseTimeMs = i
                });
            }

            Assert.That(viewModel.History.Count, Is.EqualTo(MaxHistorySize));
            Assert.That(viewModel.History[0].ResponseTimeMs, Is.EqualTo(1));
        }

        [Test]
        public void Verify_that_AddCheckResults_with_empty_collection_is_a_noop()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);

            viewModel.AddCheckResults(new List<HealthEndPointCheckResult>());

            Assert.That(viewModel.History, Is.Empty);
            Assert.That(viewModel.StatusCode, Is.EqualTo(0));
            Assert.That(viewModel.LastChecked, Is.Null);
        }

        [Test]
        public void Verify_that_AddCheckResults_appends_in_order_and_updates_from_last_entry()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);
            var firstTimestamp = DateTime.UtcNow.AddSeconds(-30);
            var lastTimestamp = DateTime.UtcNow;

            var batch = new List<HealthEndPointCheckResult>
            {
                new() { StatusCode = 200, Timestamp = firstTimestamp, ResponseTimeMs = 10 },
                new() { StatusCode = 503, Timestamp = lastTimestamp, ErrorMessage = "down", ResponseTimeMs = 99 }
            };

            viewModel.AddCheckResults(batch);

            Assert.That(viewModel.History.Count, Is.EqualTo(2));
            Assert.That(viewModel.StatusCode, Is.EqualTo(503));
            Assert.That(viewModel.LastChecked, Is.EqualTo(lastTimestamp));
            Assert.That(viewModel.ErrorMessage, Is.EqualTo("down"));
            Assert.That(viewModel.ResponseTimeMs, Is.EqualTo(99));
        }

        [Test]
        public void Verify_that_AddCheckResults_trims_excess_using_RemoveRange()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);

            var batch = Enumerable.Range(0, MaxHistorySize + 50)
                .Select(i => new HealthEndPointCheckResult
                {
                    StatusCode = 200,
                    Timestamp = DateTime.UtcNow.AddMilliseconds(i),
                    ResponseTimeMs = i
                })
                .ToList();

            viewModel.AddCheckResults(batch);

            Assert.That(viewModel.History.Count, Is.EqualTo(MaxHistorySize));
            Assert.That(viewModel.History[0].ResponseTimeMs, Is.EqualTo(50));
        }

        [Test]
        public void Verify_that_RecentResponseTimes_returns_last_20_response_times()
        {
            var viewModel = new EndpointStatusViewModel(Guid.NewGuid(), "api", "https://example.com", true);

            for (var i = 0; i < 25; i++)
            {
                viewModel.AddCheckResult(new HealthEndPointCheckResult
                {
                    StatusCode = 200,
                    Timestamp = DateTime.UtcNow.AddMilliseconds(i),
                    ResponseTimeMs = i
                });
            }

            Assert.That(viewModel.RecentResponseTimes.Count, Is.EqualTo(20));
            Assert.That(viewModel.RecentResponseTimes[0], Is.EqualTo(5));
            Assert.That(viewModel.RecentResponseTimes[19], Is.EqualTo(24));
        }
    }
}
