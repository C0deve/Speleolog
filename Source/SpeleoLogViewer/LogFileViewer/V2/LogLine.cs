namespace SpeleoLogViewer.LogFileViewer.V2;

public record LogLine(string Text, bool IsNewLine = false, bool IsError = false)
{
    public static LogLine NewLine(string text) => new(text, true); 
    public static LogLine Error(string text) => new(text, IsError: true); 
    public static implicit operator string(LogLine row) => row.Text;
    public static implicit operator LogLine(string row) => new(row);
}