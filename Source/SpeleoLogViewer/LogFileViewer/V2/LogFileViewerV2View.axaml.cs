using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Splat;

namespace SpeleoLogViewer.LogFileViewer.V2;

public partial class LogFileViewerV2View : ReactiveUserControl<LogFileViewerV2VM>, IEnableLogger
{
    private const double ScrollDelta = 100;
    private IDisposable? _refreshAllSubscription;
    private ScrollViewer? _scrollViewer; // => this.FindControl<ScrollViewer>("ScrollViewer");

    private SelectableTextBlock? LogFileContent => this.FindControl<SelectableTextBlock>("LogContent");

    public LogFileViewerV2View()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated(disposable =>
        {
            _scrollViewer ??= this.FindControl<ScrollViewer>("ScrollViewer");
            
            var observable = _scrollViewer.Events()
                .ScrollChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                //.Where(_ => LogFileContent?.Inlines?.Count > 0)
                .Publish();

            observable
                .Where(x => x.OffsetDelta.Y > 0)
                //.Do(eventArgs => Console.WriteLine($"ScrollToBottom: {eventArgs.OffsetDelta.Y}"))
                .Where(args => IsScrollToBottom((ScrollViewer)args.Source!))
                .Select(_ => Unit.Default)
                .Log(this)
                .InvokeCommand(this, view => view.ViewModel!.PreviousPage)
                .DisposeWith(disposable);

            observable
                .Where(args => args.OffsetDelta.Y < 0)
                //.Do(eventArgs => Console.WriteLine($"ScrollToTop: {eventArgs.OffsetDelta.Y}"))
                .Where(args => IsScrollToTop((ScrollViewer)args.Source!))
                .Select(_ => Unit.Default)
                .Log(this)
                .InvokeCommand(this, view => view.ViewModel!.NextPage)
                .DisposeWith(disposable);

            observable
                .Where(x => ((ScrollViewer)x.Source!).Offset.Y != 0 && x.ExtentDelta.Y != 0 || x.OffsetDelta.Y != 0 )
                .Skip(1)
                .Select(args => (extendY: args.ExtentDelta.Y, offsetY: args.OffsetDelta.Y))
                .Scan(
                    (extendY: 0.0, offsetY: 0.0),
                    (acc, value) => value.extendY == 0
                        ? value
                        : (value.extendY, acc.offsetY))
                .Where(x => x.extendY != 0)
                //.Do(args => Console.WriteLine($"ExtentDelta :{args.extendY}, OffsetDelta :{args.offsetY}"))
                .Select(x =>
                {
                    var delta = x.extendY + x.offsetY;
                    return x.offsetY > 0 ? delta : -1 * delta;
                })
                //.Do(args => Console.WriteLine($"Move offset :{args}"))
                .Do(x => _scrollViewer?.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(_scrollViewer.Offset.X, _scrollViewer.Offset.Y + x)))
                .Subscribe()
                .DisposeWith(disposable);

            observable.Connect().DisposeWith(disposable);
            _refreshAllSubscription ??= SubscribeToRefresh();
        });
        AvaloniaXamlLoader.Load(this);
    }

    private IDisposable? SubscribeToRefresh() =>
        ViewModel?
            .RefreshStream
            .Do(message => Dispatcher.UIThread.Post(() => Handle(message)))
            .Subscribe(_ => { }, ex => Console.WriteLine(ex.Message));

    private void Handle(IEvent message)
    {
        Log(message);
        switch (message)
        {
            case DeletedAll:
                LogFileContent?.Inlines?.Clear();
                break;
            case AddedToTheBottom toTheBottom:
                foreach (var run in Map(toTheBottom.Rows))
                    LogFileContent?.Inlines?.Add(run);
                break;
            case DeletedFromBottom deletedFromBottom:
                DoDeletedFromBottom(deletedFromBottom.Count);
                break;
            case DeletedFromTop deletedFromTop:
                DoDeletedFromTop(deletedFromTop.Count);
                break;
            case AddedToTheTop toTheTop:
                foreach (var run in Map(toTheTop.Rows))
                    PushToTop(run);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }

    private void DoDeletedFromBottom(int count)
    {
        while (count > 0)
        {
            if (LogFileContent?.Inlines is null || LogFileContent.Inlines.Count == 0) break;
            if (LogFileContent.Inlines.Last() is not Run last) continue;
            if (last.Text is null)
            {
                LogFileContent.Inlines.Remove(last);
                continue;
            }

            var result = last.Text.RemoveNthLineFromBottom(count);
            count -= result.LineCount;

            Debug.WriteLine($"Delete from bottom: found {result.LineCount} rows in last run");
            if (result.Text.Length == 0)
            {
                LogFileContent.Inlines.Remove(last);
                Debug.WriteLine($"Remove last run, inlines count {LogFileContent.Inlines.Count}");
                continue;
            }

            Debug.WriteLine($"delete {result.LineCount} rows from bottom, Length {last.Text.Length} => {result.Text.Length}");
            last.Text = result.Text;
            break;
        }
    }

    private void DoDeletedFromTop(int count)
    {
        while (count > 0)
        {
            if (LogFileContent?.Inlines is null || LogFileContent.Inlines.Count == 0) break;
            if (LogFileContent.Inlines.FirstOrDefault() is not Run first) continue;
            if (first.Text is null)
            {
                LogFileContent.Inlines.Remove(first);
                continue;
            }

            var result = first.Text.RemoveNthLineFromTop(count);
            count -= result.LineCount;
            Debug.WriteLine($"Remove from top found {result.LineCount} rows in first run");

            if (result.Text.Length == 0)
            {
                LogFileContent.Inlines.Remove(first);
                Debug.WriteLine($"Remove first run, inlines count {LogFileContent.Inlines.Count}");
                continue;
            }

            Debug.WriteLine($"delete {result.LineCount} rows from top, Length {first.Text.Length} => {result.Text.Length}");
            first.Text = result.Text;
            break;
        }
    }

    private IEnumerable<Run> Map(IEnumerable<LogLine> groups) =>
        groups.AggregateLog().Select(MapToRun);

    private Run MapToRun(LogGroup group)
    {
        var rows = group
            .Rows
            .Reverse()
            .Select(s => s + Environment.NewLine);

        var run = new Run(string.Concat(rows));

        if (group.Key.IsError)
            run.Classes.Add("Error");

        if (group.Key.IsNewLine)
        {
            run.Classes.Add("JustAdded");
            AddTimerToRemoveClass(run);
        }

        return run;
    }

    private void PushToTop(Run inline)
    {
        LogFileContent?
            .Inlines?
            .Insert(0, inline);
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
                Console.WriteLine($"addToBottom {addToBottom.Rows.Count}");
                break;
            case DeletedFromBottom deletedFromBottom:
                Console.WriteLine($"deletedFromBottom {deletedFromBottom.Count}");
                break;
            case DeletedFromTop deletedFromTop:
                Console.WriteLine($"deletedFromTop {deletedFromTop.Count}");
                break;
            case AddedToTheTop addToTop:
                Console.WriteLine($"addToTop {addToTop.Rows.Count}");
                break;
            case DeletedAll:
                Console.WriteLine("delete all");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }
    }
}