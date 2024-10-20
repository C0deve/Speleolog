using System.Reactive;
using ReactiveUI;

namespace SpeleoLogViewer.SpeleologTemplate;

public record TemplateInfos
{
    public TemplateInfos(string path, string name)
    {
        Path = path;
        Name = name;
        Open = ReactiveCommand.Create(() => this);
    }

    public string Name { get; }
    public ReactiveCommand<Unit, TemplateInfos> Open { get; }
    public string Path { get; }
}