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
}
