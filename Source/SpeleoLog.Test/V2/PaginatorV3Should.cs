using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test.V2;

public class PaginatorV3Should
{
    [Fact]
    public void ReturnEmptyPageOnCreation() =>
        new PaginatorV3()
            .CurrentPage
            .ShouldBe(PageRange.Empty);
    
    [Fact]
    public void ResetToEmptyPageAfterCreation() =>
        new PaginatorV3()
            .Reset(100)
            .CurrentPage
            .ShouldBe(PageRange.Empty);

    [Fact]
    public void ResetToLastPage() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(100)
            .CurrentPage[..]
            .ShouldBe([.. Enumerable.Range(100 - 15, 15)]);

    [Fact]
    public void ResetToLastPageOnSmallSize() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(10)
            .CurrentPage
            .ShouldBe(PageRange.Create(0, 9));

    [Fact]
    public void ReturnActualPage() =>
        new PaginatorV3()
            .SetTotal(100)
            .SetDisplayedRange(12)
            .CurrentPage
            .ShouldBe(PageRange.Empty);

    [Fact]
    public void MoveBackwardsThenResetToLastPage() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(20)
            .MoveBackward()
            .Reset(itemCount: 23)
            .CurrentPage
            .ShouldBe(PageRange.Create(8, 22));


    [Fact]
    public void ReturnIsOnLastPage() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(20)
            .IsOnLastPage
            .ShouldBeTrue();

    [Fact]
    public void HavePageSizeBoundedByItemCount() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(3)
            .CurrentPage[..]
            .ShouldBe([0, 1, 2]);

    [Fact]
    public void HaveEmptyActualPageOnReset0() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(0)
            .CurrentPage
            .ShouldBe(PageRange.Empty);

    [Fact]
    public void MoveBackwards() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(30)
            .MoveBackward()
            .CurrentPage
            .ShouldBe(PageRange.Create(10, 29));

    [Fact]
    public void MoveBackwardsAfterMaxPageSizeReached() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(30)
            .MoveBackward()
            .CurrentPage
            .ShouldBe(PageRange.Create(10, 29));

    [Fact]
    public void PreventMoveBeforeFirstPage() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(11)
            .MoveBackward()
            .CurrentPage
            .ShouldBe(PageRange.Create(0, 10));


    [Fact]
    public void PreventMoveAfterLastPage() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(5)
            .MoveForward()
            .CurrentPage[..]
            .ShouldBe([.. Enumerable.Range(0, 5)]);

    [Fact]
    public void PushIndex() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(3)
            .Push(2)
            .CurrentPage[..]
            .ShouldBe([.. Enumerable.Range(0, 5)]);

    [Fact]
    public void PushIndexKeepLastPage() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(3)
            .Push(2)
            .CurrentPage[..]
            .ShouldBe([0, 1, 2, 3, 4]);

    [Fact]
    public void ReturnEmpty2() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(101)
            .MoveBackward()
            .CurrentPage
            .ShouldBe(PageRange.Create(101 - 15 - 5, 100));

    [Fact]
    public void PushOnEmpty() =>
        new PaginatorV3()
            .SetDisplayedRange(10)
            .Reset(0)
            .Push(50)
            .CurrentPage
            .ShouldBe(PageRange.Create(50 - 15, 49));
}