using Avalonia.Controls.Documents;
using Avalonia.Threading;
using ReactiveMarbles.ObservableEvents;
using Splat;

namespace SpeleoLog.Viewer;

public partial class Viewer : ReactiveUserControl<ViewerVM>, IEnableLogger
{
    private const double ScrollDelta = 150;
    private IDisposable? _refreshAllSubscription;
    private ScrollViewer? _scrollViewer;
    private ItemsControl? _logsContainer;
    private SelectableTextBlock? _test;
    public Viewer() => InitializeComponent();

    private void InitializeComponent()
    {
        this.WhenActivated(disposable =>
        {
            _logsContainer ??= this.FindControl<ItemsControl>("LogsContainer") ?? throw new InvalidOperationException("LogsContainer is not yet loaded.");
            _scrollViewer ??= this.FindControl<ScrollViewer>("ScrollViewer") ?? throw new InvalidOperationException("Viewer is not yet loaded.");
            _test ??= this.FindControl<SelectableTextBlock>("Test") ?? throw new InvalidOperationException("Viewer is not yet loaded.");

            var observable = _scrollViewer.Events()
                .ScrollChanged
                // .Do(LogFirstInView)
                .Publish();

            _test.Events().SizeChanged
                .CombineLatest(observable)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Select(tuple => (tuple.First.NewSize.Height, ((ScrollViewer)tuple.Second.Source!).Viewport.Height))
                .StartWith((_test.DesiredSize.Height, _scrollViewer.Viewport.Height))
                .Where(tuple => tuple.Item1 > 0 || tuple.Item2 > 0)
                .DistinctUntilChanged()
                .Select(tuple => Convert.ToInt32(tuple.Item2) / Convert.ToInt32(tuple.Item1))
                .InvokeCommand(ViewModel?.SetDisplayedRowsCount)
                .DisposeWith(disposable);

            observable
                .Where(x => x.OffsetDelta.Y > 0)
                .Where(x => IsScrollToBottom((ScrollViewer)x.Source!))
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Select(_ => Unit.Default)
                .Log(this, "Invoke PreviousPage")
                .InvokeCommand(ViewModel?.PreviousPage)
                .DisposeWith(disposable);

            observable
                .Where(x => x.OffsetDelta.Y < 0)
                .Where(x => IsScrollToTop((ScrollViewer)x.Source!))
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Select(_ => Unit.Default)
                .InvokeCommand(ViewModel?.NextPage)
                .DisposeWith(disposable);

            observable.Connect().DisposeWith(disposable);

            _refreshAllSubscription ??= SubscribeToRefresh();
            ViewModel?.Load.Execute().Subscribe().DisposeWith(disposable);
        });

        AvaloniaXamlLoader.Load(this);
    }

    private IDisposable? SubscribeToRefresh() =>
        ViewModel?
            .EventStream
            .Do(message => Dispatcher.UIThread.Post(() =>
            {
                if (_logsContainer == null) return;
                
                HandleAdd(_logsContainer, message);
                if (_scrollViewer != null) 
                    HandleAdjustScroll(_scrollViewer, message);
                HandleDelete(_logsContainer, message);
                
            }, DispatcherPriority.Render))
            .Subscribe(_ => { }, ex => Console.WriteLine(ex.Message));

