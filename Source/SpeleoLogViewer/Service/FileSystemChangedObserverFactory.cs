using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.Service;

public class FileSystemChangedObserverFactory(Func<string, IFileSystemChangedWatcher> fileSystemObserverFactory)
{
    private readonly Dictionary<string, IObservable<FileSystemEventArgs>> _dictionary = [];

    public IObservable<string[]> GetObservable(
        string filePath, 
        Func<string, CancellationToken, Task<string[]>> getTextAsync, 
        IScheduler scheduler)
    {
        var directoryPath = Path.GetDirectoryName(filePath) ??
                            throw new InvalidOperationException($"Impossible de trouver le repertoir du fichier {filePath}");
        var fileName = Path.GetFileName(filePath);
        
        return GetDirectoryChangedObservable(directoryPath, scheduler)
            .Where(args => string.Equals(args.Name, fileName, StringComparison.InvariantCultureIgnoreCase))
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .Select(_ => Observable.FromAsync(() => getTextAsync(filePath, CancellationToken.None), scheduler))
            .Concat();
    }

    private IObservable<FileSystemEventArgs> GetDirectoryChangedObservable(string directoryPath, IScheduler scheduler)
    {
        var key = _dictionary.Keys.FirstOrDefault(directoryPath.Contains);

        if (key is null) 
            _dictionary.Add(directoryPath, ObserveFolder(directoryPath, fileSystemObserverFactory, scheduler));

        return _dictionary[key ?? directoryPath];
    }

    private static IObservable<FileSystemEventArgs> ObserveFolder(
        string folderPath, 
        Func<string, IFileSystemChangedWatcher> fileSystemWatcherFactory, 
        IScheduler scheduler) =>
        // Observable.Defer enables us to avoid doing any work
        // until we have a subscriber.
        Observable
            .Defer(() => Observable.Return(fileSystemWatcherFactory(folderPath)))
            .ObserveOn(scheduler)
            .SelectMany(fsw => Observable
                    .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => fsw.Changed += h,
                        h => fsw.Changed -= h)
                    // FromEventPattern supplies both the sender and the event
                    // args. Extract just the latter.
                    .Select(ep => ep.EventArgs)
                    .Throttle(TimeSpan.FromMilliseconds(500), scheduler)
                    // The Finally here ensures the watcher gets shut down once
                    // we have no subscribers.
                    .Finally(fsw.Dispose))
            // This combination of Publish and RefCount means that multiple
            // subscribers will get to share a single FileSystemWatcher,
            // but that it gets shut down if all subscribers unsubscribe.
            .Publish()
            .RefCount(TimeSpan.Zero, scheduler:scheduler);
}