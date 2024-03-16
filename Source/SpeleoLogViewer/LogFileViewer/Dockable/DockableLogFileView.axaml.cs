using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SpeleoLogViewer.LogFileViewer.Dockable;

public partial class DockableLogFileView : UserControl
{
    public DockableLogFileView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}