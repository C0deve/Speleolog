using System;
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
    private readonly int _lineCountByPage;
    private readonly CompositeDisposable _disposable = new();
    private string _actualText = "";
    private readonly Subject<string> _changes = new();
    private readonly Subject<string> _pageChanges = new();
    private readonly BehaviorSubject<string> _refreshAll = new("");
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);
    private int _displayStartIndex;
    [Reactive]private short CurrentPage { get; set; } = 0;
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
        _lineCountByPage = lineCountByPage;

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

        var changesFromFileSystemStream =
            fileChangedStream // changes fromm file system stream
                .SkipUntil(firstFileContentLoading)
                .Select(_ => LoadFileContentAsync(filePath, textFileLoader, taskpoolScheduler))
                .Concat()
                .Select(newText => new Data(newText, _actualText))
                .Publish();

        changesFromFileSystemStream // save new content stream
            .Do(input => _actualText = input.NewText)
            .Subscribe()
            .DisposeWith(_disposable);

        changesFromFileSystemStream // Changes stream
            .Select(Diff)
            .Select(text => DoFilter(Filter, text))
            .Select(text => DoMaskText(MaskText, text))
            .Select(Reverse)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.EndsWith(Environment.NewLine) ? s : s + Environment.NewLine)
            .Subscribe(_changes)
            .DisposeWith(_disposable);


        var filterOrMaskStream =
            this.WhenAnyValue(vm => vm.Filter, vm => vm.MaskText, (filter, mask) => (Filter: filter, Mask: mask))
                .SkipUntil(firstFileContentLoading)
                .Throttle(_throttleTime, taskpoolScheduler)
                .DistinctUntilChanged()
                .Select(data => (Text: DoFilter(data.Filter, _actualText), data.Mask))
                .Select(data => DoMaskText(data.Mask, data.Text));

        firstFileContentLoading
            .Merge(filterOrMaskStream)
            .Select(s => Take(lineCountByPage, s, CurrentPage))
            .Select(Reverse)
            .Subscribe(_refreshAll)
            .DisposeWith(_disposable);

        this.WhenAnyValue(vm => vm.CurrentPage)
            .SkipUntil(firstFileContentLoading)
            .Select(data => DoFilter(Filter, _actualText))
            .Select(s => Take(_lineCountByPage, _actualText, CurrentPage))
            .Select(text => DoMaskText(MaskText, text))
            .Select(Reverse)
            .Subscribe(_pageChanges)
            .DisposeWith(_disposable);
        
        changesFromFileSystemStream
            .Connect()
            .DisposeWith(_disposable);

        firstFileContentLoading
            .Connect()
            .DisposeWith(_disposable);
    }

    public void DisplayNextPage()
    {
        if (_displayStartIndex == 0) return;

        CurrentPage++;
        Console.WriteLine($"{nameof(DisplayNextPage)} page:{CurrentPage}");
    }

    private string Take(int lineCount, string actualText, int actualPageIndex)
    {
        using (new Watcher("Take"))
        {
            var join = actualText
                .Split(Environment.NewLine)
                .SkipLast(lineCount * actualPageIndex)
                .TakeLast(lineCount)
                .Join(Environment.NewLine);

            _displayStartIndex = actualText.Length - join.Length;
            if (_displayStartIndex < 0)
                _displayStartIndex = 0;

            return join;
        }
    }

    private static string DoMaskText(string mask, string actualText)
    {
        using (new Watcher("Mask"))
        {
            if (string.IsNullOrWhiteSpace(mask))
                return actualText;

            return actualText
                .Split(Environment.NewLine)
                .Select(line => line.Replace(mask, string.Empty, StringComparison.InvariantCultureIgnoreCase))
                .Join(Environment.NewLine);
        }
    }

    private static string DoFilter(string filter, string actualText)
    {
        using (new Watcher("Filter"))
        {
            if (string.IsNullOrWhiteSpace(filter))
                return actualText;

            return actualText
                .Split(Environment.NewLine)
                .Where(line => line.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                .Join(Environment.NewLine);
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

    private static string Reverse(string input)
    {
        using (new Watcher("Reverse"))
        {
            return input
                .Split(Environment.NewLine)
                .Reverse()
                .Join(Environment.NewLine);
        }
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}

internal record Data(string NewText, string ActualText);