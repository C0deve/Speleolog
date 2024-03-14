using System;
using System.IO;

namespace SpeleoLogViewer.FileChanged;

public interface IFileSystemChangedWatcher : IDisposable
{
    event FileSystemEventHandler Changed;
}