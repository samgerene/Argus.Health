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

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Hosting.WindowsServices;
    using Microsoft.Extensions.Logging;

    using Serilog;

    /// <summary>
    /// Main entry point for the application
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        /// <summary>
        /// The root application data folder for ArgusHealthService
        /// </summary>
        public static readonly string ApplicationDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArgusHealthService");

        /// <summary>
        /// Main entry point for the application
        /// </summary>
        /// <param name="args">
        /// command line arguments
        /// </param>
        public static async Task Main(string[] args)
        {
            if (!OperatingSystem.IsWindows() || !WindowsServiceHelpers.IsWindowsService())
            {
                try
                {
                    Console.Title = "Argus Health";
                }
                catch (IOException)
                {
                    // No console attached — ignore.
                }
            }

            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
#if DEBUG
                ?? "Development";
#else
                ?? "Production";
#endif

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                EnvironmentName = environmentName
            });

            var logFolder = Path.Combine(ApplicationDataFolder, "logs");
            Directory.CreateDirectory(logFolder);
            var logFilePath = Path.Combine(logFolder, "argus-health-service-.log");

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

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
                await Console.Error.WriteLineAsync("The Argus Health Service only supports Windows and Linux.");
                Environment.Exit(1);
            }

            var argusHealthOptions = builder.Configuration
                .GetSection("ArgusHealth")
                .Get<ArgusHealthOptions>() ?? new ArgusHealthOptions();

            builder.Services.Configure<ArgusHealthOptions>(builder.Configuration.GetSection("ArgusHealth"));

            builder.Services.AddHttpClient("ArgusHealth");

            builder.Services.AddArgusModules();
            builder.Services.AddArgusPipeHost(options =>
            {
                options.PipeName = argusHealthOptions.PipeName;
            });

            Log.Information(
                "Argus Health Service listening on pipe {PipeName} (env: {Environment})",
                argusHealthOptions.PipeName,
                environmentName);

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
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
