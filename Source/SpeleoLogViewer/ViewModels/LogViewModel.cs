using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
        FilePath = filePath;
        Title = System.IO.Path.GetFileName(FilePath);

        var firstLines = fileChangedStream
                .Take(1)
                .SelectMany(lines => lines)
                .Select(line => new LogLineViewModel(line));

        var justAppend = fileChangedStream
            .Skip(1)
            .SelectMany(lines => lines.Skip(AllLines.Count))
            .Select(line => new LogLineViewModel(line, true));
        
        firstLines
            .Merge(justAppend)
            //.Do(lineVM => AllLines.Insert(0, lineVM))
            .Do(lineVM => AllLines.Add(lineVM))
            .Subscribe(_ => { }, exception => Console.WriteLine(exception))
            .DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}