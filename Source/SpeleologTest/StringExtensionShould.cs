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
    
    [Fact]
    public void Cut1() =>
        "111"
            .Cut("11")
            .ShouldBe([
                new HighLightText("11", true),
                new HighLightText("1"),
            ]);
    
    [Fact]
    public void CutNoMatch() =>
        "000"
            .Cut("11")
            .ShouldBe([
                new HighLightText("000"),
            ]);
    
    [Fact]
    public void AllIndexOfEmpty() =>
        "111"
            .AllIndexOf("")
            .ShouldBeEmpty();
    [Fact]
    public void AllIndexOfEmpty2() =>
        ""
            .AllIndexOf("")
            .ShouldBeEmpty();
    
    [Fact]
    public void AllIndex() =>
        "111"
            .AllIndexOf("11")
            .ShouldBe([
                0..2,
            ]);
    
    [Fact]
    public void AllIndex2() =>
        "1111"
            .AllIndexOf("11")
            .ShouldBe([
                0..2,
                2..4
            ]);
    
    [Fact]
    public void AllIndexNoMatch() =>
        "000"
            .AllIndexOf("11")
            .ShouldBeEmpty();
}