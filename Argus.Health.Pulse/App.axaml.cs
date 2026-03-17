// -------------------------------------------------------------------------------------------------
//  <copyright file="App.axaml.cs">
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

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;

    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Services;
    using Argus.Health.Pulse.ViewModels;
    using Argus.Health.Pulse.Views;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Serilog;

    /// <summary>
    /// Avalonia application class
    /// </summary>
    public partial class App : Application
    {
        private MainWindowViewModel? mainViewModel;
        private IEndpointSyncService? syncService;
        private IHealthCheckService? healthCheckService;

        /// <summary>
        /// Gets or sets the application-wide DI service provider
        /// </summary>
        public static ServiceProvider? Services { get; set; }

        /// <summary>
        /// Initializes the application
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Called when the framework initialization is completed
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            Log.Information("OnFrameworkInitializationCompleted");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var healthEndPointClient = Services!.GetRequiredService<HealthEndPointClient>();
                syncService = Services!.GetRequiredService<IEndpointSyncService>();
                healthCheckService = Services!.GetRequiredService<IHealthCheckService>();
                var loggerFactory = Services!.GetRequiredService<ILoggerFactory>();

                mainViewModel = new MainWindowViewModel(healthEndPointClient, syncService, healthCheckService, loggerFactory);

                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // tray icon events
                mainViewModel.ToggleWindowRequested += (_, _) =>
                {
                    if (mainWindow.IsVisible)
                    {
                        mainWindow.Hide();
                    }
                    else
                    {
                        mainWindow.Show();
                        mainWindow.Activate();
                    }
                };

                mainViewModel.ShowWindowRequested += (_, _) =>
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                };

                mainViewModel.ExitRequested += (_, _) =>
                {
                    Log.Information("Exit requested, shutting down");
                    mainViewModel.Dispose();
                    syncService.Dispose();
                    healthCheckService.Dispose();
                    desktop.Shutdown();
                };

                desktop.ShutdownRequested += (_, _) =>
                {
                    Log.Information("Desktop shutdown requested");
                    mainViewModel.Dispose();
                    syncService.Dispose();
                    healthCheckService.Dispose();
                };

                // set window and tray icon from .ico copied to output directory
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "argus-health-pulse-icon.ico");
                var windowIcon = new WindowIcon(iconPath);
                mainWindow.Icon = windowIcon;

                var trayIcons = TrayIcon.GetIcons(this);

                if (trayIcons != null && trayIcons.Count > 0)
                {
                    trayIcons[0].Icon = windowIcon;
                }

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
