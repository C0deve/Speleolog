﻿using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;

namespace SpeleoLogViewer.LogFileViewer;

public partial class LogFileViewerView : ReactiveUserControl<LogFileViewerVM>
{
    private IDisposable? _refreshAllSubscription;

    private SelectableTextBlock? LogFileContent => this.FindControl<SelectableTextBlock>("LogContent");

    public LogFileViewerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated(disposable =>
        {
            this.FindControl<ScrollViewer>("ScrollViewer").Events()
                .ScrollChanged
                .Where(args => IsScrollToBottom((ScrollViewer)args.Source!) && (LogFileContent?.Inlines?.Any() ?? false))
                .Select(_ => Unit.Default)
                .InvokeCommand(this, view => view.ViewModel!.NextPage)
                .DisposeWith(disposable);
            
            _refreshAllSubscription ??= SubscribeToRefresh();
        });
        AvaloniaXamlLoader.Load(this);
    }
    
    private IDisposable? SubscribeToRefresh() =>
        ViewModel?
            .RefreshStream
            .Merge(ViewModel.NextPage)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(message =>
            {
                switch (message)
                {
                    case DeleteAll:
                        LogFileContent?.Inlines?.Clear();
                        break;
                    case AddToBottom lines:
                        AddLinesToBottom(lines);
                        break;
                    case AddToTop lines:
                        AddLinesToTop(lines);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(message));
                }
            })
            .Subscribe();

    private void AddLinesToBottom(AddToBottom lines)
    {
        foreach (var s in lines.Logs.Select(MapLogToRun)) 
            LogFileContent?.Inlines?.Add(s);
    }
    
    private void AddLinesToTop(AddToTop lines)
    {
        foreach (var run in lines.Logs.Select(MapLogToRun))
        {
            run.Classes.Add("JustAdded");
            AddTimerToRemoveClass(run);
            PushToTop(run);
        }
    }
    
    private static Run MapLogToRun(LogLinesAggregate line)
    {
        var run = new Run(line.Text);
        if (line is ErrorDefaultLogLinesAggregate)
            run.Classes.Add("Error");
        return run;
    }

    private void PushToTop(Inline inline)
    {
        using (new Watcher("PushToSelectableTextBox"))
        {
            LogFileContent?
                .Inlines?
                .Insert(0, inline);
        }
    }

    private void AddTimerToRemoveClass(Run inline) =>
        Observable
            .Timer(ViewModel?.AnimationDuration ?? TimeSpan.FromSeconds(3))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => inline.Classes.Remove("JustAdded"))
            .Subscribe();

    private static bool IsScrollToBottom(ScrollViewer scrollViewer) =>
        scrollViewer.Offset.Y.Equals(scrollViewer.ScrollBarMaximum.Y);
}