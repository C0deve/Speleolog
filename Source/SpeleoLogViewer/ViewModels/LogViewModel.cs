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

    public bool AppendFromBottom { get; }

    public ObservableCollection<LogLineViewModel> AllLines { get; } = [];

    /// <inheritdoc/>
    public LogViewModel(string filePath, IObservable<string[]> fileChangedStream, bool appendFromBottom)
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
            .Do(lineVM =>
            {
                if(AppendFromBottom)
                    AllLines.Add(lineVM);
                else
                    AllLines.Insert(0, lineVM);
            })
            .Subscribe(_ => { }, exception => Console.WriteLine(exception))
            .DisposeWith(_disposables);
        
        AppendFromBottom = appendFromBottom;
    }

    public void Dispose() => _disposables.Dispose();

    public void Mask(string maskText)
    {
        foreach (var logLineViewModel in AllLines)
        {
            logLineViewModel.Mask(maskText);
        }
    }
}