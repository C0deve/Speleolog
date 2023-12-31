using System.Threading.Tasks;
using Avalonia.Input;

namespace SpeleoLogViewer.ViewModels;

public interface IDropTarget
{
    void DragOver(object? sender, DragEventArgs e);
    Task Drop(object? sender, DragEventArgs e);
}
