using System;
using System.IO;

namespace SpeleoLogViewer.Service;

public interface IFileSystemObserver : IDisposable
{
    event FileSystemEventHandler Changed;
    event FileSystemEventHandler Deleted;
}