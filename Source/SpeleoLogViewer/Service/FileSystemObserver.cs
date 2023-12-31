﻿using System;
using System.IO;
using System.Reactive.Linq;

namespace SpeleoLogViewer.Service;

public static class FileSystemObserver
{
    public static IObservable<FileSystemEventArgs> ObserveFolder(string folderPath, Func<string, IFileSystemWatcher> fileSystemWatcherFactory) =>
        // Observable.Defer enables us to avoid doing any work
        // until we have a subscriber.
        Observable
            .Defer(() => Observable.Return(fileSystemWatcherFactory(folderPath)))
            .SelectMany(fsw =>
                Observable.Merge([
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                            h => fsw.Changed += h, h => fsw.Changed -= h),
                        Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                            h => fsw.Deleted += h, h => fsw.Deleted -= h)
                    ])
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

    public static IFileSystemWatcher FileSystemWatcherFactory(string directoryPath)
    {
        FileSystemWatcher fsw = new(directoryPath);
        fsw.EnableRaisingEvents = true;
        return new FileSystemWatcherWrapper(fsw);
    }
}