namespace SpeleoLog.Viewer.Core;

public static class StringExtensions
{
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