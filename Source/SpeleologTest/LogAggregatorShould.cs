using System.Collections.Immutable;
using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class LogAggregatorShould
{
    [Fact]
    public void AggregateLogByType()
    {
        IEnumerable<string> rows = ["A", "B", "Error1", "Error2", "E", "Error3"];
        rows.AggregateLog("error").ShouldBe(
            new List<LogLinesAggregate>
                {
                    new DefaultLogLinesAggregate("A" + Environment.NewLine + "B" + Environment.NewLine),
                    new ErrorDefaultLogLinesAggregate("Error1" + Environment.NewLine + "Error2" + Environment.NewLine),
                    new DefaultLogLinesAggregate("E" + Environment.NewLine),
                    new ErrorDefaultLogLinesAggregate("Error3" + Environment.NewLine)
                }
                .ToImmutableArray());
    }

    [Fact]
    public void RecognizeErrorLogByTag()
    {
        IEnumerable<string> rows = ["A", "B", "test1", "test2", "E", "test3"];
        rows.AggregateLog("test")
            .ShouldBe(new List<LogLinesAggregate>
                {
                    new DefaultLogLinesAggregate("A" + Environment.NewLine + "B" + Environment.NewLine),
                    new ErrorDefaultLogLinesAggregate("test1" + Environment.NewLine + "test2" + Environment.NewLine),
                    new DefaultLogLinesAggregate("E" + Environment.NewLine),
                    new ErrorDefaultLogLinesAggregate("test3" + Environment.NewLine)
                }
                .ToImmutableArray());
    }
    [Fact]
    public void KeepOrder()
    {
        List<string> rows = ["A", "B", "test1", "test2", "E", "test3"];
        rows.AggregateLog("error").SelectMany(aggregate => aggregate.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            .ShouldBe(rows);
    }
    [Fact]
    public void RecognizeErrorLogByTag1()
    {
        IEnumerable<string> rows = ["A", "error", "error", "A", "A", "error", "A", "error", "error", "A"];
        var expected = new List<LogLinesAggregate>
            {
                new DefaultLogLinesAggregate("A" + Environment.NewLine),
                new ErrorDefaultLogLinesAggregate("error" + Environment.NewLine + "error" + Environment.NewLine),
                new DefaultLogLinesAggregate("A" + Environment.NewLine + "A" + Environment.NewLine),
                new ErrorDefaultLogLinesAggregate("error" + Environment.NewLine),
                new DefaultLogLinesAggregate("A" + Environment.NewLine),
                new ErrorDefaultLogLinesAggregate("error" + Environment.NewLine + "error" + Environment.NewLine),
                new DefaultLogLinesAggregate("A" + Environment.NewLine),
            }
            .ToImmutableArray();
        
        rows.AggregateLog("error")
            .ShouldBe(expected);
    }
}