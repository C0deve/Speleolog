﻿using System;
using System.IO;
using System.Reactive.Linq;

namespace SpeleoLogViewer.Service;

public static class FileSystemObserver
{
    public static IObservable<FileSystemEventArgs> Observe(string file) =>
        // Observable.Defer enables us to avoid doing any work
        // until we have a subscriber.
        Observable.Defer(() =>
            {
                var directoryName = Path.GetDirectoryName(file) ?? throw new ApplicationException($"Impossible de trouver le repertoir du fichier {file}");
                var fileName = Path.GetFileName(file);
                FileSystemWatcher fsw = new(directoryName,fileName);
                fsw.EnableRaisingEvents = true;
                return Observable.Return(fsw);
            })
            .SelectMany(fsw =>
                Observable.Merge(new[]
                    {
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                            h => fsw.Created += h, h => fsw.Created -= h),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                            h => fsw.Changed += h, h => fsw.Changed -= h),
                        Observable.FromEventPattern<RenamedEventHandler, FileSystemEventArgs>(
                            h => fsw.Renamed += h, h => fsw.Renamed -= h),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                            h => fsw.Deleted += h, h => fsw.Deleted -= h)
                    })
                    // FromEventPattern supplies both the sender and the event
                    // args. Extract just the latter.
                    .Select(ep => ep.EventArgs)
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    // The Finally here ensures the watcher gets shut down once
                    // we have no subscribers.
                    .Finally(fsw.Dispose))
            // This combination of Publish and RefCount means that multiple
            // subscribers will get to share a single FileSystemWatcher,
            // but that it gets shut down if all subscribers unsubscribe.
            .Publish()
            .RefCount();
}