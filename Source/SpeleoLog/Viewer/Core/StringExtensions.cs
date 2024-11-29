namespace SpeleoLog.Viewer.Core;

public static class StringExtensions
{
    /// <summary>
    /// Get the started index of the <paramref name="nthRow" /> row from the end of the <paramref name="givenString"/>.
    /// <returns />
    /// Each row is ended by a <see cref="Environment.NewLine"/>
    /// </summary>
    /// <param name="givenString">the given string</param>
    /// <param name="nthRow">row count to be found</param>
    /// <returns></returns>
    public static RowInfo GetIndexOfNthLineFromEnd(this string givenString, int nthRow)
    {
        var breakRowLength = Environment.NewLine.Length;
        var actualRowCount = 0;

        for (var i = givenString.Length; i >= breakRowLength; i--)
        {
            var start = i - breakRowLength;
            if (givenString[start..i] != Environment.NewLine) continue;
            actualRowCount++;
            // nthRow + 1 because we look for the row before to nth row to get the started index
            if (nthRow + 1 == actualRowCount) return new RowInfo(i, nthRow /* We have found all rows */);

            // optimization : we know we are at the last index of the break row. Move i to the start index
            i -= breakRowLength - 1;
        }

        /* We found fewer rows than requested, return actual count */
        return new RowInfo(0, actualRowCount);
    }

    /// <summary>
    /// Get the ended index of the <paramref name="nthRow" /> row of the <paramref name="givenString"/>.
    /// <returns />
    /// Each row is ended by a <see cref="Environment.NewLine"/>
    /// </summary>
    /// <param name="givenString">the given string</param>
    /// <param name="nthRow">row count to be found</param>
    /// <returns></returns>
    public static RowInfo GetEndIndexOfNthLine(this string givenString, int nthRow)
    {
        var breakRowLength = Environment.NewLine.Length;
        var actualRowCount = 0;
        for (var i = 0; i <= givenString.Length - breakRowLength; i++)
        {
            var end = i + breakRowLength;
            if (givenString[i..end] != Environment.NewLine) continue;
            actualRowCount++;
            if (nthRow == actualRowCount) return new RowInfo(end - 1, nthRow);

            // optimization : we know we are at the start of the break row. Move i to the end index
            i += breakRowLength;
        }

        /* We found fewer rows than requested, return actual count */
        return new RowInfo(givenString.Length - 1, actualRowCount);
    }

    public static LineResult RemoveNthLineFromTop(this string givenString, int nthRow)
    {
        var result = givenString.GetEndIndexOfNthLine(nthRow);
        return new LineResult(givenString[(result.Index + 1)..], result.LineCount);
    }

    public static LineResult RemoveNthLineFromBottom(this string givenString, int nthRow)
    {
        var index = givenString.GetIndexOfNthLineFromEnd(nthRow);
        return new LineResult(givenString[..index.Index], index.LineCount);
    }

    /// <summary>
    /// Returns all ranges of <paramref name="str"/> in <paramref name="text" />
    /// </summary>
    /// <param name="text"></param>
    /// <param name="str"></param>
    /// <param name="comparisonType"></param>
    /// <returns></returns>
    public static IEnumerable<Range> AllIndexOf(this string text, string str, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        if (string.IsNullOrEmpty(str)) yield break;

        var index = text.IndexOf(str, comparisonType);
        while (index != -1)
        {
            var end = index + str.Length;
            yield return index..end;
            index = text.IndexOf(str, end, comparisonType);
        }
    }

    public static HighLightRange[] Cut(this string text, string highLight) => 
        string.IsNullOrEmpty(highLight) 
            ? [new HighLightRange(..text.Length)] 
            : text.CutPipeline(highLight);


    private static HighLightRange[] CutPipeline(this string text, string highLight) =>
        text
            .AllIndexOf(highLight, StringComparison.OrdinalIgnoreCase)
            .Aggregate(
                new List<HighLightRange>(),
                (list, range) =>
                {
                    var unLightStart = list.LastOrDefault()?.Range.End ?? 0;
                    var unLightEnd = range.Start;
                    if (unLightEnd.Value > unLightStart.Value)
                        list.Add(new HighLightRange(Range: unLightStart..unLightEnd));

                    list.Add(new HighLightRange(Range: range, IsHighLight: true));

                    return list;
                },
                list =>
                {
                    if (list.Count == 0) return [new HighLightRange(Range: ..text.Length)];
                    var index = list.Last().Range.End;
                    if (index.Value < text.Length)
                        list.Add(new HighLightRange(Range: index..text.Length));
                    return list.ToArray();
                });
}

public record HighLightRange(Range Range, bool IsHighLight = false);