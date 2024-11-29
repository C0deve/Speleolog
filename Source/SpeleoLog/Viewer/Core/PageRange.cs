using System.Collections;

namespace SpeleoLog.Viewer.Core;

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

    public static PageRange Create(int start, int end) => new(start, end - start + 1);
    public IEnumerator<int> GetEnumerator() => _index.AsEnumerable().GetEnumerator();

    private int IndexOf(int item) => Array.IndexOf(_index, item);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IRangeCompare Compare(PageRange incomingRange)
    {
        if (incomingRange.Size < Size)
            throw new ArgumentException($"Cannot compare range with a smaller size ({incomingRange.Size}). Actual size: {Size}");

        if (IsEmpty)
            return new IsGoneBackward([], incomingRange[..], Size);

        if (incomingRange.Start == Start && incomingRange.End == End)
            return new IsUnchanged();

        if (incomingRange.Start >= Start)
            return MoveForward(incomingRange);

        return MoveBackward(incomingRange);
    }

    private IsGoneForward MoveForward(PageRange incomingRange)
    {
        if (incomingRange.Start > End)
            return new IsGoneForward(this[..], incomingRange[..], Size);

        var localIndexOfIncomingStart = Array.IndexOf(_index, incomingRange.Start);
        var incomingIndexOfLocalEnd = incomingRange.IndexOf(End);

        return new IsGoneForward(this[..localIndexOfIncomingStart], incomingRange[(incomingIndexOfLocalEnd + 1)..], Size);
    }

    private IsGoneBackward MoveBackward(PageRange incomingRange)
    {
        if (incomingRange.End < Start)
            return new IsGoneBackward(this[..], incomingRange[..], Size);

        var localIndexOfIncomingEnd = Array.IndexOf(_index, incomingRange.End);
        var incomingIndexOfLocalStart = incomingRange.IndexOf(Start);

        return new IsGoneBackward(this[(localIndexOfIncomingEnd + 1)..], incomingRange[..incomingIndexOfLocalStart], Size);
    }

    public PageRange Move(int delta) => new(start: Start + delta, size: Size);
}

public interface IRangeCompare;

public record IsGoneBackward(int[] DeleteFromTop, int[] AddedFomBottom, int PageSize) : IRangeCompare;

public record IsGoneForward(int[] DeleteFromBottom, int[] AddedFomTop, int PageSize) : IRangeCompare;

public class IsUnchanged : IRangeCompare;