using Dock.Model.Mvvm.Controls;

namespace SpeleoLogViewer.LogFileViewer.Dockable;

public class DockableLogFileVM : Document
{
    public DockableLogFileVM(LogFileViewerVM logFileViewerVM)
    {
        LogFileViewerVM = logFileViewerVM;
        Title = System.IO.Path.GetFileName(LogFileViewerVM.FilePath);
    }
    
    public LogFileViewerVM LogFileViewerVM { get; }
}