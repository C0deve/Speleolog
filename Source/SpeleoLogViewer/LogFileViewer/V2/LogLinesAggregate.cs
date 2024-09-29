namespace SpeleoLogViewer.LogFileViewer.V2;

public abstract record LogLinesAggregateV2(params string[] Text);

public record DefaultLogLinesAggregateV2(params string[] Text) : LogLinesAggregateV2(Text);
public record ErrorLogLinesAggregateV2(params string[] Text) : DefaultLogLinesAggregateV2(Text);