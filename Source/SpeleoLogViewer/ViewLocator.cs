using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Core;

namespace SpeleoLogViewer;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        var name = data?.GetType().FullName?.Replace("ViewModel", "View");
        if (name is null)
            return new TextBlock { Text = "Invalid Data Type" };
        
        var type = Type.GetType(name);
        if (type is null) 
            return new TextBlock { Text = "Not Found: " + name };
        
        var instance = Activator.CreateInstance(type);
        if (instance is not null)
            return (Control)instance;

        return new TextBlock { Text = "Create Instance Failed: " + type.FullName };

    }

    public bool Match(object? data) => data is ObservableObject or IDockable;
}
