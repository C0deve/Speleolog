using Shouldly;
using SpeleoLog.LogFileViewer.V2;

namespace SpeleoLog.Test.V2;

public class StateShould
{
    [Fact]
    public void Initialize() =>
        State
            .Initial(10)
            .Handle(new Refresh("123"))
            .Events
            .ShouldBe([new AddedToTheBottom(blocs: "123" + Environment.NewLine)]);

    [Fact]
    public void ClearEvents() =>
        State
            .Initial(10)
            .Handle(new Refresh("123"))
            .ClearEvents()
            .Events
            .ShouldBe([]);

    [Fact]
    public void Refresh()
    {
        State
            .Initial(10)
            .Handle(new Refresh("123"))
            .Handle(new Refresh("4"))
            .Events
            .ShouldBe([
                new AddedToTheBottom(blocs: "123" + Environment.NewLine),
                new AddedToTheTop(0, 1, true,
                    new DisplayBloc("4" + Environment.NewLine, IsJustAdded: true))
            ]);
    }

    [Fact]
    public void RefreshWithPaging() =>
        State
            .Initial(3)
            .Handle(new Refresh("1", "2", "3"))
            .Handle(new Refresh("4", "5", "6"))
            .Events
            .ShouldBe([
                new AddedToTheBottom(0,
                    0,
                    "3" + Environment.NewLine + "2" + Environment.NewLine + "1" + Environment.NewLine),
                new AddedToTheTop(3,
                    3,
                    true,
                    new DisplayBloc("6" + Environment.NewLine + "5" + Environment.NewLine + "4" + Environment.NewLine,
                        true)),
            ]);

    [Fact]
    public void Filter() =>
        State
            .Initial(10)
            .Handle(new Refresh("1", "1", "3", "1"))
            .ClearEvents()
            .Handle(new Filter("1"))
            .Events
            .ShouldBe([
                IEvent.DeletedAll,
                new AddedToTheBottom(0, 0,
                    "1" + Environment.NewLine +
                    "1" + Environment.NewLine +
                    "1" + Environment.NewLine)
            ]);

    [Fact]
    public void FilterOnlyIfChanged() =>
        State
            .Initial(10)
            .Handle(new Refresh("1", "1", "3", "1"))
            .Handle(new Filter("1"))
            .ClearEvents()
            .Handle(new Filter("1"))
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void FilterWithNoMatch() =>
        State
            .Initial(10)
            .Handle(new Refresh("1", "1", "1", "1"))
            .ClearEvents()
            .Handle(new Filter("2"))
            .Events
            .ShouldBe([new DeletedAll()]);

    [Fact]
    public void FilterThenRefresh() =>
        State
            .Initial(10)
            .Handle(new Refresh("1", "1", "3", "1"))
            .Handle(new Filter("1"))
            .ClearEvents()
            .Handle(new Refresh("1"))
            .Events
            .ShouldBe([
                new AddedToTheTop(0, 3, true,
                    new DisplayBloc("1" + Environment.NewLine, IsJustAdded: true))
            ]);

    [Fact]
    public void MaskThenRefresh()
    {
        var enumerable = State
            .Initial(10)
            .Handle(new Refresh("1", "1", "3", "1"))
            .Handle(new Mask("1"))
            .ClearEvents()
            .Handle(new Refresh("1"))
            .Events
            .ToArray();
        enumerable
            .ShouldBe([
                new AddedToTheTop(0, 4, true,
                    new DisplayBloc(Environment.NewLine, IsJustAdded: true))
            ]);
    }

    [Fact]
    public void Mask() =>
        State
            .Initial(10)
            .Handle(new Refresh("abc", "def", "abg", "ab "))
            .ClearEvents()
            .Handle(new Mask("ab"))
            .Events
            .ShouldBe([
                new DeletedAll(),
                new AddedToTheBottom(0,
                    0,
                    " " + Environment.NewLine +
                    "g" + Environment.NewLine +
                    "def" + Environment.NewLine +
                    "c" + Environment.NewLine)
            ]);

