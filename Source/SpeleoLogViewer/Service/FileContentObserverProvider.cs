using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.Service;

public class FileContentObserverProvider(Func<string, IFileSystemObserver> fileSystemObserverFactory)
{
    private readonly Dictionary<string, IObservable<FileSystemEventArgs>> _dictionary = [];

    public IObservable<string[]> GetObservable(string filePath, Func<string, CancellationToken, Task<string[]>> getTextAsync)
    {
        var directoryPath = Path.GetDirectoryName(filePath) ??
                            throw new InvalidOperationException($"Impossible de trouver le repertoir du fichier {filePath}");
        var fileName = Path.GetFileName(filePath);
        
        return GetDirectoryChangedObservable(directoryPath)
            .Where(args => string.Equals(args.Name, fileName, StringComparison.InvariantCultureIgnoreCase))
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .SelectMany(_ => Observable.FromAsync(() => getTextAsync(filePath, CancellationToken.None)));
    }

    private IObservable<FileSystemEventArgs> GetDirectoryChangedObservable(string directoryPath)
    {
        var key = _dictionary.Keys.FirstOrDefault(directoryPath.Contains);

        if (key is null) 
            _dictionary.Add(directoryPath, FileSystemObserver.ObserveFolder(directoryPath, fileSystemObserverFactory));

        return _dictionary[key ?? directoryPath];
    }
}