using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SpeleoLogViewer.ApplicationState;
using MainWindow = SpeleoLogViewer.Main.MainWindow;

namespace SpeleoLogViewer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var desktopMainWindow = new MainWindow();
            desktop.MainWindow = desktopMainWindow;

            desktop.MainWindow.Closing += (_, _) =>
            {
                var state = new SpeleologState(
                    (desktopMainWindow.ViewModel?.OpenFiles ?? Enumerable.Empty<string>()).ToList(), 
                    desktopMainWindow.ViewModel?.AppendFromBottom ?? true);
                _ = new SpeleologStateRepository().SaveAsync(state);
                desktopMainWindow.ViewModel?.CloseLayout();
            };
            desktop.Exit += (_, _) => { desktopMainWindow.ViewModel?.CloseLayout(); };
        }

        base.OnFrameworkInitializationCompleted();
    }
}