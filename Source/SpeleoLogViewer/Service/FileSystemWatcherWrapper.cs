using System.IO;

namespace SpeleoLogViewer.Service;

public sealed class FileSystemWatcherWrapper : IFileSystemWatcher
{
    private readonly FileSystemWatcher _fileSystemWatcher;

    public FileSystemWatcherWrapper(FileSystemWatcher fileSystemWatcher)
    {
        _fileSystemWatcher = fileSystemWatcher;
        _fileSystemWatcher.Changed += (sender, args) => Changed?.Invoke(sender, args);
        _fileSystemWatcher.Deleted += (sender, args) => Deleted?.Invoke(sender, args);
    }

    public void Dispose() => _fileSystemWatcher.Dispose();
    public event FileSystemEventHandler? Changed;
    public event FileSystemEventHandler? Deleted;
}