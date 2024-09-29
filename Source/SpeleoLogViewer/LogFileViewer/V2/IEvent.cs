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

public class AddedToTheTop(params LogLine[] rows) : EventBase(rows);

public class AddedToTheBottom(params LogLine[] rows) : EventBase(rows);

public record DeletedFromTop(int Count) : IEvent;

public record DeletedFromBottom(int Count) : IEvent;

public record DeletedAll : IEvent;

public record Initial : IEvent;