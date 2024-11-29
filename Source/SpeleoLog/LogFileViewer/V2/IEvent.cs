namespace SpeleoLog.LogFileViewer.V2;

public interface IEvent
{
    public static IEvent Initial => new Initial();
    public static IEvent DeletedAll => new DeletedAll();
}

public class EventBase(params DisplayBloc[] blocs) : ValueObject, IEvent
{
    public IReadOnlyCollection<DisplayBloc> Blocs { get; } = blocs.ToArray().AsReadOnly();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        if (Blocs.Count == 0)
        {
            yield return 0;
            yield break;
        }

        foreach (var row in Blocs)
            yield return row;
    }
}

public class AddedToTheTop(int removedFromBottomCount = 0, int previousPageSize = 0, bool isOnTop = false, params DisplayBloc[] blocs) : EventBase(blocs)
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

public class AddedToTheBottom(int removedFromTopCount = 0, int previousPageSize = 0, params DisplayBloc[] blocs) : EventBase(blocs)
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

public record Updated : IEvent
{
    public Updated(params DisplayBloc[] blocs) => Blocs = blocs.ToArray().AsReadOnly();

    public IReadOnlyCollection<DisplayBloc> Blocs { get; }
}