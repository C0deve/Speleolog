using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest.V2;

public class LogAggregatorV2Should
{
    [Fact]
    public void AggregateLogByType()
    {
        LogRow[] rows =
        [
            LogRow.Error("Error1"),
            LogRow.Error("Error2"),
            LogRow.NewLine("A"),
            LogRow.NewLine("B"),
            LogRow.Error("Error3"),
            LogRow.Error("Error4"),
            "E",
            LogRow.Error("Error5")
        ];

        rows
            .AggregateLog()
            .ToArray()
            .ShouldBe([
                new LogGroup(LogRow.Error("Error1")).Add(LogRow.Error("Error2")),
                new LogGroup(LogRow.NewLine("A")).Add(LogRow.NewLine("B")),
                new LogGroup(LogRow.Error("Error3")).Add(LogRow.Error("Error4")),
                new LogGroup("E"),
                new LogGroup(LogRow.Error("Error5"))
            ]);
    }

    [Fact]
    public void ThrowIfNotSameIsError() =>
        Should.Throw<ArgumentException>(() => new LogGroup(LogRow.Error("Error1")).Add("new"));

    [Fact]
    public void ThrowIfNotSameIsNewLine() =>
        Should.Throw<ArgumentException>(() => new LogGroup(LogRow.NewLine("NewLine")).Add("Normal"));
}