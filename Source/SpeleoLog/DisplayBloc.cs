namespace SpeleoLog;

public record DisplayBloc(string Text, bool IsJustAdded = false, bool IsError = false, bool IsHighlighted = false)
{
    public static implicit operator DisplayBloc(string row) => new(row);
}