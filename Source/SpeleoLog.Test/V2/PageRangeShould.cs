using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test.V2;

public class PageRangeShould
{
    [Fact]
    public void ReturnEmptyFromInvalidInput() =>
        new PageRange(-1, -1)[..].ShouldBeEmpty();

    [Fact]
    public void ReturnEmpty() =>
        PageRange.Empty[..].ShouldBeEmpty();

    [Fact]
    public void ReturnIsEmpty() =>
        PageRange.Empty.IsEmpty.ShouldBeTrue();

    [Fact]
    public void ReturnAreEqual() =>
        new PageRange(0, 5)
            .Compare(new PageRange(0, 5))
            .ShouldBeOfType<IsUnchanged>();

    [Fact]
    public void MoveForward() =>
        new PageRange(0, 5)
            .Move(3)
            .ShouldBe(PageRange.Create(3, 7));

    [Fact]
    public void AlwaysBeGreaterThanZeroOnMove() =>
        new PageRange(0, 5)
            .Move(-10)
            .ShouldBe(PageRange.Create(0, 4));

    [Fact]
    public void ExpandsForward() =>
        new PageRange(0, 5)
            .ExpandsForward(3)
            .ShouldBe(PageRange.Create(0, 7));

    [Fact]
    public void ExpandsForwardBoundedByMaxSize() =>
        PageRange.Create(5, 10)
            .ExpandsForward(3, 7)
            .ShouldBe(PageRange.Create(5, 11));

    [Fact]
    public void MoveBackward() =>
        new PageRange(5, 5)
            .Move(-3)
            .ShouldBe(PageRange.Create(2, 6));

    [Fact]
    public void ExpandsBackwardBoundedByMaxSize() =>
        PageRange.Create(5, 10)
            .ExpandsBackward(3, 7)
            .ShouldBe(PageRange.Create(4, 10));

    [Fact]
    public void AlwaysBeGreaterThanZeroOnAddBackward() =>
        PageRange.Create(0, 10)
            .ExpandsBackward(10)
            .ShouldBe(PageRange.Create(0, 10));

    [Fact]
    public void ExpandsBackward() =>
        PageRange.Create(5, 10)
            .ExpandsBackward(3)
            .ShouldBe(PageRange.Create(2, 10));

    [Fact]
    public void ThrowIfCompareWithShorterRange() =>
        Should.Throw<ArgumentException>(() =>
            new PageRange(0, 5).Compare(PageRange.Empty));

    [Fact]
    public void ThrowIfMoveForwardWithPredateStart() =>
        Should.Throw<ArgumentException>(() =>
            new PageRange(2, 5).Compare(new PageRange(1, 7)));

    [Fact]
    public void CompareStrictlyGreaterThan() =>
        new PageRange(0, 5)
            .Compare(new PageRange(5, 5))
            .ShouldBeOfType<IsGoneForward>()
            .ShouldSatisfyAllConditions(forward =>
            {
                forward.AddedFomTop.ShouldBe([5, 6, 7, 8, 9]);
                forward.DeleteFromBottom.ShouldBe([0, 1, 2, 3, 4]);
            });

    [Fact]
    public void CompareStrictlyLesserThan() =>
        new PageRange(5, 5)
            .Compare(new PageRange(0, 5))
            .ShouldBeOfType<IsGoneBackward>()
            .ShouldSatisfyAllConditions(backward =>
            {
                backward.AddedFomBottom.ShouldBe([0, 1, 2, 3, 4]);
                backward.DeleteFromTop.ShouldBe([5, 6, 7, 8, 9]);
            });

    [Theory, MemberData(nameof(DataCompareGreaterThan))]
    public void CompareGreaterThan(int start1, int size1, int start2, int size2, int[] expectedAdded, int[] expectedRemoved) =>
        new PageRange(start1, size1)
            .Compare(new PageRange(start2, size2))
            .ShouldBeOfType<IsGoneForward>()
            .ShouldSatisfyAllConditions(sut =>
            {
                sut.AddedFomTop.ShouldBe(expectedAdded);
                sut.DeleteFromBottom.ShouldBe(expectedRemoved);
            });

    public static IEnumerable<object[]> DataCompareGreaterThan =>
        new List<object[]>
        {
            new object[] { 0, 10, 5, 10, new[] { 10, 11, 12, 13, 14 }, new[] { 0, 1, 2, 3, 4 } },
            new object[] { 3, 3, 4, 3, new[] { 6 }, new[] { 3 } },
            new object[] { 3, 3, 4, 6, new[] { 6, 7, 8, 9 }, new[] { 3 } },
            new object[] { 0, 5, 4, 4, new[] { 5, 6, 7 }, new[] { 0, 1, 2, 3 } },
            new object[] { 0, 5, 7, 1, new[] { 7 }, new[] { 0, 1, 2, 3, 4 } },
            new object[] { 0, 3, 7, 5, new[] { 7, 8, 9, 10, 11 }, new[] { 0, 1, 2 } },
        };

    [Theory, MemberData(nameof(DataCompareLesserThan))]
    public void CompareLesserThan(int start1, int size1, int start2, int size2, int[] expectedAddedFromBottomCount, int[] expectedRemoved) =>
        new PageRange(start1, size1)
            .Compare(new PageRange(start2, size2))
            .ShouldBeOfType<IsGoneBackward>()
            .ShouldSatisfyAllConditions(sut =>
            {
                sut.AddedFomBottom.ShouldBe(expectedAddedFromBottomCount);
                sut.DeleteFromTop.ShouldBe(expectedRemoved);
            });

    public static IEnumerable<object[]> DataCompareLesserThan =>
        new List<object[]>
        {
            new object[] { 5, 10, 0, 10, new[] { 0, 1, 2, 3, 4 }, new[] { 10, 11, 12, 13, 14 } },
            new object[] { 4, 3, 3, 3, new[] { 3 }, new[] { 6 } },
            new object[] { 0, 0, 4, 3, new[] { 4, 5, 6 }, Array.Empty<int>() },
        };
}