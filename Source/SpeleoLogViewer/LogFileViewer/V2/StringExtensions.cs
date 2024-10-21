using System;
using System.Collections.Generic;

namespace SpeleoLogViewer.LogFileViewer.V2;

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
    
    public static IEnumerable<int> AllIndexOf(this string text, string str, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        var index = text.IndexOf(str, comparisonType);
        while(index != -1)
        {
            yield return index;
            index = text.IndexOf(str, index + 1, comparisonType);
        }
    }
}