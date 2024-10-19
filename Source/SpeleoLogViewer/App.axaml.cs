using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SpeleoLogViewer.ApplicationState;
using SpeleoLogViewer.FileChanged;
using SpeleoLogViewer.Main;
using SpeleoLogViewer.SpeleologTemplate;
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
            var lastState = new StateRepository().Get() ?? SpeleologState.Default;
            
            desktop.MainWindow.DataContext = new MainWindowVM(
                desktopMainWindow.StorageProvider,
                desktop.MainWindow.Launcher, 
                FileSystemWatcherFactory, 
                lastState, 
                new SchedulerProvider(), 
                new SpeleologTemplateRepository());
            
            desktop.MainWindow.Closing += (_, _) =>
            {
                if(desktopMainWindow.ViewModel is null) return;
                
                var newState = desktopMainWindow.ViewModel.State;
                _ = new StateRepository().SaveAsync(newState);
                desktopMainWindow.ViewModel.CloseLayout();
            };
            desktop.Exit += (_, _) => { desktopMainWindow.ViewModel?.CloseLayout(); };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private static FileSystemChangedWatcher FileSystemWatcherFactory(string directoryPath) =>
        new(directoryPath);

}