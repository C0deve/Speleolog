using System;
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

    private void PushChangesToSelectableTextBox(Run inline)
    {
        using (new Watcher($"PushToSelectableTextBox {inline.Text!.Length}"))
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
}