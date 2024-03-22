using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace SpeleoLogViewer.LogFileViewer;

public partial class LogFileViewerView : ReactiveUserControl<LogFileViewerVM>
{
    private IDisposable? _changesSubscription;
    private IDisposable? _refreshAllSubscription;
    private IDisposable? _pageChangesSubscription;

    public SelectableTextBlock? LogFileContent => this.FindControl<SelectableTextBlock>("LogContent");

    public LogFileViewerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated(disposable =>
        {
            _refreshAllSubscription ??= SubscribeToRefreshAll();
            _changesSubscription ??= SubscribeToChanges();
            _pageChangesSubscription ??= SubscribeToPageChanges();
        });
        AvaloniaXamlLoader.Load(this);
    }

    private IDisposable? SubscribeToChanges() =>
        ViewModel?
            .ChangesStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(s => new Run(s))
            .Do(run => run.Classes.Add("JustAdded"))
            .Do(AddTimerToRemoveClass)
            .Do(PushChangesToSelectableTextBox)
            .Subscribe();

    private IDisposable? SubscribeToRefreshAll() =>
        ViewModel?
            .RefreshAllStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => LogFileContent?.Inlines?.Clear())
            .Select(s => new Run(s))
            .Do(PushChangesToSelectableTextBox)
            .Subscribe();
    
    private IDisposable? SubscribeToPageChanges() =>
        ViewModel?
            .PageChangesStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(s => LogFileContent?.Inlines?.Add(s))
            .Subscribe();

    private void PushChangesToSelectableTextBox(Inline inline)
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
            .Do(_ => inline.Classes.Clear())
            .Subscribe();

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var scrollViewer = (ScrollViewer)sender!;
        //Console.WriteLine($"{IsScrollToBottom(scrollViewer)}");
        if(IsScrollToBottom(scrollViewer) && (LogFileContent?.Inlines?.Any() ?? false))
            ViewModel?.DisplayNextPage();
    }

    private static bool IsScrollToBottom(ScrollViewer scrollViewer) => 
        scrollViewer.Offset.Y.Equals(scrollViewer.ScrollBarMaximum.Y);
}