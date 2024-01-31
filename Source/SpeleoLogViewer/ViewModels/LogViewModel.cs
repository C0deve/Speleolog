using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Dock.Model.Mvvm.Controls;

namespace SpeleoLogViewer.ViewModels;

public sealed class LogViewModel : Document, IDisposable
{
    private readonly CompositeDisposable _disposables = [];

    public string FilePath { get; }

    public ObservableCollection<LogLineViewModel> AllLines { get; } = [];

    /// <inheritdoc/>
    public LogViewModel(string filePath, IObservable<string[]> fileChangedStream)
    {
        var first = true;
        FilePath = filePath;
        Title = System.IO.Path.GetFileName(FilePath);

        fileChangedStream
            .ObserveOn(SynchronizationContext.Current ?? throw new InvalidOperationException())
            .Scan(AllLines, (logLineViewModels, strings) =>
            {
                for (var i = logLineViewModels.Count; i < strings.Length; i++)
                    logLineViewModels.Insert(0, new LogLineViewModel(strings[i], !first));

                if (first)
                    first = false;
                
                return logLineViewModels;
            })
            .Subscribe(_ => { }, exception => Console.WriteLine(exception))
            .DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}