using System.Diagnostics;
using ReactiveUI.Fody.Helpers;

namespace SpeleoLog.Viewer;

public sealed class ViewerVM : Document, IDisposable
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
    public ReactiveCommand<Unit, Unit> GoToTop { get; }
    public ReactiveCommand<Unit, ICommand> PreviousPage { get; } = ReactiveCommand.Create<Unit, ICommand>(_ => new Previous());
    public ReactiveCommand<Unit, ICommand> NextPage { get; } = ReactiveCommand.Create<Unit, ICommand>(_ => new Next());

    public ViewerVM(
        string filePath,
        IObservable<Unit> fileChangedStream,
        IFileLoader fileLoader,
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

        Load = ReactiveCommand.CreateFromTask(() => LoadFileContentAsync(filePath, fileLoader));
        GoToTop = ReactiveCommand.Create(() => { });
        var sequencer = new Sequencer<IEvent[]>(Console.WriteLine);

        Observable.Merge(
                NextPage.Is<ICommand>(),
                PreviousPage.Is<ICommand>(),
                this.WhenAnyValue(vm => vm.Filter, filter => new Filter(filter)).Throttle(_throttleTime, taskpoolScheduler).Skip(1).Is<ICommand>(),
                this.WhenAnyValue(vm => vm.MaskText, mask => new Mask(mask)).Skip(1).Throttle(_throttleTime, taskpoolScheduler).Is<ICommand>(),
                this.WhenAnyValue(vm => vm.ErrorTag, tag => new SetErrorTag(tag)).Skip(1).Throttle(_throttleTime, taskpoolScheduler).Is<ICommand>(),
                this.WhenAnyValue(vm => vm.HighlightText, text => new Highlight(text)).Skip(1).Throttle(_throttleTime, taskpoolScheduler).Is<ICommand>(),
                Load.Select(text => new Refresh(text)).Is<ICommand>(),
                GoToTop.Select(_ => new GoToTop()).Is<ICommand>()
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

    private async Task<string[]> LoadFileContentAsync(string filePath, IFileLoader fileLoader)
    {
        var watch = Stopwatch.StartNew();
        var textAsync = await fileLoader.GetTextAsync(filePath, CancellationToken.None);
        watch.Stop();
        LoadingDuration = watch.ElapsedMilliseconds;
        Debug.WriteLine($"File loading {watch.ElapsedMilliseconds}ms");
        return textAsync;
    }

    public void Dispose()
    {
        Debug.WriteLine($"disposing {Title}");
        _disposable.Dispose();
    }
}