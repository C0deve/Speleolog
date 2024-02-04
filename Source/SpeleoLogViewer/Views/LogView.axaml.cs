using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SpeleoLogViewer.ViewModels;

namespace SpeleoLogViewer.Views;

public partial class LogView : UserControl
{
    private LogViewModel? ViewModel => DataContext as LogViewModel;
    public LogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void ItemsRepeater_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if ((ViewModel?.AppendFromBottom ?? true) && e.HeightChanged) 
            this.FindControl<ScrollViewer>("Scroll")?.ScrollToEnd();
    }
}
