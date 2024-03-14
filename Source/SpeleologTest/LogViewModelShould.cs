using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class LogViewModelShould
{
    private static readonly TimeSpan OperationDelay = TimeSpan.FromMilliseconds(10);

    [Fact]
    public void AppendLinesOnCreation()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), true,
            new TextFileLoaderForTest(["A", "B", "C"]),
            scheduler);

        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.AllLines.Count.ShouldBe(3);
    }

    [Fact]
    public void AppendLinesOnFileChanged()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), true,
            new TextFileLoaderForTest(lines),
            scheduler);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(lines);
    }

    [Fact]
    public void AppendLinesInReverseOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();

        using var sut = new LogViewModel("",
            emitter.AsObservable(),
            false,
            new SequenceTextFileLoaderForTest(["A"], ["A", "B", "C"]),
            scheduler);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(["C", "B", "A"]);
    }

    [Fact]
    public void FirstAppendLinesAreNotJustAppend()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), true,
            new TextFileLoaderForTest(["A", "B", "C"]),
            scheduler);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.AllLines.ShouldNotBeEmpty();
        sut.AllLines.All(vm => !vm.JustAdded).ShouldBeTrue();
    }

    [Fact]
    public void SecondAppendLinesAreJustAppend()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), false,
            new SequenceTextFileLoaderForTest(["A", "B", "C"], ["A", "B", "C", "D", "E"]),
            scheduler);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);
        sut.AllLines.Take(2).All(vm => vm.JustAdded).ShouldBeTrue();
    }

    [Fact]
    public void MaskText()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), true,
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK"]),
            scheduler);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.MaskText = "mask";

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe([" A", "B ", "C"]);
    }

    [Fact]
    public void DisplayLinesContainingFilterOnFilterChanged()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), true,
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK", "coucou"]),
            scheduler);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.Filter = "mask";

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(["mask A", "B masK", "CMASK"]);
    }

    [Fact]
    public void DisplayLinesContainingFilterOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), true,
            new SequenceTextFileLoaderForTest(["coucou"], ["coucou", "mask A", "B masK", "CMASK"]),
            scheduler);

        sut.Filter = "mask";
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(["mask A", "B masK", "CMASK"]);
    }

    [Fact]
    public void DisplayAllLinesOnResetFilter()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        using var sut = new LogViewModel("", emitter.AsObservable(), true,
            new TextFileLoaderForTest(["coucou", "mask A"]),
            scheduler);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);
        sut.Filter = "mask";

        sut.Filter = "";

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(["coucou", "mask A"]);
    }
}