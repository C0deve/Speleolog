using Avalonia.Controls.Documents;
using Avalonia.Threading;
using ReactiveMarbles.ObservableEvents;
using Splat;

namespace SpeleoLog.Viewer;

public partial class Viewer : ReactiveUserControl<ViewerVM>, IEnableLogger
{
    private const double ScrollDelta = 100;
    private IDisposable? _refreshAllSubscription;
    private ScrollViewer? _scrollViewer;
    private SelectableTextBlock? _logFileContent;
    private bool _autoScroll;

    public Viewer() => InitializeComponent();

    private void InitializeComponent()
    {
        this.WhenActivated(disposable =>
        {
            _logFileContent ??= this.FindControl<SelectableTextBlock>("LogContent") ?? throw new InvalidOperationException("Viewer is not yet loaded.");
            _scrollViewer ??= this.FindControl<ScrollViewer>("ScrollViewer") ?? throw new InvalidOperationException("Viewer is not yet loaded.");

            var observable = _scrollViewer.Events()
                .ScrollChanged
                //.Do(args => Console.WriteLine($"ExtentDelta :{args.ExtentDelta.Y}, OffsetDelta :{args.OffsetDelta.Y}, ViewportDelta :{args.ViewportDelta.Y}"))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(args => (args, autoScroll: _autoScroll) )
                .Do(_ => _autoScroll = false)
                .Publish();

            observable
                .Where(x => x.args.OffsetDelta.Y > 0)
                //.Do(eventArgs => Console.WriteLine($"ScrollToBottom: {eventArgs.OffsetDelta.Y}"))
                .Where(x => !x.autoScroll && IsScrollToBottom((ScrollViewer)x.args.Source!))
                .Select(_ => Unit.Default)
                .Log(this)
                .InvokeCommand(this, view => view.ViewModel!.PreviousPage)
                .DisposeWith(disposable);

            observable
                .Where(x => x.args.OffsetDelta.Y < 0)
                //.Do(eventArgs => Console.WriteLine($"ScrollToTop: {eventArgs.OffsetDelta.Y}"))
                .Where(x => !x.autoScroll && IsScrollToTop((ScrollViewer)x.args.Source!))
                .Select(_ => Unit.Default)
                .Log(this)
                .InvokeCommand(this, view => view.ViewModel!.NextPage)
                .DisposeWith(disposable);

            observable.Connect().DisposeWith(disposable);
            _refreshAllSubscription ??= SubscribeToRefresh();
        });

        AvaloniaXamlLoader.Load(this);
    }

    private IDisposable? SubscribeToRefresh() =>
        ViewModel?
            .RefreshStream
            .Do(message => Dispatcher.UIThread.Post(() => Handle(message), DispatcherPriority.Render))
            .Subscribe(_ => { }, ex => Console.WriteLine(ex.Message));

