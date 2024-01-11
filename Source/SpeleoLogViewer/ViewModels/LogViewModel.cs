using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dock.Model.Mvvm.Controls;

namespace SpeleoLogViewer.ViewModels;

public sealed class LogViewModel : Document, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    
    public string Path { get; }

    public ObservableCollection<LogLineViewModel> AllLines { get; } = [];

    /// <inheritdoc/>
    public LogViewModel(string path, IObservable<FileSystemEventArgs> fileChangedStream, Func<string, CancellationToken, Task<string[]>> getTextAsync)
    {
        var first = true;
        Path = path;
        Title = System.IO.Path.GetFileName(Path);

        fileChangedStream
            .Where(args => string.Equals(args.Name, Title, StringComparison.InvariantCultureIgnoreCase))
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default)
            .SelectMany(_ => Observable.FromAsync(() => getTextAsync(Path, CancellationToken.None)))
            .ObserveOn(SynchronizationContext.Current ?? throw new InvalidOperationException())
            .Do(strings =>
            {
                for (var i = AllLines.Count; i < strings.Length; i++) 
                    AllLines.Insert(0, new LogLineViewModel(strings[i], !first));

                if (first) 
                    first = false;
            })
            .Subscribe()
            .DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}