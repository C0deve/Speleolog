using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeleoLogViewer.LogFileViewer;

public class LogAggregator(string error)
{
    public IEnumerable<DefaultLogLinesAggregate> Aggregate(IEnumerable<string> lines) =>
        lines.Aggregate(
                new List<Builder>(),
                (aggregateList, line) =>
                {
                    var last = aggregateList.LastOrDefault();
                    if (ErrorPredicate(line))
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
            
    
    private bool ErrorPredicate(string line) => 
        line.Contains(error, StringComparison.InvariantCultureIgnoreCase);
    
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