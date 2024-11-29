namespace SpeleoLog.FileChanged;

public interface IFileSystemChangedWatcher : IDisposable
{
    event FileSystemEventHandler Changed;
}