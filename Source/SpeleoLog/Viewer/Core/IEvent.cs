namespace SpeleoLog.Viewer.Core;

public interface IEvent
{
    public static IEvent Initial => new Initial();
    public static IEvent DeletedAll => new DeletedAll();
}

public record EventBase(params IReadOnlyCollection<TextBlock> Blocs) : IEvent;

public record AddedToTheTop(int RemovedFromBottomCount = 0, int PreviousPageSize = 0, bool IsOnTop = false, params IReadOnlyCollection<TextBlock> Blocs) : EventBase(Blocs);

public record AddedToTheBottom(int RemovedFromTopCount = 0, int PreviousPageSize = 0, params IReadOnlyCollection<TextBlock> Blocs) : EventBase(Blocs);

public record DeletedAll : IEvent;

public record Initial : IEvent;

public record Updated(params IReadOnlyCollection<TextBlock> Blocs) : EventBase(Blocs);