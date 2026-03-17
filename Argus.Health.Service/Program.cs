// -------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs">
//
//    Copyright (c) 2025-2026 Sam Gerené
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, softwareUseCases
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
//  </copyright>
//  ------------------------------------------------------------------------------------------------

namespace Argus.Health.Service
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;

    using Argus.Health.Service.BackgroundServices;
    using Argus.Health.Service.Repository;

    using ArgusTransfer.Extensions;
    using ArgusTransfer.Routing;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Serilog;

    /// <summary>
    /// Main entry point for the application
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        /// <summary>
        /// Main entry point for the application
        /// </summary>
        /// <param name="args">
        /// command line arguments
        /// </param>
        public static async Task Main(string[] args)
        {
            Console.Title = "Argus Health";

            var builder = Host.CreateApplicationBuilder(args);

            var logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArgusHealthService", "logs");

            Directory.CreateDirectory(logFolder);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(logFolder, "argus-health-service-.log"),
                    rollingInterval: RollingInterval.Day)
                .Enrich.FromLogContext()
                .CreateLogger();

            Log.Debug("Log folder: {LogFolder}", logFolder);

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            if (OperatingSystem.IsWindows())
            {
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "Argus Health Service";
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                builder.Services.AddSystemd();
            }
            else
            {
                Console.Error.WriteLine("The Argus Health Service only supports Windows and Linux.");
                Environment.Exit(1);
            }

            builder.Services.AddHttpClient("ArgusHealth");

            builder.Services.AddArgusModules();
            builder.Services.AddArgusPipeHost(options =>
            {
                options.PipeName = "ArgusHealth";
            });

            builder.Services.AddSingleton<IHealthEndPointRepository, HealthEndPointRepository>();
            builder.Services.AddSingleton<IHealthEndPointCheckResultRepository, HealthEndPointCheckResultRepository>();
            builder.Services.AddHostedService<HealthEndPointBackgroundService>();

            builder.Services.Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true;
            });

            var app = builder.Build();

            try
            {
                var repository = app.Services.GetRequiredService<IHealthEndPointRepository>();
                repository.InitializeDatabase();
                Log.Information("Database initialization complete");

                Log.Information("Starting Argus Health Service...");
                await app.RunAsync();
                Log.Information("Argus Health Service stopped.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Argus Health Service terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
