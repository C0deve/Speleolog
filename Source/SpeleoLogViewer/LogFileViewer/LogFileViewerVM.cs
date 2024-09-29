using System;
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
    private readonly Cache _cache = new();
    private readonly BehaviorSubject<IMessage> _refresh = new(IMessage.Initial);
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);
    public TimeSpan AnimationDuration { get; } = TimeSpan.FromSeconds(10);
    public string FilePath { get; }
    public IObservable<IMessage> RefreshStream { get; }
    [Reactive] public string Filter { get; set; }
    [Reactive] public string MaskText { get; set; }
    [Reactive] public string ErrorTag { get; set; }

    [Reactive] public long LoadingDuration { get; private set; }
    public ReactiveCommand<Unit, string> Load { get; }
    private ReactiveCommand<string, Unit> Refresh { get; }
    public ReactiveCommand<Unit, AddToBottom> NextPage { get; }

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
        var paginator = new Paginator<int>([], lineCountByPage);
        var taskpoolScheduler = scheduler ?? RxApp.TaskpoolScheduler;

        Load = ReactiveCommand.CreateFromTask( () => LoadFileContentAsync(filePath, textFileLoader));

        NextPage = ReactiveCommand.CreateFromObservable(() =>
            Observable
                .Return((_cache.FromIndex(paginator.Next()), MaskText, ErrorTag))
                .LogToAggregateStream(DoMaskText)
                .Select(array => new AddToBottom(array))
        );
        
        Refresh = ReactiveCommand.Create<string>(input =>  _cache.Refresh(input));
        
        var getChanges = _cache
            .Added
            .Select(text => DoFilter(Filter, text))
            .Select(text => DoMaskText(MaskText, text))
            .Select(Reverse) // Aggregate need to be done in reverse
            .Select(rows => LogAggregator.AggregateLog(rows, ErrorTag))
            .Select(aggregates => aggregates.Reverse()) // Cancel the reverse because aggregate are displayed from top to bottom
            .Select(aggregates => aggregates.ToImmutableArray())
            .Where(s => s.Length != 0)
            .Select(array => new AddToTop(array));
        
        Load
            .Take(1)
            .Do(input => _cache.Init(input))
            .Subscribe()
            .DisposeWith(_disposable);

        _cache
            .Initialized
            .Do(index => paginator = new Paginator<int>(index, lineCountByPage))
            .ToUnit()
            .InvokeCommand(NextPage)
            .DisposeWith(_disposable);

        Load
            .Skip(1)
            .InvokeCommand(Refresh)
            .DisposeWith(_disposable);

        fileChangedStream // changes fromm file system stream
            .SkipUntil(_cache.Initialized)
            .ObserveOn(taskpoolScheduler)
            .InvokeCommand(Load)
            .DisposeWith(_disposable);

        this.WhenAnyValue(vm => vm.Filter, vm => vm.MaskText, vm => vm.ErrorTag, (filter, _, _) => filter)
            .SkipUntil(_cache.Initialized)
            .Throttle(_throttleTime, taskpoolScheduler)
            .Do(filter => paginator = new Paginator<int>(_cache.Contains(filter), lineCountByPage))
            .Do(_ => _refresh.OnNext(IMessage.DeleteAll))
            .ToUnit()
            .InvokeCommand(NextPage);

        getChanges.Cast<IMessage>()
            .Merge(NextPage)
            .Subscribe(_refresh)
            .DisposeWith(_disposable);

        Load.Execute().Subscribe();
    }


    private static IEnumerable<string> DoMaskText(string mask, IEnumerable<string> actualText)
    {
        if (string.IsNullOrWhiteSpace(mask))
            return actualText;

        return actualText.Select(row =>
            row.Replace(mask, string.Empty, StringComparison.InvariantCultureIgnoreCase));
    }

    private static IEnumerable<string> DoFilter(string filter, IEnumerable<string> actualText)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return actualText;

        return actualText
            .Where(row => row.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
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
    
    private static IEnumerable<string> Reverse(IEnumerable<string> input) =>
        input.Reverse();

    public void Dispose()
    {
        Debug.WriteLine($"disposing {Title}");
        _disposable.Dispose();
    }
}