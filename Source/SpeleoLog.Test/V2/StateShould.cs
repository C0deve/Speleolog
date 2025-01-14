using Shouldly;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test.V2;

public class StateShould
{
    [Fact]
    public void Initialize() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("123"))
            .Events
            .ShouldBe([new AddedToTheBottom(Rows: "123")]);

    [Fact]
    public void ClearEvents() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("123"))
            .ClearEvents()
            .Events
            .ShouldBe([]);

    [Fact]
    public void Refresh()
    {
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("123"))
            .Handle(new AddRows("4"))
            .Events
            .ShouldBe([
                new AddedToTheBottom(Rows: "123"),
                new AddedToTheTop(0, true,
                    new DisplayedRow(1, TextBlock.JustAdded("4")))
            ]);
    }

    [Fact]
    public void RefreshWithPaging() =>
        new State()
            .Handle(new SetDisplayedRange(3))
            .Handle(new AddRows("1", "2", "3"))
            .Handle(new AddRows("4", "5", "6"))
            .Events
            .ShouldBe([
                new AddedToTheBottom(0,
                    new DisplayedRow(2, "3"),
                    new DisplayedRow(1, "2"),
                    new DisplayedRow(0, "1")),
                new AddedToTheTop(2,
                    true,
                    new DisplayedRow(5, TextBlock.JustAdded("6")),
                    new DisplayedRow(4, TextBlock.JustAdded("5")),
                    new DisplayedRow(3, TextBlock.JustAdded("4"))
                )
            ]);

    [Fact]
    public void Filter() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("1", "1", "3", "1"))
            .ClearEvents()
            .Handle(new Filter("1"))
            .Events
            .ShouldBe([
                IEvent.AllDeleted,
                new AddedToTheBottom(0,
                    new DisplayedRow(3, "1"),
                    new DisplayedRow(1, "1"),
                    new DisplayedRow(0, "1"))
            ]);

    [Fact]
    public void FilterOnlyIfChanged() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("1", "1", "3", "1"))
            .Handle(new Filter("1"))
            .ClearEvents()
            .Handle(new Filter("1"))
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void FilterWithNoMatch() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("1", "1", "1", "1"))
            .ClearEvents()
            .Handle(new Filter("2"))
            .Events
            .ShouldBe([new AllDeleted()]);

    [Fact]
    public void FilterThenRefresh() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("1", "1", "3", "1"))
            .Handle(new Filter("1"))
            .ClearEvents()
            .Handle(new AddRows("1"))
            .Events
            .ShouldBe([
                new AddedToTheTop(0, true,
                    new DisplayedRow(4, TextBlock.JustAdded("1")))
            ]);

    [Fact]
    public void MaskThenRefresh()
    {
        var enumerable = new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("1", "1", "3", "1"))
            .Handle(new Mask("1"))
            .ClearEvents()
            .Handle(new AddRows("1"))
            .Events
            .ToArray();
        enumerable
            .ShouldBe([
                new AddedToTheTop(0, true,
                    new DisplayedRow(4, TextBlock.JustAdded("")))
            ]);
    }

    [Fact]
    public void Mask() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("abc", "def", "abg", "ab "))
            .ClearEvents()
            .Handle(new Mask("ab"))
            .Events
            .ShouldBe([
                new AllDeleted(),
                new AddedToTheBottom(0,
                    new DisplayedRow(3, " "),
                    new DisplayedRow(2, "g"),
                    new DisplayedRow(1, "def"),
                    new DisplayedRow(0, "c"))
            ]);

    [Fact]
    public void MaskOnlyIfChanged() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("abc", "def", "abg", "ab "))
            .Handle(new Mask("ab"))
            .ClearEvents()
            .Handle(new Mask("ab"))
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void Paginate() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .Events
            .ShouldBe([
                new AddedToTheBottom(Rows: Enumerable
                    .Range(85, 15)
                    .Reverse()
                    .Select(x => new DisplayedRow(x, $"{x}"))
                    .ToArray()
                )
            ]);

    [Fact]
    public void PaginateOnFilter() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows(Enumerable.Repeat("a", 100).ToArray()))
            .ClearEvents()
            .Handle(new Filter("a"))
            .Events
            .ToArray()
            .ShouldBe([
                    new AllDeleted(),
                    new AddedToTheBottom(Rows: Enumerable
                        .Range(85, 15)
                        .Reverse()
                        .Select(x => new DisplayedRow(x, "a"))
                        .ToArray())
                ]
            );

    [Fact]
    public void GoPrevious() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .ClearEvents()
            .Handle(new Previous())
            .Events
            .ShouldBe([
                new AddedToTheBottom(15,
                    Rows: Enumerable.Range(70, 15)
                        .Reverse()
                        .Select(x => new DisplayedRow(x, $"{x}"))
                        .ToArray()
                ),
            ]);

    [Fact]
    public void GoNext() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .Handle(new Previous())
            .Handle(new Previous())
            .Handle(new Previous())
            .Handle(new Previous())
            .ClearEvents()
            .Handle(new Next())
            .Events
            .ShouldBe([
                new AddedToTheTop(RemovedFromBottomCount: 5,
                    IsOnTop: true,
                    Rows: Enumerable.Range(90, 10)
                        .Reverse()
                        .Select(x => new DisplayedRow(x, $"{x}"))
                        .ToArray()
                )
            ]);

    [Fact]
    public void GoTop() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .Handle(new Previous())
            .ClearEvents()
            .Handle(new GoToTop())
            .Events
            .ShouldBe([
                new AllDeleted(),
                new AddedToTheBottom(RemovedFromTopCount: 0,
                    Rows: Enumerable.Range(85, 15)
                        .Reverse()
                        .Select(x => new DisplayedRow(x, $"{x}"))
                        .ToArray()
                )
            ]);

    [Fact]
    public void GoNextOnLastPage() =>
        new State()
            .Handle(new SetDisplayedRange(100))
            .Handle(new AddRows(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .ClearEvents()
            .Handle(new Next())
            .Events
            .ShouldBeEmpty();


    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[] { new Filter("") },
        };

    [Theory, MemberData(nameof(Data))]
    public void NotInitialize(ICommand command) =>
        new State().Handle(new SetDisplayedRange(10))
            .ClearEvents()
            .Handle(command)
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void SetErrorTag() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("1", "1", "3", "1"))
            .ClearEvents()
            .Handle(new SetErrorTag("1"))
            .Events
            .ShouldBe([
                new AllDeleted(),
                new AddedToTheBottom(Rows:
                [
                    new DisplayedRow(3, new TextBlock("1", IsError: true)),
                    new DisplayedRow(2, new TextBlock("3")),
                    new DisplayedRow(1, new TextBlock("1", IsError: true)),
                    new DisplayedRow(0, new TextBlock("1", IsError: true))
                ])
            ]);

    [Fact]
    public void SetErrorTagOnlyIfChanged() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("abc", "def"))
            .Handle(new SetErrorTag("1"))
            .ClearEvents()
            .Handle(new SetErrorTag("1"))
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void Highlight() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows("abc", "def", "abg", "_ab "))
            .ClearEvents()
            .Handle(new Highlight("ab"))
            .Events
            .ShouldBe([
                new AllReplaced(
                    new DisplayedRow(3, "_", new TextBlock("ab", IsHighlighted: true), " "),
                    new DisplayedRow(2, new TextBlock("ab", IsHighlighted: true), "g"),
                    new DisplayedRow(1, "def"),
                    new DisplayedRow(0, new TextBlock("ab", IsHighlighted: true), "c")
                )
            ]);

    [Fact]
    public void SetPageRange() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new SetDisplayedRange(20))
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void SetSmallerPageRange() =>
        new State()
            .Handle(new SetDisplayedRange(10))
            .Handle(new AddRows(Enumerable.Repeat("a", 30).ToArray()))
            .ClearEvents()
            .Handle(new SetDisplayedRange(20))
            .Events
            .ShouldBe([
                new AllDeleted(),
                new AddedToTheBottom(Rows: Enumerable.Range(0, 30)
                        .Reverse()
                        .Select(x => new DisplayedRow(x, new TextBlock("a")))
                        .ToArray())
            ]);
}