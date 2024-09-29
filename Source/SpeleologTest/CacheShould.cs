using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class CacheShould
{
    private readonly string _input = string.Join(Environment.NewLine, "a", "b");

    [Fact]
    public void ReturnEmptyValues() =>
        new Cache()
            .Init("")
            .Values
            .ShouldBe([]);

    [Fact]
    public void EmitInitializedOnInit()
    {
        var cache = new Cache();
        int[] actual = [];
        cache.Initialized.Subscribe(x => actual = x);

        cache.Init(_input);

        actual.ShouldBe([0, 1]);
    }

    [Fact]
    public void EmitAddedOnRefresh()
    {
        var cache = new Cache();
        string[] actual = [];
        cache.Added.Subscribe(x => actual = x);
        cache.Init(_input);

        cache.Refresh(string.Join(Environment.NewLine, "a", "b", "c", "d"));

        actual.ShouldBe(["c", "d"]);
    }
    
    [Fact]
    public void NotEmitAddedOnRefreshWithSameText()
    {
        var cache = new Cache();
        int actual = 0;
        cache.Added.Subscribe(_ => actual++);
        cache.Init(_input);

        cache.Refresh(_input);

        actual.ShouldBe(0);
    }

    [Fact]
    public void FindValues() =>
        new Cache()
            .Init(_input)
            .Contains("a")
            .ShouldBe([0]);

    [Fact]
    public void GetValuesByIndex() =>
        new Cache()
            .Init(string.Join(Environment.NewLine, "a", "b", "c"))
            .FromIndex([0, 2])
            .ShouldBe(["a", "c"]);

    [Fact]
    public void ReturnAllIndex() =>
        new Cache()
            .Init(_input)
            .AllIndex
            .ShouldBe([0, 1]);
}