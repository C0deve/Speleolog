using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using SpeleoLogViewer.Models;
using SpeleoLogViewer.Service;
using SpeleoLogViewer.ViewModels;

namespace SpeleoLogViewer.Views;

public partial class MainWindow : Window
{
    public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, DropHandler);
        AddHandler(DragDrop.DragOverEvent, DragOverHandler);
        DataContext = new MainWindowViewModel(StorageProvider, File.ReadAllLinesAsync, FileSystemWatcherFactory, new SpeleologStateRepository(), new SchedulerProvider());
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