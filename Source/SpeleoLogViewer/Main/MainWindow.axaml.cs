using Avalonia.Controls;
using Avalonia.Input;
using SpeleoLogViewer._BaseClass;

namespace SpeleoLogViewer.Main;

public partial class MainWindow : Window
{
    public MainWindowVM? ViewModel => DataContext as MainWindowVM;
    
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, DropHandler);
        AddHandler(DragDrop.DragOverEvent, DragOverHandler);
    }
    
    private void DragOverHandler(object? sender, DragEventArgs e)
    {
        if (DataContext is IDropTarget dropTarget) dropTarget.DragOver(sender, e);
    }

    private void DropHandler(object? sender, DragEventArgs e)
    {
        if (DataContext is IDropTarget dropTarget) dropTarget.Drop(sender, e);
    }
}