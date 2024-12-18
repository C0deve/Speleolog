﻿namespace SpeleoLog;

public static class Extensions
{
    private static IEnumerable<T> GetAll<T>(this IDictionary<int, T> source, IEnumerable<int> indexes) =>
        indexes.Select(i => source[i]);

    public static IEnumerable<T> GetAll<T>(this IDictionary<int, T> source, params int[] indexes) =>
        GetAll(source, indexes.AsEnumerable());

    public static IEnumerable<IEnumerable<DisplayBloc>> SplitHighlightBloc(this IEnumerable<Row> runsInput, string highlight) =>
        runsInput.Select(row => row.SplitHighlightBloc(highlight).Blocs);

    public static DisplayRow SplitHighlightBloc(this Row row, string highlight) =>
        row.Text
            .Cut(highlight)
            .Select(bloc => new DisplayBloc(
                row.Text[bloc.Range],
                row.IsNewLine,
                row.IsError,
                bloc.IsHighLight))
            .ToDisplayRow();

    private static DisplayRow ToDisplayRow(this IEnumerable<DisplayBloc> blocs) => new(blocs.ToArray());

    public static DisplayBloc[] Group(this IEnumerable<DisplayBloc> runsInput) =>
        runsInput.Aggregate(
            new List<DisplayBloc>(),
            (state, logRun) => state switch
            {
                [] => [logRun],
                [..] => UpdateState(state, logRun)
            },
            runs => runs.ToArray());

    private static List<DisplayBloc> UpdateState(List<DisplayBloc> state, DisplayBloc displayBloc)
    {
        var last = state.Last();

        if (CanMerge(displayBloc, last))
            state[^1] = last with { Text = last.Text + displayBloc.Text };
        else
            state.Add(displayBloc);

        return state;
    }

    private static bool CanMerge(DisplayBloc displayBloc, DisplayBloc last) =>
        last.IsJustAdded == displayBloc.IsJustAdded
        && last.IsHighlighted == displayBloc.IsHighlighted
        && last.IsError == displayBloc.IsError;
}