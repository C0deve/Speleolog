namespace SpeleoLog;

/// <summary>
/// Text block for display. Contains all the information needed for the style
/// </summary>
/// <param name="Text"></param>
/// <param name="IsJustAdded"></param>
/// <param name="IsError"></param>
/// <param name="IsHighlighted"></param>
public record TextBlock(string Text, bool IsJustAdded = false, bool IsError = false, bool IsHighlighted = false, bool IsRowNumber = false)
{
    public static TextBlock RowNumber(int i) => new(i.ToString().PadRight(8), IsRowNumber: true);
    public static implicit operator TextBlock(string row) => new(row);
}