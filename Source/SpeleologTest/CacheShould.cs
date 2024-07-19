using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class CacheShould
{
    [Fact]
    public void ReturnEmptyValues() =>
        new Cache([])
            .Values
            .ShouldBe([]);

    [Fact]
    public void ReturnValues() =>
        new Cache(["a", "b"])
            .Add(["c", "d"])
            .Values
            .ShouldBe(["a", "b", "c", "d"]);

    [Fact]
    public void FindValues() =>
        new Cache(["a", "b"])
            .Contains("a")
            .ShouldBe([0]);

    [Fact]
    public void GetValuesByIndex() =>
        new Cache(["a", "b", "c"])
            .FromIndex([0, 2])
            .ShouldBe(["a", "c"]);
    
    [Fact]
    public void ReturnAllIndex() =>
        new Cache(["a", "b"])
            .AllIndex
            .ShouldBe([0,1]);
}