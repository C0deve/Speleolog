using System;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SpeleoLogViewer._BaseClass;

namespace SpeleoLogViewer.LogFileViewer;

public partial class LogLineViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty] private string _text;
    
    public bool IsError => Text.Contains("error", StringComparison.InvariantCultureIgnoreCase);
    [ObservableProperty] private bool _justAdded;
    private readonly string _originalText;
    private readonly IDisposable? _disposable;

    /// <inheritdoc/>
    public LogLineViewModel(string text, bool justAppend = false)
    {
        _originalText = Text = text;
        JustAdded = justAppend;
        if(JustAdded) 
            _disposable = Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ => JustAdded = false);
    }

    public static implicit operator LogLineViewModel(string text) => new(text);

    public void Mask(string maskText)
    {
        Text = string.IsNullOrEmpty(maskText)
            ? _originalText
            : _originalText.Replace(maskText, "", StringComparison.InvariantCultureIgnoreCase);
    }

    private void ReleaseUnmanagedResources() => _disposable?.Dispose();

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~LogLineViewModel()
    {
        ReleaseUnmanagedResources();
    }
}