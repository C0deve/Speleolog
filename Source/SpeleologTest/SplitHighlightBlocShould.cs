using Shouldly;
using SpeleoLogViewer;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest;

public class SplitHighlightBlocShould
{
    [Theory]
    [InlineData(true, false),
     InlineData(false, true),
     InlineData(true, true),
     InlineData(false, false)]
    public void SplitOneBlocNoHighlight(bool isNewLine, bool isError) =>
        new LogRow("bla bla bla", IsNewLine: isNewLine, IsError: isError)
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([new DisplayBloc("bla bla bla", IsHighlighted: false, IsError: isError, IsJustAdded: isNewLine)]);

    [Fact]
    public void SplitOneBloc() =>
        new LogRow("hey")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([new DisplayBloc("hey", IsHighlighted: true)]);

    [Fact]
    public void Split() =>
        new LogRow("hey bla bla hey bla bla hey")
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
        new LogRow("heyblaheyhey")
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
        new LogRow("blaheybla")
            .SplitHighlightBloc("hey")
            .Blocs
            .ShouldBe([
                new DisplayBloc("bla"),
                new DisplayBloc("hey", IsHighlighted: true),
                new DisplayBloc("bla")
            ]);
}