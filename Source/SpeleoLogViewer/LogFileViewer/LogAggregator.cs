using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeleoLogViewer.LogFileViewer;

public class LogAggregator
{
    public static IEnumerable<LogLinesAggregate> Aggregate(IEnumerable<string> lines, string errorTag) =>
        lines.Aggregate(
                new List<Builder>(),
                (aggregateList, line) =>
                {
                    var last = aggregateList.LastOrDefault();
                    if (ErrorPredicate(line, errorTag))
                    {
                        if (last is ErrorBuilder)
                            last.AddLine(line);
                        else
                            aggregateList.Add(new ErrorBuilder(line));

                        return aggregateList;
                    }

                    if (last is null or ErrorBuilder)
                        aggregateList.Add(new DefaultBuilder(line));
                    else
                        last.AddLine(line);

                    return aggregateList;
                })
            .Select(builder => builder switch
            {
                DefaultBuilder defaultBuilder => new DefaultLogLinesAggregate(defaultBuilder.Text),
                ErrorBuilder errorBuilder => new ErrorDefaultLogLinesAggregate(errorBuilder.Text),
                _ => throw new ArgumentOutOfRangeException(nameof(builder))
            });
            
    
    private static bool ErrorPredicate(string line, string errorTag) => 
        line.Contains(errorTag, StringComparison.InvariantCultureIgnoreCase);
    
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