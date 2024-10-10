using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest.V2;

public class EntToStartPaginatorV2Should
{
    [Fact]
    public void ReturnEmptyOnCreation() =>
        new EndToStartPaginatorV2(5)
            .ActualPage
            .ShouldBe(PageRange.Empty);

    [Fact]
    public void MoveBackwardsThenResetToLastPage() =>
        new EndToStartPaginatorV2(5)
            .Reset(20)
            .Move(-100)
            .Reset(itemCount: 23)
            .ActualPage
            .ShouldBe(PageRange.Create(18, 22));

    [Fact]
    public void ResetToLastPag() =>
        new EndToStartPaginatorV2(5)
            .Reset(20)
            .ActualPage[..]
            .ShouldBe([.. Enumerable.Range(15, 5)]);

    [Fact]
    public void ReturnIsOnLastPage() =>
        new EndToStartPaginatorV2(5)
            .Reset(20)
            .IsOnLastPage
            .ShouldBeTrue();

    [Fact]
    public void HavePageSizeBoundedByItemCount() =>
        new EndToStartPaginatorV2(10)
            .Reset(3)
            .ActualPage[..]
            .ShouldBe([0, 1, 2]);

    [Fact]
    public void HaveEmptyActualPageOnReset0() =>
        new EndToStartPaginatorV2(10)
            .Reset(0)
            .ActualPage
            .ShouldBe(PageRange.Empty);

    [Fact]
    public void MoveBackwards() =>
        new EndToStartPaginatorV2(10)
            .Reset(30)
            .Move(-10)
            .ActualPage
            .ShouldBe(PageRange.Create(10, 19));

    [Fact]
    public void PreventMoveBeforeFirstPage() =>
        new EndToStartPaginatorV2(10)
            .Reset(11)
            .Move(-100)
            .ActualPage
            .ShouldBe(PageRange.Create(0, 9));


    [Fact]
    public void PreventMoveAfterLastPage() =>
        new EndToStartPaginatorV2(5)
            .Reset(5)
            .Move(100)
            .ActualPage[..]
            .ShouldBe([.. Enumerable.Range(0, 5)]);

    [Fact]
    public void PushIndex() =>
        new EndToStartPaginatorV2(10)
            .Reset(3)
            .Push(2)
            .ActualPage[..]
            .ShouldBe([.. Enumerable.Range(0, 5)]);

    [Fact]
    public void PushIndexKeepLastPage() =>
        new EndToStartPaginatorV2(3)
            .Reset(3)
            .Push(2)
            .ActualPage[..]
            .ShouldBe([2, 3, 4]);
    
    [Fact]
    public void ReturnEmpty2() =>
        new EndToStartPaginatorV2(100)
            .Reset(101)
            .Move(-50)
            .ActualPage
            .ShouldBe(new PageRange(0, 100));

    [Fact]
    public void PushOnEmpty() =>
        new EndToStartPaginatorV2(100)
            .Reset(0)
            .Push(50)
            .ActualPage
            .ShouldBe(new PageRange(0, 50));
}