using System.Collections.Frozen;
using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class LogAggregatorShould
{
    [Fact]
    public void AggregateLogByType()
    {
        var sut = new LogAggregator("error");
        IEnumerable<string> lines = ["A", "B", "Error1", "Error2", "E", "Error3"];
        sut.Aggregate(lines).ShouldBeEquivalentTo(
            new List<LogLinesAggregate>
                {
                    new("A" + Environment.NewLine + "B" + Environment.NewLine),
                    new ErrorLogLinesAggregate("Error1" + Environment.NewLine + "Error2" + Environment.NewLine),
                    new("E" + Environment.NewLine),
                    new ErrorLogLinesAggregate("Error3" + Environment.NewLine)
                }
                .ToFrozenSet());
    }

    [Fact]
    public void RecognizeErrorLogByTag()
    {
        var sut = new LogAggregator("test");
        IEnumerable<string> lines = ["A", "B", "test1", "test2", "E", "test3"];
        sut.Aggregate(lines)
            .ShouldBeEquivalentTo(new List<LogLinesAggregate>
                {
                    new("A" + Environment.NewLine + "B" + Environment.NewLine),
                    new ErrorLogLinesAggregate("test1" + Environment.NewLine + "test2" + Environment.NewLine),
                    new("E" + Environment.NewLine),
                    new ErrorLogLinesAggregate("test3" + Environment.NewLine)
                }
                .ToFrozenSet());
    }
}