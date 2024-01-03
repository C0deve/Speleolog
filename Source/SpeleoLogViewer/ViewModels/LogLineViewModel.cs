using System;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SpeleoLogViewer.ViewModels;

public partial class LogLineViewModel : ViewModelBase
{
    public string Text { get; }
    public bool IsError => Text == "error";
    [ObservableProperty] private bool _justAppend;

    /// <inheritdoc/>
    public LogLineViewModel(string text, bool justAppend = false)
    {
        Text = text;
        JustAppend = justAppend;
        if(JustAppend)
            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => JustAppend = false);
    }

    public static implicit operator LogLineViewModel(string text) => new(text);
}