using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SpeleoLogViewer.Models;
using SpeleoLogViewer.ViewModels;
using SpeleoLogViewer.Views;

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
            var mainWindowViewModel = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            desktop.MainWindow.Closing += (_, _) =>
            {
                var state = new SpeleologState(mainWindowViewModel.OpenFiles.ToList());
                SpeleologStateRepository.SaveAsync(state);
                mainWindowViewModel.CloseLayout();
            };
            desktop.Exit += (_, _) => { mainWindowViewModel.CloseLayout(); };
        }

        base.OnFrameworkInitializationCompleted();
    }
}