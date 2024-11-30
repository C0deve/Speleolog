using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test;

public class HighlightShould
{
    [Theory]
    [InlineData(true, false),
     InlineData(false, true),
     InlineData(true, true),
     InlineData(false, false)]
    public void SplitOneBlocIfNoMatch(bool isNewLine, bool isError) =>
        new Row(0, "bla bla bla", IsNewLine: isNewLine, IsError: isError)
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([new TextBlock("bla bla bla", IsHighlighted: false, IsError: isError, IsJustAdded: isNewLine)]);

    [Fact]
    public void SplitOneBloc() =>
        new Row(0, "hey")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([new TextBlock("hey", IsHighlighted: true)]);

    [Fact]
    public void Split() =>
        new Row(0, "hey bla bla hey bla bla hey")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([
                new TextBlock("hey", IsHighlighted: true),
                new TextBlock(" bla bla "),
                new TextBlock("hey", IsHighlighted: true),
                new TextBlock(" bla bla "),
                new TextBlock("hey", IsHighlighted: true)
            ]);

    [Fact]
    public void Split2() =>
        new Row(0, "heyblaheyhey")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([
                new TextBlock("hey", IsHighlighted: true),
                new TextBlock("bla"),
                new TextBlock("hey", IsHighlighted: true),
                new TextBlock("hey", IsHighlighted: true)
            ]);

    [Fact]
    public void Split3() =>
        new Row(0, "blaheybla")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([
                new TextBlock("bla"),
                new TextBlock("hey", IsHighlighted: true),
                new TextBlock("bla")
            ]);
    
    [Fact]
    public void Split4() =>
        new Row(0, "111")
            .SplitHighlightBloc("11")
            .Blocs
            .ShouldBe([
                new TextBlock("11", IsHighlighted: true),
                new TextBlock("1")
            ]);
}