using System.Collections;

namespace SpeleoLog.Viewer.Core;

/// <summary>
/// Represents the indexes of a page
/// </summary>
public record PageRange : IEnumerable<int>
{
    private readonly int[] _index;
    public static PageRange Empty => new(0, 0);
    public int[] this[Range i] => _index[i];

    public int Start { get; }
    public int End { get; }
    public int Size { get; }
    public bool IsEmpty => Size == 0;

    public PageRange(int start, int size)
    {
        Start = Math.Max(0, start);
        Size = Math.Max(0, size);

        _index = Enumerable
            .Range(Start, Size)
            .ToArray();

        End = _index.LastOrDefault();
    }

    public static PageRange Create(int start, int end)
    {
        start = Math.Max(0, start);
        var pageRange = new PageRange(start, end - start + 1);
        if (pageRange.End != Math.Max(0, end)) throw new ArgumentException($"Calculated end {pageRange.End} is not equal to given end {end}");
        return pageRange;
    }

    public IEnumerator<int> GetEnumerator() => _index.AsEnumerable().GetEnumerator();

    private int IndexOf(int item) => Array.IndexOf(_index, item);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Return differences between actual range and <param name="incomingRange" />
    /// </summary>
    /// <param name="incomingRange" />
    /// <returns />
    /// <exception cref="ArgumentException" />
    public IRangeCompare Compare(PageRange incomingRange)
    {
        if (IsEmpty)
            return new IsGoneBackward([], incomingRange[..], Size);

        if (incomingRange.Start == Start && incomingRange.End == End)
            return new IsUnchanged();

        if (incomingRange.End > End)
            return CompareForward(incomingRange);

        return CompareBackward(incomingRange);
    }

    private IsGoneForward CompareForward(PageRange incomingRange)
    {
        if (Start > incomingRange.Start)
            throw new ArgumentException($"Impossible to move forward when the new start predates the current one ({incomingRange.Start}). Actual start: {Start}");

        if (End < incomingRange.Start)
            return new IsGoneForward(this[..], incomingRange[..], Size);

        var localIndexOfIncomingStart = Array.IndexOf(_index, incomingRange.Start);
        var incomingIndexOfLocalEnd = incomingRange.IndexOf(End);

        return new IsGoneForward(this[..localIndexOfIncomingStart], incomingRange[(incomingIndexOfLocalEnd + 1)..], Size);
    }

    private IsGoneBackward CompareBackward(PageRange incomingRange)
    {
        if (End < incomingRange.End)
            throw new ArgumentException($"Impossible to go back when the new ending is later than the current one ({incomingRange.End}). Actual end: {End}");

        if (incomingRange.End < Start)
            return new IsGoneBackward(this[..], incomingRange[..], Size);

        var localIndexOfIncomingEnd = Array.IndexOf(_index, incomingRange.End);
        var incomingIndexOfLocalStart = incomingRange.IndexOf(Start);

        return new IsGoneBackward(this[(localIndexOfIncomingEnd + 1)..], incomingRange[..incomingIndexOfLocalStart], Size);
    }

    /// <summary>
    /// Create new <see cref="PageRange"/> by moving the current range
    /// </summary>
    /// <param name="delta" />
    /// <returns />
    public PageRange Move(int delta) => new(start: Start + delta, size: Size);

    /// <summary>
    /// Create new <see cref="PageRange"/> by extending the current range from the back
    /// </summary>
    /// <param name="delta" />
    /// <param name="maxPageSize" />
    /// <returns />
    public PageRange ExpandsBackward(int delta, int maxPageSize = 0)
    {
        if (maxPageSize != 0 && Size + delta > maxPageSize)
            delta = maxPageSize - Size;

        return Create(start: Start - delta, End);
    }

    /// <summary>
    /// Create new <see cref="PageRange"/> by extending the current range from the front
    /// </summary>
    /// <param name="delta" />
    /// <param name="maxPageSize" />
    /// <returns />
    public PageRange ExpandsForward(int delta, int maxPageSize = 0)
    {
        if (maxPageSize != 0 && Size + delta > maxPageSize)
            delta = maxPageSize - Size;

        return Create(start: Start, End + delta);
    }
}

public interface IRangeCompare;

public record IsGoneBackward(int[] DeleteFromTop, int[] AddedFomBottom, int PageSize) : IRangeCompare;

public record IsGoneForward(int[] DeleteFromBottom, int[] AddedFomTop, int PageSize) : IRangeCompare;

public class IsUnchanged : IRangeCompare;