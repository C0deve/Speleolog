using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeleoLogViewer.Service;

namespace SpeleoLogViewer.ViewModels;

public class FileObserverProvider
{
    private readonly Dictionary<string, IObservable<FileSystemEventArgs>> _dictionary = [];

    public IObservable<FileSystemEventArgs> GetObservable(string filePath)
    {
        var directoryPath = Path.GetDirectoryName(filePath) ??
                            throw new InvalidOperationException(
                                $"Impossible de trouver le repertoir du fichier {filePath}");

        var key = _dictionary.Keys.FirstOrDefault(directoryPath.Contains);

        if (key is not null)
            return _dictionary[key];

        _dictionary.Add(directoryPath, FileSystemObserver.ObserveFolder(directoryPath, FileSystemObserver.FileSystemWatcherFactory));

        return _dictionary[directoryPath];
    }
}