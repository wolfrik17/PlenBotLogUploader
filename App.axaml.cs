using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.Services;
using PlenBotLogUploader.ViewModels;
using PlenBotLogUploader.Views;
using System;

namespace PlenBotLogUploader;

public partial class App : Application
{
    private LogUploaderService _service;
    private MainWindow _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Apply saved theme
        var theme = ApplicationSettings.Current.Theme;
        RequestedThemeVariant = theme switch
        {
            "Dark" => ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };

        _service = new LogUploaderService();
        _service.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel();
            _mainWindow = new MainWindow
            {
                DataContext = vm,
            };

            desktop.MainWindow = _mainWindow;

            // Check if started minimized with -m flag
            var args = Environment.GetCommandLineArgs();
            var startMinimized = Array.Exists(args, a => a.Equals("-m", StringComparison.OrdinalIgnoreCase));
            if (startMinimized && ApplicationSettings.Current.MinimiseToTray)
            {
                _mainWindow.WindowState = Avalonia.Controls.WindowState.Minimized;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayOpenClicked(object sender, EventArgs e)
    {
        if (_mainWindow is not null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    private void OnTrayToggleMonitoringClicked(object sender, EventArgs e)
    {
        if (_service is not null)
        {
            if (_service.IsMonitoring)
            {
                _service.StopMonitoring();
            }
            else
            {
                _service.StartMonitoring(ApplicationSettings.Current.LogsLocation);
            }
        }
    }

    private void OnTrayToggleSessionClicked(object sender, EventArgs e)
    {
        if (_service is not null)
        {
            if (!_service.IsSessionRunning)
            {
                _service.StartSession();
            }
            else
            {
                _ = _service.StopSessionAsync();
            }
        }
    }

    private void OnTrayExitClicked(object sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
