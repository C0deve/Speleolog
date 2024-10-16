using System.Reactive;
using ReactiveUI;

namespace SpeleoLogViewer.SpeleologTemplate;

public record TemplateInfos
{
    public TemplateInfos(string path)
    {
        Path = path;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Open = ReactiveCommand.Create(() => this);
    }

    public string Name { get; }
    public ReactiveCommand<Unit, TemplateInfos> Open { get; }
    public string Path { get; }

    public void Deconstruct(out string path)
    {
        path = Path;
    }
}