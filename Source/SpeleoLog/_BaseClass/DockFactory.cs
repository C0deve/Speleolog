using Dock.Avalonia.Controls;
using Dock.Model.ReactiveUI;

namespace SpeleoLog._BaseClass;

public class DockFactory : Factory
{
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    
    public override IRootDock CreateLayout()
    {
        var documentDock = new DocumentDock
        {
            Id = "Files",
            Title = "Files",
            IsCollapsable = false,
            Proportion = 0.5,
            CanCreateDocument = false
        };
        
        var rootDock = CreateRootDock();

        rootDock.IsCollapsable = false;
        rootDock.VisibleDockables = CreateList<IDockable>(documentDock);
        rootDock.ActiveDockable = documentDock;
        rootDock.DefaultDockable = documentDock;

        _documentDock = documentDock;
        _rootDock = rootDock;

        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        DockableLocator = new Dictionary<string, Func<IDockable?>>
        {
            ["Root"] = () => _rootDock,
            ["Files"] = () => _documentDock
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}