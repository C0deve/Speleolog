using System.IO;

namespace SpeleoLogViewer.Service;

public sealed class FileSystemObserverWrapper : IFileSystemObserver
{
    private readonly FileSystemWatcher _fileSystemWatcher;

    public FileSystemObserverWrapper(FileSystemWatcher fileSystemWatcher)
    {
        _fileSystemWatcher = fileSystemWatcher;
        _fileSystemWatcher.Changed += (sender, args) => Changed?.Invoke(sender, args);
        _fileSystemWatcher.Deleted += (sender, args) => Deleted?.Invoke(sender, args);
    }

    public void Dispose() => _fileSystemWatcher.Dispose();
    public event FileSystemEventHandler? Changed;
    public event FileSystemEventHandler? Deleted;
}