using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest.V2;

public class LogAggregatorV2Should
{
    [Fact]
    public void AggregateLogByType()
    {
        LogLine[] rows =
        [
            LogLine.Error("Error1"),
            LogLine.Error("Error2"),
            LogLine.NewLine("A"),
            LogLine.NewLine("B"),
            LogLine.Error("Error3"),
            LogLine.Error("Error4"),
            "E",
            LogLine.Error("Error5")
        ];

        rows
            .AggregateLog()
            .ToArray()
            .ShouldBe([
                new LogGroup(LogLine.Error("Error1")).Add(LogLine.Error("Error2")),
                new LogGroup(LogLine.NewLine("A")).Add(LogLine.NewLine("B")),
                new LogGroup(LogLine.Error("Error3")).Add(LogLine.Error("Error4")),
                new LogGroup("E"),
                new LogGroup(LogLine.Error("Error5"))
            ]);
    }

    [Fact]
    public void ThrowIfNotSameIsError() =>
        Should.Throw<ArgumentException>(() => new LogGroup(LogLine.Error("Error1")).Add("new"));

    [Fact]
    public void ThrowIfNotSameIsNewLine() =>
        Should.Throw<ArgumentException>(() => new LogGroup(LogLine.NewLine("NewLine")).Add("Normal"));
}