﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using SpeleoLogViewer.ViewModels;

namespace SpeleoLogViewer.Service;

public class FileSystemChangedObserverFactory(Func<string, IFileSystemChangedWatcher> fileSystemObserverFactory)
{
    private readonly Dictionary<string, IObservable<FileSystemEventArgs>> _dictionary = [];
    public static readonly TimeSpan ThrottleDuration = TimeSpan.FromMilliseconds(500);

    public IObservable<IEnumerable<string>> GetObservable(
        string filePath,
        ITextFileLoader textFileLoader,
        IScheduler? scheduler = null)
    {
        var directoryPath = Path.GetDirectoryName(filePath) ??
                            throw new InvalidOperationException($"Impossible de trouver le repertoir du fichier {filePath}");
        var fileName = Path.GetFileName(filePath);

        var taskpoolScheduler = scheduler ?? Scheduler.Default;
        return GetDirectoryChangedObservable(directoryPath, taskpoolScheduler)
            .Where(args => string.Equals(args.Name, fileName, StringComparison.InvariantCultureIgnoreCase))
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .Select(_ => Observable.FromAsync(async () =>
            {
                var sw = new Stopwatch();
                sw.Start();

                var textAsync = await textFileLoader.GetTextAsync(filePath, CancellationToken.None);

                sw.Stop();
                Console.WriteLine(sw.ElapsedMilliseconds);

                return textAsync;
            }, taskpoolScheduler))
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
            .SelectMany(fsw => Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    handler => fsw.Changed += handler,
                    handler => fsw.Changed -= handler)
                // FromEventPattern supplies both the sender and the event
                // args. Extract just the latter.
                .Select(ep => ep.EventArgs)
                .Throttle(ThrottleDuration, scheduler)
                // The Finally here ensures the watcher gets shut down once
                // we have no subscribers.
                .Finally(fsw.Dispose))
            // This combination of Publish and RefCount means that multiple
            // subscribers will get to share a single FileSystemWatcher,
            // but that it gets shut down if all subscribers unsubscribe.
            .Publish()
            .RefCount(TimeSpan.Zero, scheduler: scheduler);
}