using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SpeleoLogViewer.LogFileViewer;

public sealed class LogFileViewerVM : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private string _actualText = "";
    private readonly Subject<string> _changes = new();
    private readonly Subject<string> _pageChanges = new();
    private readonly BehaviorSubject<string> _refreshAll = new("");
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);
    private bool _allDataAreDisplayed;
    [Reactive] private short CurrentPage { get; set; }
    public TimeSpan AnimationDuration { get; } = TimeSpan.FromSeconds(10);
    public string FilePath { get; }
    public IObservable<string> ChangesStream { get; }
    public IObservable<string> RefreshAllStream { get; }
    public IObservable<string> PageChangesStream { get; }
    [Reactive] public string Filter { get; set; }
    [Reactive] public string MaskText { get; set; }

    public LogFileViewerVM(
        string filePath,
        IObservable<Unit> fileChangedStream,
        ITextFileLoader textFileLoader,
        int lineCountByPage,
        IScheduler? scheduler = null)
    {
        ChangesStream = _changes.AsObservable();
        RefreshAllStream = _refreshAll.AsObservable();
        PageChangesStream = _pageChanges.AsObservable();

        Filter = string.Empty;
        MaskText = string.Empty;
        FilePath = filePath;

        var taskpoolScheduler = scheduler ?? RxApp.TaskpoolScheduler;


        var firstFileContentLoading =
            LoadFileContentAsync(filePath, textFileLoader, taskpoolScheduler) // File loading on creation
                .Do(input => { _actualText = input; })
                .Publish();

        var fileContentStream =
            fileChangedStream // changes fromm file system stream
                .SkipUntil(firstFileContentLoading)
                .Select(_ => LoadFileContentAsync(filePath, textFileLoader, taskpoolScheduler))
                .Concat()
                .Select(newText => new Data(newText, _actualText))
                .Publish();

        fileContentStream // save new content stream
            .Do(input => _actualText = input.NewText)
            .Subscribe()
            .DisposeWith(_disposable);

        fileContentStream // Changes stream
            .Select(Diff)
            .Select(Split)
            .Select(text => DoFilter(Filter, text))
            .Select(text => DoMaskText(MaskText, text))
            .Select(Reverse)
            .Select(Join)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.EndsWith(Environment.NewLine) ? s : s + Environment.NewLine)
            .Subscribe(_changes)
            .DisposeWith(_disposable);
        
        var filterOrMaskStream =
            this.WhenAnyValue(vm => vm.Filter, vm => vm.MaskText, (filter, mask) => (Filter: filter, Mask: mask))
                .SkipUntil(firstFileContentLoading)
                .Throttle(_throttleTime, taskpoolScheduler)
                .DistinctUntilChanged()
                .Select(data => (Text:Split(_actualText), data.Mask, data.Filter))
                .Select(data => (Text: DoFilter(data.Filter, data.Text), data.Mask))
                .Select(data => DoMaskText(data.Mask, data.Text));

        firstFileContentLoading // Refresh all stream
            .Select(Split)
            .Merge(filterOrMaskStream)
            .Do(_ => CurrentPage = 0)
            .Select(text => TakeLast(lineCountByPage, text, 0))
            .Select(Reverse)
            .Select(Join)
            .Do(text => _allDataAreDisplayed = string.IsNullOrWhiteSpace(text))
            .Subscribe(_refreshAll)
            .DisposeWith(_disposable);

        this.WhenAnyValue(vm => vm.CurrentPage) // Pages stream
            .SkipUntil(firstFileContentLoading)
            .Where(page => page > 0)
            .Select(page => (Text:Split(_actualText), NewPage:page))
            .Select(data => (Text: DoFilter(Filter, data.Text), data.NewPage))
            .Select(data => TakeLast(lineCountByPage, data.Text, data.NewPage))
            .Select(text => DoMaskText(MaskText, text))
            .Select(Reverse)
            .Select(Join)
            .Do(text => _allDataAreDisplayed = string.IsNullOrWhiteSpace(text))
            .Where(_ => !_allDataAreDisplayed)
            .Subscribe(_pageChanges)
            .DisposeWith(_disposable);

        fileContentStream
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
           return actualText.Split(Environment.NewLine);
        }
    }
    
    private static string Join(IEnumerable<string> actualText)
    {
        using (new Watcher("Join"))
        {
            return actualText.Join(Environment.NewLine);
        }
    }
    
    private static IEnumerable<string> TakeLast(int lineCount, IEnumerable<string> actualText, int actualPageIndex)
    {
        using (new Watcher("TakeLast"))
        {
            return actualText
                .SkipLast(lineCount * actualPageIndex)
                .TakeLast(lineCount);
        }
    }

    private static IEnumerable<string> DoMaskText(string mask, IEnumerable<string> actualText)
    {
        using (new Watcher("Mask"))
        {
            if (string.IsNullOrWhiteSpace(mask))
                return actualText;

            return actualText.Select(line => 
                line.Replace(mask, string.Empty, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    private static IEnumerable<string> DoFilter(string filter, IEnumerable<string> actualText)
    {
        using (new Watcher("Filter"))
        {
            if (string.IsNullOrWhiteSpace(filter))
                return actualText;

            return actualText
                .Where(line => line.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    private static IObservable<string> LoadFileContentAsync(string filePath, ITextFileLoader textFileLoader, IScheduler taskpoolScheduler) =>
        Observable.FromAsync(async () =>
        {
            using (new Watcher("File loading"))
            {
                return await textFileLoader.GetTextAsync(filePath, CancellationToken.None);
            }
        }, taskpoolScheduler);


    private static string Diff(Data input)
    {
        var actualTextLength = input.ActualText.Length;
        var newTextLength = input.NewText.Length;

        return actualTextLength < newTextLength
            ? input.NewText.Substring(actualTextLength, newTextLength - actualTextLength)
            : string.Empty;
    }

    private static IEnumerable<string> Reverse(IEnumerable<string> input)
    {
        using (new Watcher("Reverse"))
        {
            return input.Reverse();
        }
    }

    public void Dispose() => _disposable.Dispose();
}

internal record Data(string NewText, string ActualText);