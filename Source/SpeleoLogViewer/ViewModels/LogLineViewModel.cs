using System;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SpeleoLogViewer.ViewModels;

public partial class LogLineViewModel : ViewModelBase
{
    [ObservableProperty] private string _text;
    
    public bool IsError => Text.Contains("error", StringComparison.InvariantCultureIgnoreCase);
    [ObservableProperty] private bool _justAppend;
    private readonly string _originalText;

    /// <inheritdoc/>
    public LogLineViewModel(string text, bool justAppend = false)
    {
        _originalText = Text = text;
        JustAppend = justAppend;
        if(JustAppend)
            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => JustAppend = false);
    }

    public static implicit operator LogLineViewModel(string text) => new(text);

    public void Mask(string maskText)
    {
        Text = _originalText.Replace(maskText, "", StringComparison.InvariantCultureIgnoreCase);
    }
}