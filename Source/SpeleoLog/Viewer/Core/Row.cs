namespace SpeleoLog.Viewer.Core;

public record Row(int Index, string Text, bool IsNewLine = false, bool IsError = false)
{
    public static Row NewLine(string text) => new(0, text, IsNewLine: true); 
    public static Row Error(string text) => new(0, text, IsError: true); 
    public static implicit operator string(Row row) => row.Text;
    public static implicit operator Row(string row) => new(0, row);
}