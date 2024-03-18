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
    private readonly CompositeDisposable _disposable = new();
    private string _actualText = "";
    private readonly Subject<string> _changes = new();
    private readonly BehaviorSubject<string> _refreshAll = new("");
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);

    public TimeSpan AnimationDuration { get; } = TimeSpan.FromSeconds(10);
    public string FilePath { get; }
    public IObservable<string> ChangesStream { get; }
    public IObservable<string> RefreshAllStream { get; }
    [Reactive] public string Filter { get; set; }
    [Reactive] public string MaskText { get; set; }

    public LogFileViewerVM(
        string filePath,
        IObservable<Unit> fileChangedStream,
        ITextFileLoader textFileLoader,
        IScheduler? scheduler = null)
    {
        Filter = string.Empty;
        MaskText = string.Empty;
        FilePath = filePath;
        var taskpoolScheduler = scheduler ?? RxApp.TaskpoolScheduler;

        ChangesStream = _changes.AsObservable();
        RefreshAllStream = _refreshAll.AsObservable();

        var firstFileContentLoading =
            LoadFileContentAsync(filePath, textFileLoader, taskpoolScheduler) // File loading on creation
                .Do(input => _actualText = input)
                .Publish();

        var changesFromFileSystemStream = fileChangedStream // changes fromm file system stream
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
            .Subscribe(_changes)
            .DisposeWith(_disposable);

        var filterStream =
            this.WhenAnyValue(vm => vm.Filter)
                .SkipUntil(firstFileContentLoading)
                .Throttle(_throttleTime, taskpoolScheduler)
                .DistinctUntilChanged()
                .Select(filter => DoFilter(filter, _actualText));
        var textMaskStream =
            this.WhenAnyValue(vm => vm.MaskText)
                .SkipUntil(firstFileContentLoading)
                .Throttle(_throttleTime, taskpoolScheduler)
                .DistinctUntilChanged()
                .Select(mask => DoMaskText(mask, _actualText));

        firstFileContentLoading
            .Merge(filterStream)
            .Merge(textMaskStream)
            .Select(Reverse)
            .Select(s => Take(300, s))
            .Subscribe(_refreshAll)
            .DisposeWith(_disposable);

        changesFromFileSystemStream
            .Connect()
            .DisposeWith(_disposable);

        firstFileContentLoading
            .Connect()
            .DisposeWith(_disposable);
    }

    private static string Take(int lineCount, string actualText)
    {
        using (new Watcher("Take"))
        {
            return actualText
                .Split(Environment.NewLine)
                .Take(lineCount)
                .Join(Environment.NewLine);
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