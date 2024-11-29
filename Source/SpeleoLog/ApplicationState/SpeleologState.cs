namespace SpeleoLog.ApplicationState;

public record SpeleologState(List<string> LastOpenFiles, bool AppendFromBottom = true, string TemplateFolder = "")
{
    public static SpeleologState Default => new([], TemplateFolder: "Templates");
}