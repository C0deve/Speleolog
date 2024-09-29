using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeleoLogViewer.LogFileViewer.V2;

public static class LogAggregator
{
    public static IEnumerable<LogGroup> AggregateLog(this IEnumerable<LogLine> rows) =>
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
    public static GroupKey FromLog(LogLine log) => new(log.IsError, log.IsNewLine);
}

public class LogGroup
{
    private readonly List<LogLine> _logLines = [];
    public string[] Rows => _logLines.Select(row => row.Text).ToArray();

    public GroupKey Key { get; }

    public LogGroup(LogLine logLine)
    {
        Key = GroupKey.FromLog(logLine);
        _logLines.Add(logLine);
    }

    public LogGroup Add(LogLine logLine)
    {
        if (new GroupKey(logLine.IsError, logLine.IsNewLine) != Key) 
            throw new ArgumentException("Log row must have same isError and isNewline than group row");
        
        _logLines.Add(logLine);
        return this;

    }
}