using System;
using System.Diagnostics;
using System.IO;
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

namespace SpeleoLogViewer.LogFileViewer.V2;

public sealed class LogFileViewerV2VM : Document, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly BehaviorSubject<IEvent> _refresh = new(IEvent.Initial);
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);
    public TimeSpan AnimationDuration { get; } = TimeSpan.FromSeconds(10);
    public string FilePath { get; }
    public IObservable<IEvent> RefreshStream { get; }
    [Reactive] public string Filter { get; set; }
    [Reactive] public string MaskText { get; set; }
    [Reactive] public string ErrorTag { get; set; }
    [Reactive] public string HighlightText { get; set; }
    [Reactive] public long LoadingDuration { get; private set; }
    [Reactive] public string LogsCountDisplay { get; private set; } = string.Empty;
    public ReactiveCommand<Unit, string[]> Load { get; }
    public ReactiveCommand<Unit, ICommand> PreviousPage { get; } = ReactiveCommand.Create<Unit, ICommand>(_ => new Previous());
    public ReactiveCommand<Unit, ICommand> NextPage { get; } = ReactiveCommand.Create<Unit, ICommand>(_ => new Next());

    public LogFileViewerV2VM(
        string filePath,
        IObservable<Unit> fileChangedStream,
        ITextFileLoaderV2 textFileLoader,
        int lineCountByPage,
        string errorTag,
        IScheduler? scheduler = null)
    {
        ErrorTag = errorTag;
        RefreshStream = _refresh.AsObservable().Where(message => message is not Initial);
        var state = State.Initial(lineCountByPage);
        Filter = MaskText = HighlightText = string.Empty;
        FilePath = filePath;
        Title = Path.GetFileName(FilePath);
        var taskpoolScheduler = scheduler ?? RxApp.TaskpoolScheduler;

        Load = ReactiveCommand.CreateFromTask(() => LoadFileContentAsync(filePath, textFileLoader));

        var sequencer = new Sequencer<IEvent[]>(Console.WriteLine);

        Observable.Merge(
                NextPage.Is<ICommand>(),
                PreviousPage.Is<ICommand>(),
                this.WhenAnyValue(vm => vm.Filter, filter => new Filter(filter)).Throttle(TimeSpan.FromMilliseconds(500), taskpoolScheduler).Skip(1).Is<ICommand>(),
                this.WhenAnyValue(vm => vm.MaskText, mask => new Mask(mask)).Skip(1).Throttle(TimeSpan.FromMilliseconds(500), taskpoolScheduler).Is<ICommand>(),
                this.WhenAnyValue(vm => vm.ErrorTag, tag => new SetErrorTag(tag)).Skip(1).Throttle(TimeSpan.FromMilliseconds(500), taskpoolScheduler).Is<ICommand>(),
                this.WhenAnyValue(vm => vm.HighlightText, text => new Highlight(text)).Skip(1).Throttle(TimeSpan.FromMilliseconds(500), taskpoolScheduler).Is<ICommand>(),
                Load.Select(text => new Refresh(text)).Is<ICommand>()
            )
            .ObserveOn(taskpoolScheduler)
            .Do(command => sequencer.Enqueue(() =>
            {
                state.Handle(command);
                var newEvents = state.Events;
                state.ClearEvents();
                LogsCountDisplay = state.IsSearchOn
                    ? $"{state.FilteredLogsCount} / {state.TotalLogsCount}"
                    : state.TotalLogsCount.ToString();
                return newEvents;
            }))
            .Subscribe()
            .DisposeWith(_disposable);

        sequencer
            .Output
            .SelectMany(x => x)
            .Log(this)
            .Subscribe(_refresh)
            .DisposeWith(_disposable);

        fileChangedStream
            .InvokeCommand(Load)
            .DisposeWith(_disposable);


        Load.Execute().Subscribe();
    }

    private async Task<string[]> LoadFileContentAsync(string filePath, ITextFileLoaderV2 textFileLoader)
    {
        //ActivitySource.StartActivity("LoadFileContentAsync");
        using var watcher = new Watcher("File loading");
        var watch = Stopwatch.StartNew();
        var textAsync = await textFileLoader.GetTextAsync(filePath, CancellationToken.None);
        watch.Stop();
        LoadingDuration = watch.ElapsedMilliseconds;
        return textAsync;
    }

    public void Dispose()
    {
        Debug.WriteLine($"disposing {Title}");
        _disposable.Dispose();
    }
}