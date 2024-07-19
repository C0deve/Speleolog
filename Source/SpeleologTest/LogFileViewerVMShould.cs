using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Shouldly;
using SpeleoLogViewer;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class LogFileViewerVMShould
{
    private static readonly TimeSpan OperationDelay = TimeSpan.FromMilliseconds(10);
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);
    private const string ErrorTag = "error";

    [Fact]
    public void EmitRefreshAllOnCreation()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        AddToBottom? message = null;
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["A", "B", "C"]),
            300,
            ErrorTag,
            scheduler);

        scheduler.AdvanceBy(OperationDelay.Ticks);
        sut.RefreshStream.Is<AddToBottom>().Subscribe(s => message = s);

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["C", "B", "A"]);
    }

    [Fact]
    public void EmitAddToBottomOnCreation()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        List<IMessage> messages = [];
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["A", "B", "C"]), 2, ErrorTag,
            scheduler);

        scheduler.AdvanceBy(OperationDelay.Ticks);
        sut.RefreshStream.Subscribe(s => messages.Add(s));

        messages
            .ShouldHaveSingleItem()
            .ShouldBeOfType<AddToBottom>()
            .Logs
            .ToStringArray()
            .Length
            .ShouldBe(2);
    }

    [Fact]
    public void EmitChangesOnFileChanged()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        AddToTop? message = null;
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest([""], lines), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream.Is<AddToTop>().Subscribe(s => message = s);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["C", "B", "A"]);
    }

    [Fact]
    public void ChangesEmitOnlyDiff()
    {
        string[] lines = ["A", "B", "C", "D"];
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        AddToTop? message = null;
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["A"], lines), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream.Is<AddToTop>().Subscribe(s => message = s);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["D", "C", "B"]);
    }

    [Fact]
    public void EmitRefreshAllOnFilterChanged()
    {
        var emitter = new Subject<Unit>();
        List<IMessage> messages = [];
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK", "coucou"]), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut
            .RefreshStream
            .Skip(1) // skip first refresh from constructor
            .Subscribe(s => messages.Add(s));

        sut.Filter = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        messages.Count.ShouldBe(2);

        messages[0].ShouldBeOfType<DeleteAll>();
        messages[1]
            .ShouldBeOfType<AddToBottom>()
            .Logs
            .ToStringArray().ShouldBe(["CMASK", "B masK", "mask A"]);
    }

    [Fact]
    public void EmitFilteredChangesOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        AddToTop? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["coucou"], ["coucou", "mask A", "B masK", "coucou", "CMASK"]), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut
            .RefreshStream
            .Is<AddToTop>().Subscribe(s => message = s);
        sut.Filter = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["CMASK", "B masK", "mask A"]);
    }

    [Fact]
    public void EmitRefreshAllOnResetFilter()
    {
        var emitter = new Subject<Unit>();
        List<IMessage> messages = [];
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM(
            "",
            emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "mask A"]), 
            300,
            ErrorTag,
            scheduler);
        sut
            .RefreshStream
            .Subscribe(s => messages.Add(s));
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.Filter = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time
        
        sut.Filter = "";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        messages.Count.ShouldBe(5); // AddToBottomn, DeleteAll, AddToBottomn, DeleteAll, AddToBottomn 
        messages[3].ShouldBeOfType<DeleteAll>();
        messages[4]
            .ShouldBeOfType<AddToBottom>()
            .Logs
            .ToStringArray().ShouldBe(["mask A", "coucou"]);
    }

    [Fact]
    public void ResetCurrentPageOnResetFilter()
    {
        var emitter = new Subject<Unit>();
        AddToBottom? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "mask A"]), 1, ErrorTag,
            scheduler);
        sut.RefreshStream.Is<AddToBottom>().Subscribe(s => message = s);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.Filter = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        sut.Filter = "";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        sut.NextPage.Execute().Subscribe();
        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["coucou"]);
    }

    [Fact]
    public void DisplayNextPageAfterAllDisplayedAndResetFilter()
    {
        var emitter = new Subject<Unit>();
        AddToBottom? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "mask A"]), 1, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.NextPage.Execute().Subscribe();
        sut.NextPage.Execute().Subscribe();
        sut.Filter = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time
        sut.RefreshStream.Is<AddToBottom>().Subscribe(s => message = s);

        sut.Filter = "";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time
        sut.NextPage.Execute().Subscribe();

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["coucou"]);
    }

    [Fact]
    public void EmitEmptyArrayOnFilterWithNoMatch()
    {
        var emitter = new Subject<Unit>();
        List<IMessage> messages = [];
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["A", "B"]), 3, ErrorTag,
            scheduler);
        sut.RefreshStream.Subscribe(s => messages.Add(s));
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading

        sut.Filter = "C";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        messages
            .Last()
            .ShouldBeOfType<DeleteAll>();
    }

    [Fact]
    public void EmitRefreshAllOnMask()
    {
        var emitter = new Subject<Unit>();
        AddToBottom? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK"]), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream
            .Is<AddToBottom>()
            .Subscribe(s => message = s);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.MaskText = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["C", "B ", " A"]);
    }

    [Fact]
    public void EmitRefreshAllOnMaskAndFilter()
    {
        var emitter = new Subject<Unit>();
        AddToBottom? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "filterA", "Mask B", "filterC Mask"]), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream
            .Is<AddToBottom>()
            .Subscribe(s => message = s);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.MaskText = "mask";
        sut.Filter = "filter";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["filterC ", "filterA"]);
    }

    [Fact]
    public void EmitMaskedChangesOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        AddToTop? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["coucou"], ["coucou", "mask A", "B masK", "coucou", "CMASK"]), 300, ErrorTag,
            scheduler);
        sut.RefreshStream.Is<AddToTop>().Subscribe(s => message = s);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.MaskText = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["C", "coucou", "B ", " A"]);
    }

    [Fact]
    public void EmitMaskedAndFilteredChangesOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        AddToTop? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["coucou"], ["coucou", "filterA", "Mask filterB"]), 300, ErrorTag,
            scheduler);
        sut.RefreshStream.Is<AddToTop>().Subscribe(s => message = s);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.MaskText = "mask";
        sut.Filter = "filter";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe([" filterB", "filterA"]);
    }

    [Fact]
    public void EmitPageChangesOnDisplayNextPage()
    {
        var emitter = new Subject<Unit>();
        AddToBottom? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "mask A", "B masK", "coucou", "CMASK"]), 2, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream.Is<AddToBottom>().Subscribe(s => message = s);

        sut.NextPage.Execute().Subscribe();
        scheduler.AdvanceBy(OperationDelay.Ticks);

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray()
            .ShouldBe(["B masK", "mask A"]);
    }

    [Fact]
    public void NotEmitPageChangesOnDisplayNextPageIfAllDataDisplayed()
    {
        var emitter = new Subject<Unit>();
        List<IMessage> messages = [];
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "mask A", "B masK", "coucou", "CMASK"]), 5, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream.Is<AddToBottom>().Subscribe(s => messages.Add(s));
        messages.Clear();
        
        sut.NextPage.Execute().Subscribe();
        scheduler.AdvanceBy(OperationDelay.Ticks);

        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShowFileLoadingTime()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        var loadingDuration = TimeSpan.FromMilliseconds(100);
        var textFileLoaderWithDuration = new TextFileLoaderWithDuration(loadingDuration);
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            textFileLoaderWithDuration, 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading

        emitter.OnNext(Unit.Default);
        await Task.Delay(loadingDuration + TimeSpan.FromMilliseconds(20));
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.LoadingDuration.ShouldBeInRange(loadingDuration.Milliseconds, (int)(loadingDuration.Milliseconds * 1.20));
    }

    [Fact]
    public void EmitChangesOnReloadFile()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        AddToTop? message = null;
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest([""], lines), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream.Is<AddToTop>().Subscribe(s => message = s);

        sut.Load.Execute().Subscribe();
        scheduler.AdvanceBy(OperationDelay.Ticks);

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["C", "B", "A"]);
    }

    [Fact]
    public void EmitRefreshAllOnErrorTagChanged()
    {
        var emitter = new Subject<Unit>();
        AddToBottom? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["A", "[Error  ]B", "C"]), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream
            .Is<AddToBottom>()
            .Subscribe(s => message = s);

        sut.ErrorTag = "[Error  ]";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["C", "[Error  ]B", "A"]);
    }

    [Fact]
    public void EmitChangesOnFileArchiving()
    {
        var emitter = new Subject<Unit>();
        AddToTop? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["A", "B"], ["E"]), 300, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.RefreshStream.Is<AddToTop>().Subscribe(s => message = s);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["E"]);
    }

    [Fact]
    public void EmitPageChangesOnDisplayNextPageAfterFileArchiving()
    {
        var emitter = new Subject<Unit>();
        AddToBottom? message = null;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["A", "B", "C"], ["D"]), 2, ErrorTag,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);
        sut.RefreshStream.Is<AddToBottom>().Subscribe(s => message = s);

        sut.NextPage.Execute().Subscribe();
        scheduler.AdvanceBy(OperationDelay.Ticks);

        message
            .ShouldNotBeNull()
            .Logs
            .ToStringArray().ShouldBe(["A"]);
    }
}

internal static class Extensions
{
    public static string[] ToStringArray(this IEnumerable<LogLinesAggregate>? aggregates) => aggregates?
        .SelectMany(aggregate => aggregate.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)).ToArray() ?? Array.Empty<string>();
}