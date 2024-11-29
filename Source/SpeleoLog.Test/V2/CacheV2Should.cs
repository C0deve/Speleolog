using Shouldly;
using SpeleoLog.LogFileViewer.V2;

namespace SpeleoLog.Test.V2;

public class CacheV2Should
{
    private readonly string[] _input = ["a", "b"];

    private CacheV2 InitCache(params string[] input)
    {
        var cache = new CacheV2();
        cache.Push(input.Length > 0 ? input : _input);
        return cache;
    }

    [Fact]
    public void GetLastAddedIndex() =>
        InitCache("a", "b")
            .Push("c", "d")
            .LastAddedIndex
            .ShouldBe([2, 3]);

    [Fact]
    public void NotBeInitializedOnFirstRefresh() =>
        new CacheV2()
            .Push("")
            .IsInitialized
            .ShouldBeFalse();

    [Fact]
    public void BeInitializedOnSecondRefresh() =>
        new CacheV2()
            .Push("")
            .Push("")
            .IsInitialized
            .ShouldBeTrue();

    [Fact]
    public void GetLastAddedOnThirdRefresh()
    {
        new CacheV2()
            .Push("a", "b")
            .Push("d")
            .Push("e")
            .LastAddedIndex
            .ShouldBe([3]);
    }

    [Fact]
    public void GetEmptyLastAddedIndexOnEmptyRefresh() =>
        InitCache()
            .Push()
            .LastAddedIndex
            .ShouldBeEmpty();

    [Fact]
    public void GetValuesByIndex() =>
        InitCache("a", "b", "c")
            [[0, 2]]
            .Select(tuple => tuple.Text)
            .ShouldBe(["a" , "c" ]);

    [Fact]
    public void GetEmptyLastAddedIndexOnRefreshWithNoMatchText() =>
        InitCache("a")
            .SetSearchTerm("a")
            .Push("b")
            .LastAddedIndex
            .ShouldBeEmpty();

    [Fact]
    public void ReturnFilteredRows() =>
        InitCache("a", "b", "a")
            .SetSearchTerm("a")[Enumerable.Range(0, 2)]
            .ShouldBe([new LogRow("a"), new LogRow("a")]);

    [Fact]
    public void Mask() =>
        InitCache("aa", "ab", "aaab")
            .SetMask("a")[..]
            .ShouldBe([new LogRow("a"), new LogRow("b"), new LogRow("aab")]);

    [Fact]
    public void ReturnFilteredRowsIfNoMatch() =>
        InitCache("a", "b", "a")
            .SetSearchTerm("Z")[Enumerable.Range(0, 2)]
            .ShouldBeEmpty();

    [Fact]
    public void ReturnFilteredRowsIfIndexOutOfRange() =>
        InitCache("a", "b", "c")
            .SetSearchTerm("a")[Enumerable.Range(0, 10)]
            .ShouldBe([new LogRow("a")]);

    [Fact]
    public void ReturnFilteredRowsIfIndexOutOfRange2() =>
        InitCache("a", "b", "c")
            .SetSearchTerm("a")[Enumerable.Range(1, 10)]
            .ShouldBeEmpty();
}