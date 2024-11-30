using Avalonia.Controls.Templates;

namespace SpeleoLog;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        var name = data?.GetType().FullName?
            .Replace("VM", "");
        if (name is null)
            return new Avalonia.Controls.TextBlock { Text = "Invalid Data Type" };
        
        var type = Type.GetType(name);
        if (type is null) 
            return new Avalonia.Controls.TextBlock { Text = "Not Found: " + name };
        
        var instance = Activator.CreateInstance(type);
        if (instance is not null)
            return (Control)instance;

        return new Avalonia.Controls.TextBlock { Text = "Create Instance Failed: " + type.FullName };
    }

    public bool Match(object? data) => data is IDockable;
}