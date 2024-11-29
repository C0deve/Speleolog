using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test;

public class GroupShould
{
    [Fact]
    public void Group() =>
        new List<string> { "Hello world", "Bye world bye", "World Bye Bye" }
            .Select(s => s + Environment.NewLine)
            .Select(s => new List<DisplayBloc> { new(s) })
            .SelectMany(runs => runs)
            .Group()
            .ShouldBe([
                new DisplayBloc("Hello world" + Environment.NewLine + "Bye world bye" + Environment.NewLine + "World Bye Bye" + Environment.NewLine),
            ]);

    [Fact]
    public void GroupHighlighted() =>
        new List<string> { "Hello world", "Bye world bye", "World Bye Bye" }
            .Select(s => s + Environment.NewLine)
            .Select(s => new Row(s))
            .SplitHighlightBloc("world")
            .SelectMany(runs => runs)
            .Group()
            .ShouldBe([
                new DisplayBloc("Hello "),
                new DisplayBloc("world", IsHighlighted: true),
                new DisplayBloc(Environment.NewLine + "Bye "),
                new DisplayBloc("world", IsHighlighted: true),
                new DisplayBloc(" bye" + Environment.NewLine),
                new DisplayBloc("World", IsHighlighted: true),
                new DisplayBloc(" Bye Bye" + Environment.NewLine),
            ]);

    [Fact]
    public void GroupDisplayBlocs() =>
        new List<DisplayBloc> { new("Hello world"), new(", "), new("bye world bye") }
            .Group()
            .ShouldBe([new DisplayBloc("Hello world, bye world bye")]);

    [Fact]
    public void GroupDisplayBlocs2() =>
        new List<DisplayBloc> { new("Hello world"), new(", ", IsError: true), new("bye world bye") }
            .Group()
            .ShouldBe([
                new DisplayBloc("Hello world"),
                new DisplayBloc(", ", IsError: true),
                new DisplayBloc("bye world bye"),
            ]);
}