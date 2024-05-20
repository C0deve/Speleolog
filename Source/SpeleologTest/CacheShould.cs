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
}