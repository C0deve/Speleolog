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
        new Row("bla bla bla", IsNewLine: isNewLine, IsError: isError)
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([new DisplayBloc("bla bla bla", IsHighlighted: false, IsError: isError, IsJustAdded: isNewLine)]);

    [Fact]
    public void SplitOneBloc() =>
        new Row("hey")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([new DisplayBloc("hey", IsHighlighted: true)]);

    [Fact]
    public void Split() =>
        new Row("hey bla bla hey bla bla hey")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([
                new DisplayBloc("hey", IsHighlighted: true),
                new DisplayBloc(" bla bla "),
                new DisplayBloc("hey", IsHighlighted: true),
                new DisplayBloc(" bla bla "),
                new DisplayBloc("hey", IsHighlighted: true)
            ]);

    [Fact]
    public void Split2() =>
        new Row("heyblaheyhey")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([
                new DisplayBloc("hey", IsHighlighted: true),
                new DisplayBloc("bla"),
                new DisplayBloc("hey", IsHighlighted: true),
                new DisplayBloc("hey", IsHighlighted: true)
            ]);

    [Fact]
    public void Split3() =>
        new Row("blaheybla")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([
                new DisplayBloc("bla"),
                new DisplayBloc("hey", IsHighlighted: true),
                new DisplayBloc("bla")
            ]);
    
    [Fact]
    public void Split4() =>
        new Row("111")
            .SplitHighlightBloc("11")
            .Blocs
            .ShouldBe([
                new DisplayBloc("11", IsHighlighted: true),
                new DisplayBloc("1")
            ]);
}