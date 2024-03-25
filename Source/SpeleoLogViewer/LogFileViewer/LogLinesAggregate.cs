namespace SpeleoLogViewer.LogFileViewer;

public record LogLinesAggregate(string Text);
public record ErrorLogLinesAggregate(string Text) : LogLinesAggregate(Text);
