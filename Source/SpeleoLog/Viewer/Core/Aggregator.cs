namespace SpeleoLog.Viewer.Core;

public static class Aggregator
{
    public static IEnumerable<LogGroup> AggregateLog(this IEnumerable<Row> rows) =>
        rows.Aggregate(
            new List<LogGroup>(),
            (builders, row) =>
            {
                var last = builders.LastOrDefault();

                if (GroupKey.FromLog(row) == last?.Key)
                    last.Add(row);
                else
                    builders.Add(new LogGroup(row));

                return builders;
            });
}

public record GroupKey(bool IsError, bool IsNewLine)
{
    public static GroupKey FromLog(Row log) => new(log.IsError, log.IsNewLine);
}

public class LogGroup
{
    private readonly List<Row> _logLines = [];
    public string[] Rows => _logLines.Select(row => row.Text).ToArray();

    public GroupKey Key { get; }

    public LogGroup(Row row)
    {
        Key = GroupKey.FromLog(row);
        _logLines.Add(row);
    }

    public LogGroup Add(Row row)
    {
        if (new GroupKey(row.IsError, row.IsNewLine) != Key) 
            throw new ArgumentException("Log row must have same isError and isNewline than group row");
        
        _logLines.Add(row);
        return this;

    }
}