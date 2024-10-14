using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer.LogFileViewer.V2;

public interface IEvent
{
    public static IEvent Initial => new Initial();
    public static IEvent DeletedAll => new DeletedAll();
}

public class EventBase(params LogLine[] rows) : ValueObject, IEvent
{
    public IReadOnlyCollection<LogLine> Rows { get; } = rows.ToArray().AsReadOnly();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        if (Rows.Count == 0)
        {
            yield return 0;
            yield break;
        }

        foreach (var row in Rows)
            yield return row;
    }
}

public class AddedToTheTop(int removedFromBottomCount = 0, int previousPageSize = 0, bool isOnTop = false, params LogLine[] rows) : EventBase(rows)
{
    public int RemovedFromBottomCount { get; } = removedFromBottomCount;
    public int PreviousPageSize { get; } = previousPageSize;
    public bool IsOnTop { get; } = isOnTop;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RemovedFromBottomCount;
        yield return PreviousPageSize;
        yield return IsOnTop;
        foreach (var equalityComponent in base.GetEqualityComponents())
            yield return equalityComponent;
    }
}

public class AddedToTheBottom(int removedFromTopCount = 0, int previousPageSize = 0, params LogLine[] rows) : EventBase(rows)
{
    public int RemovedFromTopCount { get; } = removedFromTopCount;
    public int PreviousPageSize { get; } = previousPageSize;
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RemovedFromTopCount;
        yield return PreviousPageSize;
        foreach (var equalityComponent in base.GetEqualityComponents())
            yield return equalityComponent;
    }
}

public record DeletedAll : IEvent;

public record Initial : IEvent;