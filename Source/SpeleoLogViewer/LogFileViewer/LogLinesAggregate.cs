namespace SpeleoLogViewer.LogFileViewer;

public abstract record LogLinesAggregate(string Text);

public record DefaultLogLinesAggregate(string Text) : LogLinesAggregate(Text);
public record ErrorDefaultLogLinesAggregate(string Text) : DefaultLogLinesAggregate(Text);