    private void HandleAdd(ItemsControl logsContainer, IEvent message)
    {
        Log(message);
        switch (message)
        {
            case AllDeleted:
                break;
            case AddedToTheBottom toTheBottom:
                AddToBottom(logsContainer, toTheBottom);
                break;
            case AddedToTheTop toTheTop:
                AddToTop(logsContainer, toTheTop);
                break;
            case AllReplaced updated:
                ReplaceAllRows(logsContainer, updated);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }

    private static void HandleDelete(ItemsControl logsContainer, IEvent message)
    {
        switch (message)
        {
            case AllDeleted:
                DeleteAll(logsContainer);
                break;
            case AddedToTheBottom toTheBottom:
                DeleteRows(logsContainer,
                    toTheBottom.RemovedFromTopCount);
                break;
            case AddedToTheTop toTheTop:
                DeleteRows(logsContainer,
                    toTheTop.RemovedFromBottomCount,
                    false);
                break;
            case AllReplaced:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }

    private void HandleAdjustScroll(ScrollViewer scrollViewer, IEvent message)
    {
        var textHeight = _test?.DesiredSize.Height;
        switch (message)
        {
            case AllDeleted:
                scrollViewer.ScrollToHome();
                break;
            case AddedToTheBottom toTheBottom:
                AdjustScrollAfterRowDeleting(scrollViewer, toTheBottom.RemovedFromTopCount * -1, textHeight);
                break;
            case AddedToTheTop toTheTop:
                if (toTheTop.IsOnTop)
                    scrollViewer.ScrollToHome();
                else
                    AdjustScrollAfterRowDeleting(scrollViewer, toTheTop.RemovedFromBottomCount, textHeight);
                break;
            case AllReplaced:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }

    private static void AdjustScrollAfterRowDeleting(ScrollViewer scrollViewer, int count, double? textHeight)
    {
        if (count == 0) return;
        var offsetDelta = (textHeight ?? 0) * count;
        var offset = scrollViewer.Offset + new Vector(0, offsetDelta);
        scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, offset);
    }

    private void ReplaceAllRows(ItemsControl logsContainer, AllReplaced allReplaced)
    {
        logsContainer.Items.Clear();
        foreach (var textBlock in allReplaced.Rows.Select(Map))
            logsContainer.Items.Add(textBlock);
    }

    private static void DeleteAll(ItemsControl logsContainer) => logsContainer.Items.Clear();

    private void AddToBottom(ItemsControl logsContainer, AddedToTheBottom toTheBottom)
    {
        foreach (var textBlock in toTheBottom.Rows.Select(Map))
            logsContainer.Items.Add(textBlock);
    }

    private void AddToTop(ItemsControl logsContainer, AddedToTheTop toTheTop)
    {
        foreach (var textBlock in toTheTop.Rows.Reverse().Select(Map))
            logsContainer.Items.Insert(0, textBlock);
    }

    private static void DeleteRows(ItemsControl logsContainer, int rowsToDeleteCount, bool isFromTop = true)
    {
        if (rowsToDeleteCount == 0 || logsContainer.Items.Count == 0) return;
        if (logsContainer.Items.Count <= rowsToDeleteCount)
        {
            logsContainer.Items.Clear();
            return;
        }

        for (var i = 0; i < rowsToDeleteCount; i++)
        {
            logsContainer.Items.RemoveAt(isFromTop
                ? 0
                : logsContainer.Items.Count - 1);
        }
    }

    private SelectableTextBlock Map(DisplayedRow row)
    {
        var selectableTextBlock = new SelectableTextBlock { Inlines = new InlineCollection() };
        if (row.Blocs.First().IsJustAdded)
        {
            selectableTextBlock.Classes.Add("JustAdded");
            AddTimerToRemoveClass(selectableTextBlock);
        }

        selectableTextBlock.Inlines.Add(Map(TextBlock.RowNumber(row.Index)));

        foreach (var textBlock in row.Blocs.Select(Map))
            selectableTextBlock.Inlines.Add(textBlock);

        return selectableTextBlock;
    }

    private static Run Map(TextBlock textBlock)
    {
        var run = new Run(textBlock.Text);

        if (textBlock.IsRowNumber)
            run.Classes.Add("RowNumber");

        if (textBlock.IsError)
            run.Classes.Add("Error");

        if (textBlock.IsHighlighted)
            run.Classes.Add("HighLight");
        return run;
    }

    private void AddTimerToRemoveClass(SelectableTextBlock control) =>
        Observable
            .Timer(ViewModel?.AnimationDuration ?? TimeSpan.FromSeconds(3))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => control.Classes.Remove("JustAdded"))
            .Subscribe();

    private static bool IsScrollToBottom(ScrollViewer scrollViewer)
    {
        var isScrollToBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height + ScrollDelta >= scrollViewer.Extent.Height;
        if (isScrollToBottom) Console.WriteLine("IsScrollToBottom = true");
        return isScrollToBottom;
    }

    private static bool IsScrollToTop(ScrollViewer scrollViewer) => 
        scrollViewer.Offset.Y <= ScrollDelta;


    private static void Log(IEvent message)
    {
        switch (message)
        {
            case AddedToTheBottom addToBottom:
                Console.WriteLine($"addToBottom {addToBottom.Rows.Count} row(s)");
                break;
            case AddedToTheTop addToTop:
                Console.WriteLine($"addToTop {addToTop.Rows.Count} row(s)");
                break;
            case AllDeleted:
                Console.WriteLine("delete all");
                break;
            case AllReplaced updated:
                Console.WriteLine($"update current page {updated.Rows.Count} row(s)");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }

    private void LogFirstInView(ScrollChangedEventArgs scrollChangedEventArgs)
    {
        if (_scrollViewer == null) return;
        if (_logsContainer == null) return;

        var offsetY = _scrollViewer.Offset.Y;
        var control = _logsContainer.Items
            .Cast<SelectableTextBlock>()
            .LastOrDefault(block => block.Bounds.Top <= offsetY && block.Bounds.Bottom >= offsetY);

        var controlBounds = control?.Bounds;
        Console.WriteLine($"First in view {((Run?)control?.Inlines?[0])?.Text?.Trim()}, Top: {controlBounds?.Top}, Bottom: {controlBounds?.Bottom}, OffsetDelta :{scrollChangedEventArgs.OffsetDelta.Y}");
    }
}