using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest.V2;

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
    public void AlwaysBeGreaterThanZero() =>
        new PageRange(0, 5)
            .Move(-10)
            .ShouldBe(PageRange.Create(0, 4));
    
    [Fact]
    public void MoveBackward() =>
        new PageRange(5, 5)
            .Move(-3)
            .ShouldBe(PageRange.Create(2, 6));

    [Fact]
    public void ThrowIfCompareWithShorterRange() =>
        Should.Throw<ArgumentException>(() =>
            new PageRange(0, 5).Compare(PageRange.Empty));
    
    [Fact]
    public void CompareStrictlyGreaterThan()
    {
        var sut = new PageRange(0, 5)
            .Compare(new PageRange(5, 5))
            .ShouldBeOfType<IsGoneForward>();

        sut.AddedFomTop.ShouldBe([5, 6, 7, 8, 9]);
        sut.DeleteFromBottom.ShouldBe([0, 1, 2, 3, 4]);
    }

    [Fact]
    public void CompareStrictlyLesserThan()
    {
        var sut = new PageRange(5, 5)
            .Compare(new PageRange(0, 5))
            .ShouldBeOfType<IsGoneBackward>();

        sut.AddedFomBottom.ShouldBe([0, 1, 2, 3, 4]);
        sut.DeleteFromTop.ShouldBe([5, 6, 7, 8, 9]);
    }

    [Theory, MemberData(nameof(DataCompareGreaterThan))]
    public void CompareGreaterThan(int start1, int size1, int start2, int size2, int[] expectedAdded, int[] expectedRemoved)
    {
        var sut = new PageRange(start1, size1)
            .Compare(new PageRange(start2, size2))
            .ShouldBeOfType<IsGoneForward>();

        sut.AddedFomTop.ShouldBe(expectedAdded);
        sut.DeleteFromBottom.ShouldBe(expectedRemoved);
    }

    public static IEnumerable<object[]> DataCompareGreaterThan =>
        new List<object[]>
        {
            new object[] { 0, 10, 5, 10, new[] { 10, 11, 12, 13, 14 }, new[] { 0, 1, 2, 3, 4 } },
            new object[] { 3, 3, 4, 3, new[] { 6 }, new[] { 3 } },
            new object[] { 3, 3, 4, 6, new[] { 6, 7, 8, 9 }, new[] { 3 } },
        };

    [Theory, MemberData(nameof(DataCompareLesserThan))]
    public void CompareLesserThan(int start1, int size1, int start2, int size2, int[] expectedAddedFromBottomCount, int[] expectedRemoved)
    {
        var sut = new PageRange(start1, size1)
            .Compare(new PageRange(start2, size2))
            .ShouldBeOfType<IsGoneBackward>();

        sut.AddedFomBottom.ShouldBe(expectedAddedFromBottomCount);
        sut.DeleteFromTop.ShouldBe(expectedRemoved);
    }

    public static IEnumerable<object[]> DataCompareLesserThan =>
        new List<object[]>
        {
            new object[] { 5, 10, 0, 10, new[] { 0, 1, 2, 3, 4 }, new[] { 10, 11, 12, 13, 14 } },
            new object[] { 4, 3, 3, 3, new[] { 3 }, new[] { 6 } },
            new object[] { 0, 0, 4, 3, new[] { 4, 5, 6 }, Array.Empty<int>() },
        };
}