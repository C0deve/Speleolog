using Avalonia.Controls.ApplicationLifetimes;
using SpeleoLog._Infrastructure;
using SpeleoLog.Main;
using FileSystemWatcher = SpeleoLog._Infrastructure.FileSystemWatcher;
using MainWindow = SpeleoLog.Main.MainWindow;

namespace SpeleoLog;

public class App : Application
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
                new SpeleologTemplateRepositoryV2(),
                () => new FileLoader());
            
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
    
    private static FileSystemWatcher FileSystemWatcherFactory(string directoryPath) =>
        new(directoryPath);

}