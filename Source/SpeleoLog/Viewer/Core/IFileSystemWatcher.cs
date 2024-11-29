namespace SpeleoLog.Viewer.Core;

public interface IFileSystemWatcher : IDisposable
{
    event FileSystemEventHandler Changed;
}