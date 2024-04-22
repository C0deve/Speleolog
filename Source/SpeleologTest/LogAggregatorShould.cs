using System.Collections.Immutable;
using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class LogAggregatorShould
{
    [Fact]
    public void AggregateLogByType()
    {
        var sut = new LogAggregator();
        IEnumerable<string> lines = ["A", "B", "Error1", "Error2", "E", "Error3"];
        LogAggregator.Aggregate(lines, "error").ShouldBe(
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
        var sut = new LogAggregator();
        IEnumerable<string> lines = ["A", "B", "test1", "test2", "E", "test3"];
        LogAggregator.Aggregate(lines, "test")
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
        var sut = new LogAggregator();
        List<string> lines = ["A", "B", "test1", "test2", "E", "test3"];
        LogAggregator.Aggregate(lines, "error").SelectMany(aggregate => aggregate.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            .ShouldBe(lines);
    }
    [Fact]
    public void RecognizeErrorLogByTag1()
    {
        var sut = new LogAggregator();
        IEnumerable<string> lines = ["A", "error", "error", "A", "A", "error", "A", "error", "error", "A"];
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
        
        LogAggregator.Aggregate(lines, "error")
            .ShouldBe(expected);
    }
}