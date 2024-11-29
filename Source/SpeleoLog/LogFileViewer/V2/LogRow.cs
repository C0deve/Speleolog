namespace SpeleoLog.LogFileViewer.V2;

public record LogRow(string Text, bool IsNewLine = false, bool IsError = false)
{
    public static LogRow NewLine(string text) => new(text, true); 
    public static LogRow Error(string text) => new(text, IsError: true); 
    public static implicit operator string(LogRow row) => row.Text;
    public static implicit operator LogRow(string row) => new(row);
}