    private void Handle(IEvent message)
    {
        Log(message);
        switch (message)
        {
            case DeletedAll:
                DeleteAll();
                break;
            case AddedToTheBottom toTheBottom:
                AddToBottom(toTheBottom);
                break;
            case AddedToTheTop toTheTop:
                AddToTop(toTheTop);
                break;
            case Updated updated:
                Update(updated);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }

    private void Update(Updated updated)
    {
        if (_logFileContent is null) return;

        _logFileContent.Inlines?.Clear();
        foreach (var run in updated.Blocs.Select(MapToRun))
            _logFileContent.Inlines?.Add(run);
    }

    private void DeleteAll()
    {
        if (_logFileContent is null) return;

        _logFileContent.Inlines?.Clear();
        _autoScroll = true;
        _scrollViewer?.ScrollToHome();
    }

    private void AddToBottom(AddedToTheBottom toTheBottom)
    {
        if (_logFileContent is null) return;

        foreach (var run in toTheBottom.Blocs.Select(MapToRun))
            _logFileContent.Inlines?.Add(run);
        DeleteLines(_logFileContent, toTheBottom.RemovedFromTopCount, toTheBottom.PreviousPageSize, _scrollViewer);
    }

    private void AddToTop(AddedToTheTop toTheTop)
    {
        if (_logFileContent is null) return;

        foreach (var run in toTheTop.Blocs.Reverse().Select(MapToRun))
            _logFileContent.Inlines?.Insert(0, run);
        DeleteLines(_logFileContent,
            toTheTop.RemovedFromBottomCount,
            toTheTop.PreviousPageSize,
            toTheTop.IsOnTop ? null : _scrollViewer,
            true);
    }

    private void DeleteLines(SelectableTextBlock selectableTextBlock, int lineToDeleteCount, int previousPageSize, ScrollViewer? scrollViewer, bool isFromBottom = false)
    {
        if (lineToDeleteCount == 0) return;
        if (selectableTextBlock.Inlines is null || selectableTextBlock.Inlines.Count == 0) return;

        var runs = selectableTextBlock.Inlines
            .Where(inline => inline is Run)
            .Cast<Run>();

        if (isFromBottom)
            runs = runs.Reverse();

        var toDelete =
            DeleteLines(runs.ToArray(), lineToDeleteCount, isFromBottom ? RemoveFromBottom : RemoveFromTop)
                .ToArray();

        foreach (var run in toDelete)
            selectableTextBlock.Inlines.Remove(run);

        if (!isFromBottom)
            lineToDeleteCount *= -1;

        AdjustScroll(lineToDeleteCount, previousPageSize, scrollViewer);
        return;

        LineResult RemoveFromBottom(string s, int i) => s.RemoveNthLineFromBottom(i);
        LineResult RemoveFromTop(string s, int i) => s.RemoveNthLineFromTop(i);
    }

    private static IEnumerable<Run> DeleteLines(Run[] runs, int lineToDeleteCount, Func<string, int, LineResult> remove)
    {
        foreach (var run in runs)
        {
            if (lineToDeleteCount == 0) break;

            if (run.Text is null)
            {
                yield return run;
                continue;
            }

            var result = remove(run.Text, lineToDeleteCount);
            lineToDeleteCount -= result.LineCount;

            // Debug.WriteLine($"Delete from bottom: found {result.LineCount} rows in last run");
            if (result.Text.Length == 0)
            {
                yield return run;
                continue;
            }

            // Debug.WriteLine($"delete {result.LineCount} rows from bottom, Length {run.Text.Length} => {result.Text.Length}");
            run.Text = result.Text;
        }
    }

    private void AdjustScroll(int count, int previousPageSize, ScrollViewer? scrollViewer)
    {
        if (scrollViewer is null) return;
        _autoScroll = true;
        var offsetDelta = scrollViewer.Extent.Height * (1.0 * count / previousPageSize); // proportion of removed
        // Console.WriteLine($"{scrollViewer.Offset.Y} => {scrollViewer.Offset.Y - offsetDelta}");
        scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, scrollViewer.Offset + new Vector(0, offsetDelta));
    }

    private Run MapToRun(DisplayBloc displayBloc)
    {
        var run = new Run(displayBloc.Text);

        if (displayBloc.IsError)
            run.Classes.Add("Error");

        if (displayBloc.IsJustAdded)
        {
            run.Classes.Add("JustAdded");
            AddTimerToRemoveClass(run);
        }

        if (displayBloc.IsHighlighted)
            run.Classes.Add("HighLight");
        return run;
    }

    private void AddTimerToRemoveClass(Run inline) =>
        Observable
            .Timer(ViewModel?.AnimationDuration ?? TimeSpan.FromSeconds(3))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => inline.Classes.Remove("JustAdded"))
            .Subscribe();

    private static bool IsScrollToBottom(ScrollViewer scrollViewer)
    {
        // Console.WriteLine($"{scrollViewer.Offset.Y + scrollViewer.Viewport.Height + ScrollDelta} >= {scrollViewer.Extent.Height}");
        return scrollViewer.Offset.Y + scrollViewer.Viewport.Height + ScrollDelta >= scrollViewer.Extent.Height;
    }

    private static bool IsScrollToTop(ScrollViewer scrollViewer)
    {
        // Console.WriteLine($"{scrollViewer.Offset.Y} <= {ScrollDelta}");
        return scrollViewer.Offset.Y <= ScrollDelta;
    }


    private static void Log(IEvent message)
    {
        switch (message)
        {
            case AddedToTheBottom addToBottom:
                Console.WriteLine($"addToBottom {addToBottom.Blocs.Count} bloc(s)");
                break;
            case AddedToTheTop addToTop:
                Console.WriteLine($"addToTop {addToTop.Blocs.Count} bloc(s)");
                break;
            case DeletedAll:
                Console.WriteLine("delete all");
                break;
            case Updated updated:
                Console.WriteLine($"update current page {updated.Blocs.Count} bloc(s)");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }
}