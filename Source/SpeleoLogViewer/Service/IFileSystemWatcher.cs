using System;
using System.IO;

namespace SpeleoLogViewer.Service;

public interface IFileSystemWatcher : IDisposable
{
    event FileSystemEventHandler Changed;
    event FileSystemEventHandler Deleted;
}