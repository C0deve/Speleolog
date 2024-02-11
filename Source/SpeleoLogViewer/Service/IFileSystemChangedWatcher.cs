using System;
using System.IO;

namespace SpeleoLogViewer.Service;

public interface IFileSystemChangedWatcher : IDisposable
{
    event FileSystemEventHandler Changed;
}