using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test;

public class StringExtensionShould
{
    [Fact]
    public void CutFirstWorld() =>
        "Hello World"
            .Cut("Hello")
            .ShouldBe([
                new HighLightRange(..5, true),
                new HighLightRange(5..11)
            ]);

    [Fact]
    public void CutMiddleWorld() =>
        "World Hello World"
            .Cut("Hello")
            .ShouldBe([
                new HighLightRange(..6),
                new HighLightRange(6..11, true),
                new HighLightRange(11..17)
            ]);

    [Fact]
    public void CutLastWorld() =>
        "Hello World"
            .Cut("World")
            .ShouldBe([
                new HighLightRange(..6),
                new HighLightRange(6..11, true),
            ]);
    
    [Fact]
    public void Cut1() =>
        "111"
            .Cut("11")
            .ShouldBe([
                new HighLightRange(..2, true),
                new HighLightRange(2..3),
            ]);
    
    [Fact]
    public void CutNoMatch() =>
        "000"
            .Cut("11")
            .ShouldBe([
                new HighLightRange(..3),
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