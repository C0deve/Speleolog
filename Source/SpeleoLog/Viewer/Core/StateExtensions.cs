namespace SpeleoLog.Viewer.Core;

public static class StateExtensions
{
    public static IEnumerable<IEnumerable<TextBlock>> SplitHighlightBloc(this IEnumerable<Row> runsInput, string highlight) =>
        runsInput.Select(row => row.SplitHighlightBloc(highlight).Blocs);

    public static DisplayedRow SplitHighlightBloc(this Row row, string highlight) =>
        row.Text
            .Cut(highlight)
            .Select(range => new TextBlock(
                row.Text[range.Range],
                row.IsNewLine,
                row.IsError,
                range.IsHighLight))
            .ToDisplayedRow(row.Index);
    
    public static TextBlock[] Group(this IEnumerable<TextBlock> runsInput) =>
        runsInput.Aggregate(
            new List<TextBlock>(),
            (state, logRun) => state switch
            {
                [] => [logRun],
                [..] => UpdateState(state, logRun)
            },
            runs => runs.ToArray());

    private static DisplayedRow ToDisplayedRow(this IEnumerable<TextBlock> blocs, int rowIndex) => 
        new(rowIndex, blocs.ToArray());

    private static List<TextBlock> UpdateState(List<TextBlock> state, TextBlock textBlock)
    {
        var last = state.Last();

        if (CanMerge(textBlock, last))
            state[^1] = last with { Text = last.Text + textBlock.Text };
        else
            state.Add(textBlock);

        return state;
    }

    private static bool CanMerge(TextBlock textBlock, TextBlock last) =>
        last.IsJustAdded == textBlock.IsJustAdded
        && last.IsHighlighted == textBlock.IsHighlighted
        && last.IsError == textBlock.IsError
        && last.IsRowNumber == textBlock.IsRowNumber;
}