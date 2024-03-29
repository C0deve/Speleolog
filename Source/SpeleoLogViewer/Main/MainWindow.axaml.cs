using Avalonia.Controls;
using Avalonia.Input;
using SpeleoLogViewer._BaseClass;
using SpeleoLogViewer.ApplicationState;
using SpeleoLogViewer.FileChanged;
using SpeleoLogViewer.LogFileViewer;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleoLogViewer.Main;

public partial class MainWindow : Window
{
    public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, DropHandler);
        AddHandler(DragDrop.DragOverEvent, DragOverHandler);
        DataContext = new MainWindowViewModel(
            StorageProvider, 
            new TextFileLoaderInOneRead(), 
            FileSystemWatcherFactory, 
            new SpeleologStateRepository(), 
            new SchedulerProvider(), 
            new SpeleologTemplateReader());
    }

    private static FileSystemChangedWatcher FileSystemWatcherFactory(string directoryPath) =>
        new(directoryPath);
    
    private void DragOverHandler(object? sender, DragEventArgs e)
    {
        if (DataContext is IDropTarget dropTarget)
        {
            dropTarget.DragOver(sender, e);
        }
    }

    private void DropHandler(object? sender, DragEventArgs e)
    {
        if (DataContext is IDropTarget dropTarget)
        {
            dropTarget.Drop(sender, e);
        }
    }
}