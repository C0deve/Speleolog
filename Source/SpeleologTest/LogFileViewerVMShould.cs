﻿using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Shouldly;
using SpeleoLogViewer.LogFileViewer;

namespace SpeleologTest;

public class LogFileViewerVMShould
{
    private static readonly TimeSpan OperationDelay = TimeSpan.FromMilliseconds(10);
    private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);

    [Fact]
    public void EmitRefreshAllOnCreation()
    {
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        var text = string.Empty;
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["A", "B", "C"]), 300,
            scheduler);

        scheduler.AdvanceBy(OperationDelay.Ticks);
        sut.RefreshAllStream.Subscribe(s => text = s);

        text.ShouldBe(string.Join(Environment.NewLine, ["C", "B", "A"]));
    }

    [Fact]
    public void EmitChangesOnFileChanged()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        var text = string.Empty;
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest([""], lines), 300,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks);  // file loading
        sut.ChangesStream.Subscribe(s => text = s);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        text.ShouldBe(string.Join(Environment.NewLine, ["C", "B", "A", ""]));
    }

    [Fact]
    public void ChangesEmitOnlyDiff()
    {
        string[] lines = ["A", "B", "C", "D"];
        var emitter = new Subject<Unit>();
        var scheduler = new TestScheduler();
        var text = string.Empty;
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["A"], lines), 300,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading
        sut.ChangesStream.Subscribe(s => text = s);

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks); // file loading

        text.ShouldBe(string.Join(Environment.NewLine, ["D", "C", "B", ""]));
    }

    [Fact]
    public void EmitRefreshAllOnFilterChanged()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK", "coucou"]), 300,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks);  // file loading
        sut
            .RefreshAllStream
            .Subscribe(s => text = s);

        sut.Filter = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        text.ShouldBe(string.Join(Environment.NewLine, ["CMASK", "B masK", "mask A"]));
    }

    [Fact]
    public void EmitFilteredChangesOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["coucou"], ["coucou", "mask A", "B masK", "coucou", "CMASK"]), 300,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks);  // file loading
        sut
            .ChangesStream
            .Subscribe(s => text = s);
        sut.Filter = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        text.ShouldBe(string.Join(Environment.NewLine, ["CMASK", "B masK", "mask A", ""]));
    }

    [Fact]
    public void EmitRefreshAllOnResetFilter()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "mask A"]), 300,
            scheduler);
        sut.RefreshAllStream
            .Skip(1) // skip refresh from creation
            .Subscribe(s => text = s);
        sut.Filter = "mask";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(250).Ticks); // throttle time

        sut.Filter = "";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(250).Ticks); // throttle time

        text.ShouldBe(string.Join(Environment.NewLine, ["mask A", "coucou"]));
    }

    [Fact]
    public void EmitRefreshAllOnMask()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK"]), 300,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks);  // file loading
        sut.RefreshAllStream
            .Subscribe(s => text = s);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.MaskText = "mask";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time
        
        text.ShouldBe(string.Join(Environment.NewLine, ["C", "B ", " A"]));
    }
    
    [Fact]
    public void EmitRefreshAllOnMaskAndFilter()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "filterA", "Mask B", "filterC Mask"]), 300,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks);  // file loading
        sut.RefreshAllStream
            .Subscribe(s => text = s);
        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        sut.MaskText = "mask";
        sut.Filter = "filter";
        scheduler.AdvanceBy(_throttleTime.Ticks); // throttle time
        
        text.ShouldBe(string.Join(Environment.NewLine, ["filterC ", "filterA"]));
    }
    
    [Fact]
    public void EmitMaskedChangesOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["coucou"], ["coucou", "mask A", "B masK", "coucou", "CMASK"]), 300,
            scheduler);
        sut.ChangesStream.Subscribe(s => text = s);
        sut.MaskText = "mask";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(250).Ticks); // throttle time

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        text.ShouldBe(string.Join(Environment.NewLine, ["C", "coucou", "B ", " A", ""]));
    }
    
    [Fact]
    public void EmitMaskedAndFilteredChangesOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new SequenceTextFileLoaderForTest(["coucou"], ["coucou", "filterA", "Mask filterB"]), 300,
            scheduler);
        sut.ChangesStream.Subscribe(s => text = s);
        sut.MaskText = "mask";
        sut.Filter = "filter";
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(250).Ticks); // throttle time

        emitter.OnNext(Unit.Default);
        scheduler.AdvanceBy(OperationDelay.Ticks);

        text.ShouldBe(string.Join(Environment.NewLine, [" filterB", "filterA", ""]));
    }
    
    [Fact]
    public void EmitPageChangesOnDisplayNextPage()
    {
        var emitter = new Subject<Unit>();
        var text = string.Empty;
        var scheduler = new TestScheduler();
        using var sut = new LogFileViewerVM("", emitter.AsObservable(),
            new TextFileLoaderForTest(["coucou", "mask A", "B masK", "coucou", "CMASK"]), 2,
            scheduler);
        scheduler.AdvanceBy(OperationDelay.Ticks);
        sut.PageChangesStream.Subscribe(s => text = s);
        
        sut.DisplayNextPage();
        scheduler.AdvanceBy(OperationDelay.Ticks);

        text.ShouldBe(string.Join(Environment.NewLine,["B masK", "mask A"]));
    }
}