    [Fact]
    public void MaskOnlyIfChanged() =>
        State
            .Initial(10)
            .Handle(new Refresh("abc", "def", "abg", "ab "))
            .Handle(new Mask("ab"))
            .ClearEvents()
            .Handle(new Mask("ab"))
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void Paginate() =>
        State
            .Initial(10)
            .Handle(new Refresh(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .Events
            .ShouldBe([
                new AddedToTheBottom(blocs: string.Join("", Enumerable
                    .Range(90, 10)
                    .Reverse()
                    .Select(x => $"{x}" + Environment.NewLine)
                ))
            ]);

    [Fact]
    public void PaginateOnFilter() =>
        State
            .Initial(10)
            .Handle(new Refresh(Enumerable.Repeat("a", 100).ToArray()))
            .ClearEvents()
            .Handle(new Filter("a"))
            .Events
            .ToArray()
            .ShouldBe([
                    new DeletedAll(),
                    new AddedToTheBottom(blocs: string.Join("", Enumerable.Repeat("a" + Environment.NewLine, 10)))
                ]
            );

    [Fact]
    public void GoPrevious() =>
        State
            .Initial(10)
            .Handle(new Refresh(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .ClearEvents()
            .Handle(new Previous())
            .Events
            .ShouldBe([
                new AddedToTheBottom(10,
                    10,
                    blocs: string.Join("", Enumerable.Range(40, 10)
                        .Reverse()
                        .Select(x => $"{x}" + Environment.NewLine)
                    )),
            ]);

    [Fact]
    public void GoNext() =>
        State
            .Initial(10)
            .Handle(new Refresh(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .Handle(new Previous())
            .ClearEvents()
            .Handle(new Next())
            .Events
            .ShouldBe([
                new AddedToTheTop(removedFromBottomCount: 10,
                    previousPageSize: 10,
                    isOnTop: true,
                    blocs: string.Join("", Enumerable.Range(90, 10)
                        .Reverse()
                        .Select(x => $"{x}" + Environment.NewLine)
                    ))
            ]);
 [Fact]
    public void GoTop() =>
        State
            .Initial(10)
            .Handle(new Refresh(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .Handle(new Previous())
            .ClearEvents()
            .Handle(new GoToTop())
            .Events
            .ShouldBe([
                new DeletedAll(),
                new AddedToTheBottom(removedFromTopCount: 0,
                    previousPageSize: 0,
                    blocs: string.Join("", Enumerable.Range(90, 10)
                        .Reverse()
                        .Select(x => $"{x}" + Environment.NewLine)
                    ))
            ]);

    [Fact]
    public void GoNextOnLastPage() =>
        State
            .Initial(pageRange: 100)
            .Handle(new Refresh(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
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
        State.Initial(10)
            .ClearEvents()
            .Handle(command)
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void SetErrorTag() =>
        State
            .Initial(10)
            .Handle(new Refresh("1", "1", "3", "1"))
            .ClearEvents()
            .Handle(new SetErrorTag("1"))
            .Events
            .ShouldBe([
                new DeletedAll(),
                new AddedToTheBottom(blocs:
                [
                    new DisplayBloc("1" + Environment.NewLine, IsError: true),
                    "3" + Environment.NewLine,
                    new DisplayBloc("1" + Environment.NewLine + "1" + Environment.NewLine, IsError: true),
                ])
            ]);

    [Fact]
    public void SetErrorTagOnlyIfChanged() =>
        State
            .Initial(10)
            .Handle(new Refresh("abc", "def"))
            .Handle(new SetErrorTag("1"))
            .ClearEvents()
            .Handle(new SetErrorTag("1"))
            .Events
            .ShouldBeEmpty();

    [Fact]
    public void Highlight() =>
        State
            .Initial(10)
            .Handle(new Refresh("abc", "def", "abg", "_ab "))
            .ClearEvents()
            .Handle(new Highlight("ab"))
            .Events
            .ShouldBe([
                new Updated(
                    "_",
                    new DisplayBloc("ab", IsHighlighted: true),
                    " " + Environment.NewLine,
                    new DisplayBloc("ab", IsHighlighted: true),
                    "g" + Environment.NewLine + "def" + Environment.NewLine,
                    new DisplayBloc("ab", IsHighlighted: true),
                    "c" + Environment.NewLine
                )
            ]);
}