using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test;

public class GroupShould
{
    [Fact]
    public void Group() =>
        new List<string> { "Hello world", "Bye world bye", "World Bye Bye" }
            .Select(s => s + Environment.NewLine)
            .Select(s => new List<TextBlock> { new(s) })
            .SelectMany(runs => runs)
            .Group()
            .ShouldBe([
                new TextBlock("Hello world" + Environment.NewLine + "Bye world bye" + Environment.NewLine + "World Bye Bye" + Environment.NewLine),
            ]);

    [Fact]
    public void GroupHighlighted() =>
        new List<string> { "Hello world", "Bye world bye", "World Bye Bye" }
            .Select(s => s + Environment.NewLine)
            .Select(s => new Row(0, s))
            .SplitHighlightBloc("world")
            .SelectMany(runs => runs)
            .Group()
            .ShouldBe([
                new TextBlock("Hello "),
                new TextBlock("world", IsHighlighted: true),
                new TextBlock(Environment.NewLine + "Bye "),
                new TextBlock("world", IsHighlighted: true),
                new TextBlock(" bye" + Environment.NewLine),
                new TextBlock("World", IsHighlighted: true),
                new TextBlock(" Bye Bye" + Environment.NewLine),
            ]);

    [Fact]
    public void GroupDisplayBlocs() =>
        new List<TextBlock> { new("Hello world"), new(", "), new("bye world bye") }
            .Group()
            .ShouldBe([new TextBlock("Hello world, bye world bye")]);

    [Fact]
    public void GroupDisplayBlocs2() =>
        new List<TextBlock> { new("Hello world"), new(", ", IsError: true), new("bye world bye") }
            .Group()
            .ShouldBe([
                new TextBlock("Hello world"),
                new TextBlock(", ", IsError: true),
                new TextBlock("bye world bye"),
            ]);
}