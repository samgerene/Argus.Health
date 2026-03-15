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

using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ArgusTransfer.Client;

using Argus.Health.Client;
using Argus.Health.Pulse.Services;
using Argus.Health.Pulse.ViewModels;
using Argus.Health.Pulse.Views;

namespace Argus.Health.Pulse
{
    public partial class App : Application
    {
        private MainWindowViewModel? mainViewModel;
        private IEndpointSyncService? syncService;
        private IHealthCheckService? healthCheckService;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // manual construction — flat dependency graph, no DI container needed
                var argusClient = new ArgusClient("ArgusHealth");
                var healthEndPointClient = new HealthEndPointClient(argusClient);

                syncService = new EndpointSyncService(healthEndPointClient);
                healthCheckService = new HealthCheckService();

                mainViewModel = new MainWindowViewModel(healthEndPointClient, syncService, healthCheckService);

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
                    mainViewModel.Dispose();
                    syncService.Dispose();
                    healthCheckService.Dispose();
                    desktop.Shutdown();
                };

                desktop.ShutdownRequested += (_, _) =>
                {
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
