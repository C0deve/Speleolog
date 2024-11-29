namespace SpeleoLog.Viewer.Core;

internal static class CacheExtensions
{
    public static IEnumerable<Row> MaskRows(this IEnumerable<Row> actualRows, string mask) =>
        string.IsNullOrWhiteSpace(mask)
            ? actualRows
            : actualRows.Select(x => Mask(x, mask));

    private static Row Mask(Row row, string mask)
    {
        var index = row.Text.IndexOf(mask, StringComparison.Ordinal);
        var maskedRow = index < 0
            ? row
            : row with { Text = row.Text.Remove(index, mask.Length) };
        return maskedRow;
    }
    
    public static IEnumerable<Row> SetIsError(this IEnumerable<Row> actualRows, string errorTag) =>
        string.IsNullOrWhiteSpace(errorTag)
            ? actualRows
            : actualRows.Select(x => SetIsError(x, errorTag));

    private static Row SetIsError(Row row, string errorTag) =>
        row.Text.Contains(errorTag, StringComparison.InvariantCultureIgnoreCase)
            ? row with { IsError = true }
            : row;
}