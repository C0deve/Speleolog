using Avalonia.Input;

namespace SpeleoLogViewer._BaseClass;

public interface IDropTarget
{
    void DragOver(object? sender, DragEventArgs e);
    void Drop(object? sender, DragEventArgs e);
}
