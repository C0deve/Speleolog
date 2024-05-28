using System.Collections.Immutable;

namespace SpeleoLogViewer.LogFileViewer;

public interface IMessage
{
    public static IMessage Initial => new Initial();
    public static IMessage DeleteAll => new DeleteAll();
}

public record Initial : IMessage;

public record AddToBottom(ImmutableArray<LogLinesAggregate> Logs) : IMessage;

public record AddToTop(ImmutableArray<LogLinesAggregate> Logs) : IMessage;

public record DeleteAll : IMessage;