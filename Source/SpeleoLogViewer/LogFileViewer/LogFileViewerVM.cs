﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Dock.Model.ReactiveUI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SpeleoLogViewer.LogFileViewer;

public sealed class LogFileViewerVM : Document, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private Cache _cache = new([]);
    private readonly BehaviorSubject<IMessage> _refresh = new(IMessage.Initial);
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);
    private int _actualLength;

    public TimeSpan AnimationDuration { get; } = TimeSpan.FromSeconds(10);
    public string FilePath { get; }
    public IObservable<IMessage> RefreshStream { get; }
    [Reactive] public string Filter { get; set; }
    [Reactive] public string MaskText { get; set; }
    [Reactive] public string ErrorTag { get; set; }

    [Reactive] public long LoadingDuration { get; private set; }
    public ReactiveCommand<Unit, string> Load { get; }
    public ReactiveCommand<Unit, AddToBottom> NextPage { get; }
    private ReactiveCommand<Data, AddToTop> GetChanges { get; }

    public LogFileViewerVM(
        string filePath,
        IObservable<Unit> fileChangedStream,
        ITextFileLoader textFileLoader,
        int lineCountByPage,
        string errorTag,
        IScheduler? scheduler = null)
    {
        ErrorTag = errorTag;
        RefreshStream = _refresh.AsObservable().Where(message => message is not Initial);

        Filter = string.Empty;
        MaskText = string.Empty;
        FilePath = filePath;
        Title = Path.GetFileName(FilePath);
        var paginator = new Paginator<int>(Enumerable.Empty<int>(), lineCountByPage);
        var taskpoolScheduler = scheduler ?? RxApp.TaskpoolScheduler;

        Load = ReactiveCommand.CreateFromTask(
            () => LoadFileContentAsync(filePath, textFileLoader),
            outputScheduler: taskpoolScheduler
        );

        NextPage = ReactiveCommand.CreateFromObservable(() =>
            Observable
                .Return((_cache.FromIndex(paginator.Next()), MaskText, ErrorTag))
                .LogToAggregateStream(DoMaskText)
                .Select(array => new AddToBottom(array))
        );

        GetChanges = ReactiveCommand.CreateFromObservable<Data, AddToTop>(data => Observable
            .Return(data)
            .Select(Diff)
            .Select(Split)
            .Do(change => _cache.Add(change))
            .Select(text => DoFilter(Filter, text))
            .Select(text => DoMaskText(MaskText, text))
            .Select(Reverse) // Aggregate need to be done in reverse
            .Select(lines => LogAggregator.Aggregate(lines, ErrorTag))
            .Select(aggregates => aggregates.Reverse()) // Cancel the reverse because aggregate are displayed from top to bottom
            .Select(aggregates => aggregates.ToImmutableArray())
            .Where(s => s.Length != 0)
            .Select(array => new AddToTop(array))
        );

        var initialLoad = Load
            .Take(1)
            .Select(s => (Split: Split(s), TotalLength: s.Length))
            .Select(input => (Cache: new Cache(input.Split), input.TotalLength))
            .Do(input =>
            {
                _cache = input.Cache;
                _actualLength = input.TotalLength;
                paginator = new Paginator<int>(input.Cache.AllIndex, lineCountByPage);
            })
            .ToUnit()
            .Publish();

        initialLoad.InvokeCommand(NextPage);

        Load
            .SkipUntil(initialLoad)
            .Select(newText => new Data(newText, _actualLength))
            .InvokeCommand(GetChanges);

        fileChangedStream // changes fromm file system stream
            .SkipUntil(initialLoad)
            .InvokeCommand(Load);

        this.WhenAnyValue(vm => vm.Filter, vm => vm.MaskText, vm => vm.ErrorTag, (filter, _, _) => filter)
            .SkipUntil(initialLoad)
            .Throttle(_throttleTime, taskpoolScheduler)
            .Do(filter => paginator = new Paginator<int>(_cache.Contains(filter), lineCountByPage))
            .Do(_ => _refresh.OnNext(IMessage.DeleteAll))
            .ToUnit()
            .InvokeCommand(NextPage);

        GetChanges.Cast<IMessage>()
            .Merge(NextPage)
            .Subscribe(_refresh)
            .DisposeWith(_disposable);

        initialLoad.Connect().DisposeWith(_disposable);
        Load.Execute().Subscribe();
    }


    private static string[] Split(string actualText)
    {
        using (new Watcher("Split"))
        {
            return actualText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private static IEnumerable<string> DoMaskText(string mask, IEnumerable<string> actualText)
    {
        if (string.IsNullOrWhiteSpace(mask))
            return actualText;

        return actualText.Select(line =>
            line.Replace(mask, string.Empty, StringComparison.InvariantCultureIgnoreCase));
    }

    private static IEnumerable<string> DoFilter(string filter, IEnumerable<string> actualText)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return actualText;

        return actualText
            .Where(line => line.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task<string> LoadFileContentAsync(string filePath, ITextFileLoader textFileLoader)
    {
        using var watcher = new Watcher("File loading");
        var watch = Stopwatch.StartNew();
        var textAsync = await textFileLoader.GetTextAsync(filePath, CancellationToken.None);
        watch.Stop();
        LoadingDuration = watch.ElapsedMilliseconds;
        return textAsync;
    }


    private static string Diff(Data input)
    {
        var actualTextLength = input.ActualLength;
        var newTextLength = input.NewText.Length;

        return actualTextLength <= newTextLength
            ? input.NewText.Substring(actualTextLength, newTextLength - actualTextLength) // file size increases so logs have been added : keep only the changes 
            : Environment.NewLine + input.NewText; // file size decreases so it is a creation. keep all text and add an artificial line break
    }

    private static IEnumerable<string> Reverse(IEnumerable<string> input) =>
        input.Reverse();

    public void Dispose()
    {
        Console.WriteLine($"disposing {Title}");
        _disposable.Dispose();
    }
}

internal record Data(string NewText, int ActualLength);