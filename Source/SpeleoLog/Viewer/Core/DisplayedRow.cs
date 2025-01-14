namespace SpeleoLog.Viewer.Core;

public record DisplayedRow(int Index, params TextBlock[] Blocs)
{
    public DisplayedRow(int index, TextBlock bloc) : this(index, [bloc]) { }
    
    public static implicit operator DisplayedRow(string row) => new(0, [row]);
}