namespace SpeleoLog.Viewer.Core;

public record Row(string Text, bool IsNewLine = false, bool IsError = false)
{
    public static Row NewLine(string text) => new(text, true); 
    public static Row Error(string text) => new(text, IsError: true); 
    public static implicit operator string(Row row) => row.Text;
    public static implicit operator Row(string row) => new(row);
}