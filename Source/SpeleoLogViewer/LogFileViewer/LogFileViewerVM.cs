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
using Dock.Model.ReactiveUI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SpeleoLogViewer.LogFileViewer;

public sealed class LogFileViewerVM : Document, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private Cache _cache = new([]);
    private readonly Subject<ImmutableArray<LogLinesAggregate>> _changes = new();
    private readonly Subject<ImmutableArray<LogLinesAggregate>> _pageChanges = new();
    private readonly BehaviorSubject<ImmutableArray<LogLinesAggregate>> _refreshAll = new(ImmutableArray<LogLinesAggregate>.Empty);
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);
    private bool _allDataAreDisplayed;
    private int _actualLength;
    [Reactive] private short CurrentPage { get; set; }
    public TimeSpan AnimationDuration { get; } = TimeSpan.FromSeconds(10);
    public string FilePath { get; }
    public IObservable<ImmutableArray<LogLinesAggregate>> ChangesStream { get; }
    public IObservable<ImmutableArray<LogLinesAggregate>> RefreshAllStream { get; }
    public IObservable<ImmutableArray<LogLinesAggregate>> PageChangesStream { get; }
    [Reactive] public string Filter { get; set; }
    [Reactive] public string MaskText { get; set; }
    [Reactive] public string ErrorTag { get; set; }

    [Reactive] public long LoadingDuration { get; private set; }
    public ReactiveCommand<Unit, string> Reload { get; }

    public LogFileViewerVM(
        string filePath,
        IObservable<Unit> fileChangedStream,
        ITextFileLoader textFileLoader,
        int lineCountByPage,
        string errorTag,
        IScheduler? scheduler = null)
    {
        ErrorTag = errorTag;
        ChangesStream = _changes.AsObservable();
        RefreshAllStream = _refreshAll.AsObservable();
        PageChangesStream = _pageChanges.AsObservable();

        Filter = string.Empty;
        MaskText = string.Empty;
        FilePath = filePath;
        Title = Path.GetFileName(FilePath);

        var taskpoolScheduler = scheduler ?? RxApp.TaskpoolScheduler;

        Reload = ReactiveCommand.CreateFromObservable(() =>
            LoadFileContentAsync(filePath, textFileLoader, taskpoolScheduler));

        var firstFileContentLoading =
            LoadFileContentAsync(filePath, textFileLoader, taskpoolScheduler) // File loading on creation
                .Select(s => (Split:Split(s), TotalLength: s.Length))
                .Select(input => (Cache: new Cache(input.Split), input.TotalLength))
                .Do(input =>
                {
                    _cache = input.Cache;
                    _actualLength = input.TotalLength;
                })
                .Publish();

        var fileContentChangedStream =
            fileChangedStream // changes fromm file system stream
                .SkipUntil(firstFileContentLoading)
                .Select(_ => LoadFileContentAsync(filePath, textFileLoader, taskpoolScheduler))
                .Concat()
                .Merge(Reload)
                .Select(newText => new Data(newText, _actualLength))
                .Publish();

        fileContentChangedStream // Changes stream
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
            .Subscribe(_changes)
            .DisposeWith(_disposable);

        var filterOrMaskOrErrorTagStream =
            this.WhenAnyValue(vm => vm.Filter, vm => vm.MaskText, vm => vm.ErrorTag, (filter, mask, errorTag1) => (Filter: filter, Mask: mask, ErrorTag: errorTag1))
                .SkipUntil(firstFileContentLoading)
                .Throttle(_throttleTime, taskpoolScheduler)
                .DistinctUntilChanged()
                .Select(data => (Text: _cache.Values, data.Mask, data.Filter))
                .Select(data => (Text: DoFilter(data.Filter, data.Text), data.Mask))
                .Select(data => DoMaskText(data.Mask, data.Text));

        firstFileContentLoading // Refresh all stream
            .Select(input => input.Cache.Values)
            .Merge(filterOrMaskOrErrorTagStream)
            .Do(_ =>
            {
                CurrentPage = 0;
                _allDataAreDisplayed = false;
            })
            .Select(text => TakeLast(lineCountByPage, text, 0))
            .Select(Reverse)
            .Select(lines => LogAggregator.Aggregate(lines, ErrorTag))
            .Select(aggregates => aggregates.ToImmutableArray())
            .Subscribe(_refreshAll)
            .DisposeWith(_disposable);

        this.WhenAnyValue(vm => vm.CurrentPage) // Pages stream
            .SkipUntil(firstFileContentLoading)
            .Where(page => page > 0)
            .Select(page => (Text: _cache.Values, NewPage: page))
            .Select(data => (Text: DoFilter(Filter, data.Text), data.NewPage))
            .Select(data => TakeLast(lineCountByPage, data.Text, data.NewPage))
            .Select(text => DoMaskText(MaskText, text))
            .Select(Reverse)
            .Select(lines => LogAggregator.Aggregate(lines, ErrorTag))
            .Select(aggregates => aggregates.ToImmutableArray())
            .Do(text => _allDataAreDisplayed = text.Length == 0)
            .Where(_ => !_allDataAreDisplayed)
            .Subscribe(_pageChanges)
            .DisposeWith(_disposable);

        fileContentChangedStream
            .Connect()
            .DisposeWith(_disposable);

        firstFileContentLoading
            .Connect()
            .DisposeWith(_disposable);
    }

    public void DisplayNextPage()
    {
        if (_allDataAreDisplayed) return;

        CurrentPage++;
        Console.WriteLine($"{nameof(DisplayNextPage)} page:{CurrentPage}");
    }

    private static string[] Split(string actualText)
    {
        using (new Watcher("Split"))
        {
            return actualText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private static IEnumerable<string> TakeLast(int lineCount, IEnumerable<string> actualText, int actualPageIndex) =>
        actualText
            .SkipLast(lineCount * actualPageIndex)
            .TakeLast(lineCount);

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

    private IObservable<string> LoadFileContentAsync(string filePath, ITextFileLoader textFileLoader, IScheduler taskpoolScheduler) =>
        Observable.FromAsync(async () =>
        {
            using (new Watcher("File loading"))
            {
                var watch = Stopwatch.StartNew();
                var textAsync = await textFileLoader.GetTextAsync(filePath, CancellationToken.None);
                watch.Stop();
                LoadingDuration = watch.ElapsedMilliseconds;
                return textAsync;
            }
        }, taskpoolScheduler);


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

    public void Dispose() => _disposable.Dispose();
}

internal record Data(string NewText, int ActualLength);