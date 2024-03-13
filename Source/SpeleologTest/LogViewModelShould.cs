using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shouldly;
using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class LogViewModelShould
{
    private static readonly TimeSpan OperationDelay = TimeSpan.FromMilliseconds(10);

    [Fact]
    public async Task AppendLinesOnCreation()
    {
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true, 
            new TextFileLoaderForTest(["A", "B", "C"]));
        
        await Task.Delay(OperationDelay);

        sut.AllLines.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task AppendLinesOnFileChanged()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true, 
            new TextFileLoaderForTest(lines));
        
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(lines);
    }
    
    [Fact]
    public async Task AppendLinesInReverseOnFileChanged()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), false, 
            new TextFileLoaderForTest(lines));
        
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(lines.Reverse());
    }
    
    [Fact]
    public async Task FirstAppendLinesAreNotJustAppend()
    {
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true, 
            new TextFileLoaderForTest(["A", "B", "C"]));
        
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);

        sut.AllLines.ShouldNotBeEmpty();
        sut.AllLines.All(vm => !vm.JustAdded).ShouldBeTrue();
    }
    
    [Fact]
    public async Task SecondAppendLinesAreJustAppend()
    {
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), false, 
            new SequenceTextFileLoaderForTest(["A", "B", "C"],["A", "B", "C", "D", "E"]));
        
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);
        
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);
        sut.AllLines.Take(2).All(vm => vm.JustAdded).ShouldBeTrue();
    }
    
    [Fact]
    public async Task MaskText()
    {
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true, 
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK"]));
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);

        sut.MaskText = "mask";
        
        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe([" A", "B ", "C"]);
    }
    
    [Fact]
    public async Task DisplayLinesContainingFilterOnFilterChanged()
    {
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true, 
            new TextFileLoaderForTest(["mask A", "B masK", "CMASK", "coucou"]));
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);

        sut.Filter = "mask";
        
        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(["mask A", "B masK", "CMASK"]);
    }
    
    [Fact]
    public async Task DisplayLinesContainingFilterOnFileChanged()
    {
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true, 
            new SequenceTextFileLoaderForTest(["coucou"],["coucou", "mask A", "B masK", "CMASK"]));
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);

        sut.Filter = "mask";
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(["mask A", "B masK", "CMASK"]);
    }
    
    [Fact]
    public async Task DisplayAllLinesOnResetFilter()
    {
        var emitter = new Subject<Unit>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true, 
            new TextFileLoaderForTest(["coucou", "mask A"]));
        emitter.OnNext(Unit.Default);
        await Task.Delay(OperationDelay);
        sut.Filter = "mask";
        
        sut.Filter = "";

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(["coucou", "mask A"]);
    }
}