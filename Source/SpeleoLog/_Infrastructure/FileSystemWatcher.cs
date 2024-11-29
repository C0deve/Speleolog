namespace SpeleoLog._Infrastructure;

/// <summary>
/// Wrapper around <see cref="System.IO.FileSystemWatcher"/>
/// Track only changed events
/// </summary>
public sealed class FileSystemWatcher : IFileSystemWatcher
{
    private readonly System.IO.FileSystemWatcher _fileSystemWatcher;

    public FileSystemWatcher(string directoryPath)
    {
        _fileSystemWatcher = new System.IO.FileSystemWatcher(directoryPath);
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