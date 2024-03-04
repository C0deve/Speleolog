using System.IO;

namespace SpeleoLogViewer.Service;

public sealed class FileSystemChangedWatcher : IFileSystemChangedWatcher
{
    private readonly FileSystemWatcher _fileSystemWatcher;

    public FileSystemChangedWatcher(string directoryPath)
    {
        _fileSystemWatcher = new FileSystemWatcher(directoryPath);
        _fileSystemWatcher.EnableRaisingEvents = true;
        _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
        
        _fileSystemWatcher.Changed += OnFileSystemWatcherOnChanged;
    }

    private void OnFileSystemWatcherOnChanged(object sender, FileSystemEventArgs args) => 
        Changed?.Invoke(sender, args);

    public void Dispose()
    {
        _fileSystemWatcher.Changed -= OnFileSystemWatcherOnChanged;
        _fileSystemWatcher.Dispose();
    }

    public event FileSystemEventHandler? Changed;
}