using Shouldly;
using SpeleoLogViewer.LogFileViewer.V2;

namespace SpeleologTest.V2;

public class StateShould
{
    [Fact]
    public void Initialize() =>
        State
            .Initial(10)
            .Handle(new Refresh("123"))
            .Events
            .ShouldBe([new AddedToTheTop(10, 1, true, rows: "123")]);

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
                new AddedToTheTop(10, 1, true, rows: "123"),
                new AddedToTheTop(10, 1, true, rows: LogLine.NewLine("4"))
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
                new AddedToTheTop(10, 1, true, "1", "2", "3"),
                new AddedToTheTop(10, 1, true, LogLine.NewLine("4"), LogLine.NewLine("5"), LogLine.NewLine("6")),
            ]);

    [Fact]
    public void Filter() =>
        State
            .Initial(10)
            .Handle(new Refresh("1", "1", "3", "1"))
            .Handle(new Filter("1"))
            .Events
            .ShouldBe([
                new AddedToTheTop(10, 1, true, "1", "1", "3", "1"),
                IEvent.DeletedAll, 
                new AddedToTheTop(10, 1, true, "1", "1", "1")
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
            .Handle(new Refresh("1"))
            .Events
            .ShouldBe([
                new AddedToTheTop(10, 1, true, "1", "1", "3", "1"),
                new DeletedAll(),
                new AddedToTheTop(10, 1, true, "1", "1", "1"),
                new AddedToTheTop(10, 1, true, LogLine.NewLine("1"))
            ]);

    [Fact]
    public void MaskThenRefresh()
    {
        var enumerable = State
            .Initial(10)
            .Handle(new Refresh("1", "1", "3", "1"))
            .Handle(new Mask("1"))
            .Handle(new Refresh("1"))
            .Events
            .ToArray();
        enumerable
            .ShouldBe([
                new AddedToTheTop(10, 1, true, "1", "1", "3", "1"),
                new DeletedAll(),
                new AddedToTheTop(10, 1, true, "", "", "3", ""),
                new AddedToTheTop(10, 1, true, LogLine.NewLine(""))
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
                new AddedToTheTop(10, 1, false, "c", "def", "g", " ")
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
                new AddedToTheTop(0, 10,
                    true,
                    Enumerable
                        .Range(90, 10)
                        .Select(x => new LogLine($"{x}"))
                        .ToArray())
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
            .ShouldBe([new DeletedAll(), new AddedToTheTop(10, 1, true, rows: Enumerable.Repeat(new LogLine("a"), 10).ToArray())]);

    [Fact]
    public void GoPrevious() =>
        State
            .Initial(10)
            .Handle(new Refresh(Enumerable.Range(0, 100).Select(x => $"{x}").ToArray()))
            .ClearEvents()
            .Handle(new Previous())
            .Events
            .ShouldBe([
                new AddedToTheBottom(10, 10, [..Enumerable.Range(40, 10).Select(x => $"{x}").Select(s => new LogLine(s))]),
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
                new AddedToTheTop(10, 10, false, [..Enumerable.Range(90, 10).Select(x => $"{x}")]),
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
                new AddedToTheTop(10, 1, true, LogLine.Error("1"), LogLine.Error("1"), "3", LogLine.Error("1"))
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
}