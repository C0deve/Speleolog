namespace SpeleoLogViewer.SpeleologTemplate;

public record TemplateInfos(string Path)
{
    public string Name { get; } = System.IO.Path.GetFileNameWithoutExtension(Path);
}