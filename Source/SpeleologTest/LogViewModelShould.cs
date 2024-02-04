using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shouldly;
using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class LogViewModelShould
{
    private static readonly TimeSpan OperationDelay = TimeSpan.FromMilliseconds(10);

    [Fact]
    public async Task AppendLinesInReverseOnFileChanged()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<string[]>();
        using var sut = new LogViewModel("", emitter.AsObservable(), false);
        
        emitter.OnNext(lines);
        await Task.Delay(OperationDelay);

        sut.AllLines.Select(lineVM => lineVM.Text).ShouldBe(lines.Reverse());
    }
    
    [Fact]
    public async Task FirstAppendLinesAreNotJustAppend()
    {
        var emitter = new Subject<string[]>();
        using var sut = new LogViewModel("", emitter.AsObservable(), true);
        
        emitter.OnNext(["A", "B", "C"]);
        await Task.Delay(OperationDelay);

        sut.AllLines.Any(vm => vm.JustAppend).ShouldBeFalse();
    }
    
    [Fact]
    public async Task SecondAppendLinesAreJustAppend()
    {
        var emitter = new Subject<string[]>();
        using var sut = new LogViewModel("", emitter.AsObservable(), false);
        emitter.OnNext(["A", "B", "C"]);
        await Task.Delay(OperationDelay);


        emitter.OnNext(["A", "B", "C", "D", "E"]);
        await Task.Delay(OperationDelay);
        sut.AllLines.Take(2).All(vm => vm.JustAppend).ShouldBeTrue();
    }
}