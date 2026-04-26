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
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
//  </copyright>
//  ------------------------------------------------------------------------------------------------

namespace Argus.Health.Pulse
{
    using System;
    using System.IO;
    using System.Linq;

    using Avalonia;
    using ReactiveUI.Avalonia;

    using ArgusTransfer.Client;

    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Services;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Serilog;

    /// <summary>
    /// Application entry point
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The root application data folder for ArgusHealthPulse
        /// </summary>
        public static readonly string ApplicationDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArgusHealthPulse");

        /// <summary>
        /// Application entry point
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            var logFolder = Path.Combine(ApplicationDataFolder, "logs");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(logFolder, "pulse-.log"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Argus Health Pulse starting");

                var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
#if DEBUG
                    ?? "Development";
#else
                    ?? "Production";
#endif

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
                    .Build();

                var pipeName = configuration["ArgusHealth:PipeName"] ?? "ArgusHealth";

                Log.Information("Argus Health Pulse connecting to pipe {PipeName} (env: {Environment})", pipeName, environmentName);

                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddSerilog());
                services.AddSingleton(new PipeNameProvider(pipeName));
                services.AddSingleton(new ArgusClient(pipeName));
                services.AddSingleton<HealthEndPointClient>();
                services.AddSingleton<IEndpointSyncService, EndpointSyncService>();
                services.AddSingleton<IHealthCheckService, HealthCheckService>();

                if (OperatingSystem.IsWindows())
                {
                    services.AddSingleton<IAutoStartService, WindowsAutoStartService>();
                    services.AddSingleton<IToastNotificationService, WindowsToastNotificationService>();
                }
                else
                {
                    services.AddSingleton<IAutoStartService, NullAutoStartService>();
                    services.AddSingleton<IToastNotificationService, NullToastNotificationService>();
                }

                App.Services = services.BuildServiceProvider();

                App.StartMinimized = args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);

                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Argus Health Pulse terminated unexpectedly");
            }
            finally
            {
                Log.Information("Argus Health Pulse shutting down");

                if (App.Services is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Builds the Avalonia application
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI(_ => { })
                .RegisterReactiveUIViewsFromAssemblyOf<App>();
        }
    }
}
