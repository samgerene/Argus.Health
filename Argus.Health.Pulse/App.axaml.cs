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
    using System.Reactive.Linq;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using Avalonia.Threading;

    using Argus.Health.Pulse.Client;
    using Argus.Health.Pulse.Services;
    using Argus.Health.Pulse.ViewModels;
    using Argus.Health.Pulse.Views;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ReactiveUI.Avalonia;

    using Serilog;

    /// <summary>
    /// Avalonia application class
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The main window view model instance
        /// </summary>
        private MainWindowViewModel? mainViewModel;

        /// <summary>
        /// The endpoint sync service instance
        /// </summary>
        private IEndpointSyncService? syncService;

        /// <summary>
        /// The health check service instance
        /// </summary>
        private IHealthCheckService? healthCheckService;

        /// <summary>
        /// Gets or sets the application-wide DI service provider
        /// </summary>
        public static ServiceProvider? Services { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the application should start minimized to the system tray
        /// </summary>
        public static bool StartMinimized { get; set; }

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
                this.syncService = Services!.GetRequiredService<IEndpointSyncService>();
                this.healthCheckService = Services!.GetRequiredService<IHealthCheckService>();
                var loggerFactory = Services!.GetRequiredService<ILoggerFactory>();
                var autoStartService = Services!.GetRequiredService<IAutoStartService>();
                var toastService = Services!.GetRequiredService<IToastNotificationService>();
                var pipeNameProvider = Services!.GetRequiredService<PipeNameProvider>();

                this.mainViewModel = new MainWindowViewModel(healthEndPointClient, this.syncService, this.healthCheckService, autoStartService, loggerFactory, pipeNameProvider);
                this.DataContext = this.mainViewModel;

                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                this.mainViewModel.ExitRequested += (_, _) =>
                {
                    Log.Information("Exit requested, shutting down");
                    toastService.Dispose();
                    this.mainViewModel.Dispose();
                    this.syncService.Dispose();
                    this.healthCheckService.Dispose();
                    desktop.Shutdown();
                };

                desktop.ShutdownRequested += (_, _) =>
                {
                    Log.Information("Desktop shutdown requested");
                    toastService.Dispose();
                    this.mainViewModel.Dispose();
                    this.syncService.Dispose();
                    this.healthCheckService.Dispose();
                };

                this.mainViewModel.ToggleWindowRequested += (_, _) =>
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

                this.mainViewModel.ShowWindowRequested += (_, _) =>
                {
                    mainWindow.Show();
                    mainWindow.Activate();
                };

                this.mainViewModel.ShowAboutRequested += (_, _) =>
                {
                    var aboutWindow = new AboutWindow { DataContext = new AboutViewModel() };

                    if (mainWindow.IsVisible)
                    {
                        aboutWindow.ShowDialog(mainWindow);
                    }
                    else
                    {
                        aboutWindow.Show();
                    }
                };

                // close-to-tray: hide window instead of closing when user clicks X
                mainWindow.Closing += (_, e) =>
                {
                    e.Cancel = true;
                    mainWindow.Hide();
                };

                // start hidden when launched with --minimized
                if (StartMinimized)
                {
                    mainWindow.Opened += (_, _) => mainWindow.Hide();
                }

                // show OS toast notifications for failures when window is hidden
                this.healthCheckService.FailureObservable
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .Subscribe(result =>
                    {
                        if (!mainWindow.IsVisible)
                        {
                            var endpointName = this.mainViewModel.ResolveEndpointName(result.HealthEndPoint)
                                               ?? $"Endpoint {result.HealthEndPoint}";

                            toastService.ShowEndpointFailure(endpointName, result.StatusCode, result.ErrorMessage);
                        }
                    });

                // handle toast click: show the main window
                toastService.ToastActivated += (_, _) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        mainWindow.Show();
                        mainWindow.Activate();
                    });
                };

                // set window and tray icon from .ico copied to output directory
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "argus-health-pulse-icon.ico");
                var windowIcon = new WindowIcon(iconPath);
                mainWindow.Icon = windowIcon;

                var trayIcons = TrayIcon.GetIcons(this);

                if (trayIcons != null)
                {
                    foreach (var trayIcon in trayIcons)
                    {
                        trayIcon.Icon = windowIcon;
                    }
                }

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
