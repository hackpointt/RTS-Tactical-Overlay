using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RTS_Tactical_Overlay.Services;
using RTS_Tactical_Overlay.ViewModels;

namespace RTS_Tactical_Overlay;

public partial class App : Application
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Allocate console for debug output
        AllocConsole();
        Console.WriteLine("=== RTS Tactical Overlay Debug Console ===");

        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<INativeInputSimulator, NativeInputSimulator>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IStageExecutor, StageExecutor>();
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
