using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer.LogFileViewer.V2;

public static class LogAggregator
{
    public static IEnumerable<LogGroup> AggregateLog(this IEnumerable<LogRow> rows) =>
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
    public static GroupKey FromLog(LogRow log) => new(log.IsError, log.IsNewLine);
}

public class LogGroup
{
    private readonly List<LogRow> _logLines = [];
    public string[] Rows => _logLines.Select(row => row.Text).ToArray();

    public GroupKey Key { get; }

    public LogGroup(LogRow logRow)
    {
        Key = GroupKey.FromLog(logRow);
        _logLines.Add(logRow);
    }

    public LogGroup Add(LogRow logRow)
    {
        if (new GroupKey(logRow.IsError, logRow.IsNewLine) != Key) 
            throw new ArgumentException("Log row must have same isError and isNewline than group row");
        
        _logLines.Add(logRow);
        return this;

    }
}