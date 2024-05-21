using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class PaginatorShould
{
    [Fact]
    public void ReturnPage()
    {
        var sut = new Paginator<string>(["a", "b", "c"], 1);
        sut.Next().ShouldBe(["c"]);
        sut.Next().ShouldBe(["b"]);
        sut.Next().ShouldBe(["a"]);
        sut.Next().ShouldBe([]);
    }

    [Fact]
    public void ReturnEmpty() =>
        new Paginator<string>([], 1)
            .Next()
            .ShouldBe([]);

    [Fact]
    public void ReturnIsEmpty() =>
        new Paginator<string>([], 1)
            .IsEmpty()
            .ShouldBe(true);
}