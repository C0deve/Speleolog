using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace SpeleoLogViewer.ViewModels;

public partial class LogViewModel : Document, IDisposable
{
    private readonly CompositeDisposable _disposables = [];

    [ObservableProperty] private string _maskText = string.Empty;
    
    [ObservableProperty] private string _filter = string.Empty;
    private readonly HashSet<LogLineViewModel> _original = new();

    public string FilePath { get; }

    public bool AppendFromBottom { get; }

    public ObservableCollection<LogLineViewModel> AllLines { get; } = [];

    /// <inheritdoc/>
    public LogViewModel(string filePath, IObservable<IEnumerable<string>> fileChangedStream, bool appendFromBottom)
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
            .Do(AddLine)
            .Subscribe(_ => { }, exception => Console.WriteLine(exception))
            .DisposeWith(_disposables);
        
        AppendFromBottom = appendFromBottom;
    }

    private void AddLine(LogLineViewModel lineVM)
    {
        lineVM.Mask(MaskText);
        _original.Add(lineVM);
        Display(lineVM);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    partial void OnMaskTextChanged(string value)
    {
        foreach (var logLineViewModel in _original) 
            logLineViewModel.Mask(value);
    }
    
    partial void OnFilterChanged(string _)
    {
        AllLines.Clear();
        foreach (var logLineViewModel in _original) Display(logLineViewModel);
    }

    private void Display(LogLineViewModel lineVM)
    {
        if(IsFiltered(lineVM))
            return;
        
        if(AppendFromBottom)
            AllLines.Add(lineVM);
        else
            AllLines.Insert(0, lineVM);
    }

    private  bool IsFiltered(LogLineViewModel model) => 
        !string.IsNullOrWhiteSpace(Filter) && !model.Text.Contains(Filter, StringComparison.InvariantCultureIgnoreCase);
}