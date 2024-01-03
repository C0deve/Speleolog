using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shouldly;
using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class LogViewModelShould
{
    [Fact]
    public void LoadAllLinesAfterInstantiation()
    {
        string[] lines = ["A", "B", "C"];

        var emitter = new Subject<FileSystemEventArgs>();
        using var sut = new LogViewModel("", emitter.AsObservable(), GetTextAsync());

        sut.AllLines.Count.ShouldBe(lines.Length);
        return;

        Func<string, CancellationToken, Task<string[]>> GetTextAsync() =>
            (_, _) => Task.FromResult(lines);
    }

    [Fact]
    public void LoadAllLinesInReverse()
    {
        string[] lines = ["A", "B", "C"];
        var emitter = new Subject<FileSystemEventArgs>();
        using var sut = new LogViewModel("", emitter.AsObservable(), GetTextAsync());

        sut.AllLines.ShouldBe(lines.Select(s => new LogLineViewModel(s)).Reverse());
        return;

        Func<string, CancellationToken, Task<string[]>> GetTextAsync() =>
            (_, _) => Task.FromResult(lines);
    }

    [Fact]
    public async Task AppendLinesOnFileChanged()
    {
        var nbCall = 0;
        string[] lines = ["A", "B", "C"];
        string[] linesEvolution = ["A", "B", "C", "D", "E"];
        var emitter = new Subject<FileSystemEventArgs>();
        using var sut = new LogViewModel(@"path\log.txt", emitter.AsObservable(), GetTextAsync);

        await Task.Delay(TimeSpan.FromMilliseconds(10));
        sut.AllLines.ShouldBe(lines.Reverse().Select(s => new LogLineViewModel(s)));
        
        emitter.OnNext(new FileSystemEventArgs(WatcherChangeTypes.Changed, "path", "log.txt"));

        await Task.Delay(TimeSpan.FromMilliseconds(10));
        sut.AllLines.ShouldBe(linesEvolution.Reverse().Select(s => new LogLineViewModel(s)));
        return;

        Task<string[]> GetTextAsync(string path, CancellationToken token)
        {
            // ReSharper disable once InvertIf
            if (nbCall == 0)
            {
                nbCall++;
                return Task.FromResult(lines);
            }
            
            return Task.FromResult(linesEvolution);
        }
    }
    
    [Fact]
    public void AppendLinesOnlyIfFileNameMatch()
    {
        var nbCall = 0;
        string[] lines = ["A", "B", "C"];
        string[] linesEvolution = ["A", "B", "C", "D", "E"];
        var emitter = new Subject<FileSystemEventArgs>();
        using var sut = new LogViewModel(@"path\log.txt", emitter.AsObservable(), GetTextAsync);

        emitter.OnNext(new FileSystemEventArgs(WatcherChangeTypes.Changed, "path", "log2.txt"));

        sut.AllLines.ShouldBe(lines.Select(s => new LogLineViewModel(s)).Reverse());
        return;

        Task<string[]> GetTextAsync(string path, CancellationToken token)
        {
            // ReSharper disable once InvertIf
            if (nbCall == 0)
            {
                nbCall++;
                return Task.FromResult(lines);
            }
            
            return Task.FromResult(linesEvolution);
        }
    }
}