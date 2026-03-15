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
        public static void Main(string[] args)
        {
            Console.Title = "Argus Health";

            var builder = Host.CreateApplicationBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
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
                Console.Error.WriteLine("The Argus Health Service only supports Windows and Linux.");
                Environment.Exit(1);
            }

            // Register named HttpClient with resilience policies
            builder.Services.AddHttpClient("ArgusHealth");

            builder.Services.AddArgusModules();
            builder.Services.AddArgusPipeHost();
            
            builder.Services.AddSingleton<IHealthEndPointRepository, HealthEndPointRepository>();
            builder.Services.AddHostedService<HealthEndPointBackgroundService>();
            
            builder.Services.Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true;
            });

            var app = builder.Build();
            
            try
            {
                Log.Information("Starting Argus Health Service...");
                app.Run();
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
