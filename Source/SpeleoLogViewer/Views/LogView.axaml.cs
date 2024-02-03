using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SpeleoLogViewer.Views;

public partial class LogView : UserControl
{
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
        if (e.HeightChanged) this.FindControl<ScrollViewer>("Scroll")?.ScrollToEnd();
    }
}
