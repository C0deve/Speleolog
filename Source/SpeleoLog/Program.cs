using Avalonia.Dialogs;

namespace SpeleoLog;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var buildAvaloniaApp = AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
        
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            buildAvaloniaApp.UseManagedSystemDialogs();
    
        return buildAvaloniaApp;
    }
}