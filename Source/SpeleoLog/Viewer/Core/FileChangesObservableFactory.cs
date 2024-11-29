namespace SpeleoLog.Viewer.Core;

public class FileChangesObservableFactory(Func<string, IFileSystemWatcher> fileSystemObserverFactory)
{
    private readonly Dictionary<string, IObservable<FileSystemEventArgs>> _observablesByFolderCache = [];
    public static readonly TimeSpan ThrottleDuration = TimeSpan.FromMilliseconds(500);

    public IObservable<Unit> Build(
        string filePath,
        IScheduler? scheduler = null)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var directoryPath = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException($"Null parent directory. File: {filePath}");
        var fileName = Path.GetFileName(filePath);

        var taskpoolScheduler = scheduler ?? Scheduler.Default;
        return GetObservableDirectory(directoryPath, taskpoolScheduler)
                .Where(args => string.Equals(args.Name, fileName, StringComparison.InvariantCultureIgnoreCase))
                .Select(_ => Unit.Default);
    }

    /// <summary>
    /// Return an hot observable of all files changes for the given directory path.
    /// </summary>
    /// <remarks>Create the observable only if the given folder (or it's parent) is not observed.</remarks>
    /// <param name="directoryPath"></param>
    /// <param name="scheduler"></param>
    /// <returns></returns>
    private IObservable<FileSystemEventArgs> GetObservableDirectory(string directoryPath, IScheduler scheduler)
    {
        var key = _observablesByFolderCache.Keys.FirstOrDefault(directoryPath.StartsWith);

        if (key is null)
            _observablesByFolderCache.Add(directoryPath, ObserveFolder(directoryPath, fileSystemObserverFactory, scheduler));

        return _observablesByFolderCache[key ?? directoryPath];
    }

    private static IObservable<FileSystemEventArgs> ObserveFolder(
        string folderPath,
        Func<string, IFileSystemWatcher> fileSystemWatcherFactory,
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