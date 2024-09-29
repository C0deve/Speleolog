using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeleoLogViewer.LogFileViewer;

public static class LogAggregator
{
    public static IEnumerable<LogLinesAggregate> AggregateLog(this IEnumerable<string> rows, string errorTag) =>
        rows.Aggregate(
                new List<Builder>(),
                (aggregateList, row) =>
                {
                    var last = aggregateList.LastOrDefault();
                    if (ErrorPredicate(row, errorTag))
                    {
                        if (last is ErrorBuilder)
                            last.AddLine(row);
                        else
                            aggregateList.Add(new ErrorBuilder(row));

                        return aggregateList;
                    }

                    if (last is null or ErrorBuilder)
                        aggregateList.Add(new DefaultBuilder(row));
                    else
                        last.AddLine(row);

                    return aggregateList;
                })
            .Select(builder => builder switch
            {
                DefaultBuilder defaultBuilder => new DefaultLogLinesAggregate(defaultBuilder.Text),
                ErrorBuilder errorBuilder => new ErrorDefaultLogLinesAggregate(errorBuilder.Text),
                _ => throw new ArgumentOutOfRangeException(nameof(builder))
            });
            
    
    private static bool ErrorPredicate(string row, string errorTag) => 
        row.Contains(errorTag, StringComparison.InvariantCultureIgnoreCase);
    
    private abstract class Builder
    {
        private readonly StringBuilder _builder = new();
        public string Text => _builder.ToString();

        protected Builder(string text) => AddLine(text);

        public void AddLine(string text) => _builder.AppendLine(text);
    }
    
    private class DefaultBuilder(string text) : Builder(text);
    private class ErrorBuilder(string text) : Builder(text);
}