using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest;

public class StringExtensionShould
{
    [Fact]
    public void CutFirstWorld() =>
        "Hello World"
            .Cut("Hello")
            .ShouldBe([
                new HighLightText("Hello", true),
                new HighLightText(" World")
            ]);

    [Fact]
    public void CutMiddleWorld() =>
        "World Hello World"
            .Cut("Hello")
            .ShouldBe([
                new HighLightText("World "),
                new HighLightText("Hello", true),
                new HighLightText(" World")
            ]);

    [Fact]
    public void CutLastWorld() =>
        "Hello World"
            .Cut("World")
            .ShouldBe([
                new HighLightText("Hello "),
                new HighLightText("World", true),
            ]);
}