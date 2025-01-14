using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test.V2;

public class PaginatorShould
{
    [Fact]
    public void ReturnEmptyOnCreation() =>
        new Paginator(5)
            .ActualPage
            .ShouldBe(PageRange.Empty);

    [Fact]
    public void MoveBackwardsThenResetToLastPage() =>
        new Paginator(5)
            .Reset(20)
            .Move(-100)
            .Reset(itemCount: 23)
            .ActualPage
            .ShouldBe(PageRange.Create(18, 22));

    [Fact]
    public void ResetToLastPag() =>
        new Paginator(5)
            .Reset(20)
            .ActualPage[..]
            .ShouldBe([.. Enumerable.Range(15, 5)]);

    [Fact]
    public void ReturnIsOnLastPage() =>
        new Paginator(5)
            .Reset(20)
            .IsOnLastPage
            .ShouldBeTrue();

    [Fact]
    public void HavePageSizeBoundedByItemCount() =>
        new Paginator(10)
            .Reset(3)
            .ActualPage[..]
            .ShouldBe([0, 1, 2]);

    [Fact]
    public void HaveEmptyActualPageOnReset0() =>
        new Paginator(10)
            .Reset(0)
            .ActualPage
            .ShouldBe(PageRange.Empty);

    [Fact]
    public void MoveBackwards() =>
        new Paginator(10)
            .Reset(30)
            .Move(-10)
            .ActualPage
            .ShouldBe(PageRange.Create(10, 19));

    [Fact]
    public void PreventMoveBeforeFirstPage() =>
        new Paginator(10)
            .Reset(11)
            .Move(-100)
            .ActualPage
            .ShouldBe(PageRange.Create(0, 9));


    [Fact]
    public void PreventMoveAfterLastPage() =>
        new Paginator(5)
            .Reset(5)
            .Move(100)
            .ActualPage[..]
            .ShouldBe([.. Enumerable.Range(0, 5)]);

    [Fact]
    public void PushIndex() =>
        new Paginator(10)
            .Reset(3)
            .Push(2)
            .ActualPage[..]
            .ShouldBe([.. Enumerable.Range(0, 5)]);

    [Fact]
    public void PushIndexKeepLastPage() =>
        new Paginator(3)
            .Reset(3)
            .Push(2)
            .ActualPage[..]
            .ShouldBe([2, 3, 4]);
    
    [Fact]
    public void ReturnEmpty2() =>
        new Paginator(100)
            .Reset(101)
            .Move(-50)
            .ActualPage
            .ShouldBe(new PageRange(0, 100));

    [Fact]
    public void PushOnEmpty() =>
        new Paginator(100)
            .Reset(0)
            .Push(50)
            .ActualPage
            .ShouldBe(new PageRange(0, 50));
}