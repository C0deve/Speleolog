namespace SpeleoLog.Viewer.Core;

public interface IEvent
{
    public static IEvent Initial => new Initial();
    public static IEvent AllDeleted => new AllDeleted();
}

public record EventBase(params IReadOnlyCollection<DisplayedRow> Rows) : IEvent;

public record AddedToTheTop(int RemovedFromBottomCount = 0, bool IsOnTop = false, params IReadOnlyCollection<DisplayedRow> Rows) : EventBase(Rows);

public record AddedToTheBottom(int RemovedFromTopCount = 0, params IReadOnlyCollection<DisplayedRow> Rows) : EventBase(Rows);

public record AllDeleted : IEvent;

public record Initial : IEvent;

public record AllReplaced(params IReadOnlyCollection<DisplayedRow> Rows) : EventBase(Rows);