using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test.V2;

public class LogAggregatorV2Should
{
    [Fact]
    public void AggregateLogByType()
    {
        Row[] rows =
        [
            Row.Error("Error1"),
            Row.Error("Error2"),
            Row.NewLine("A"),
            Row.NewLine("B"),
            Row.Error("Error3"),
            Row.Error("Error4"),
            "E",
            Row.Error("Error5")
        ];

        rows
            .AggregateLog()
            .ToArray()
            .ShouldBe([
                new LogGroup(Row.Error("Error1")).Add(Row.Error("Error2")),
                new LogGroup(Row.NewLine("A")).Add(Row.NewLine("B")),
                new LogGroup(Row.Error("Error3")).Add(Row.Error("Error4")),
                new LogGroup("E"),
                new LogGroup(Row.Error("Error5"))
            ]);
    }

    [Fact]
    public void ThrowIfNotSameIsError() =>
        Should.Throw<ArgumentException>(() => new LogGroup(Row.Error("Error1")).Add("new"));

    [Fact]
    public void ThrowIfNotSameIsNewLine() =>
        Should.Throw<ArgumentException>(() => new LogGroup(Row.NewLine("NewLine")).Add("Normal"));
}