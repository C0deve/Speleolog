using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Shouldly;
using SpeleoLogViewer.Service;

namespace SpeleologTest;

public class FileSystemChangedObserverShould
{
    [Fact]
    public async Task EmitOnFileChangedIntegration()
    {
        var scheduler = new TestScheduler();
        var actual = Array.Empty<string>();
        var filePath = Utils.CreateUniqueEmptyFile();
        
        var fileContentObserverProvider = new FileSystemChangedObserverFactory(p => new FileSystemChangedWatcher(p));
        fileContentObserverProvider
            .GetObservable(filePath, File.ReadAllLinesAsync, scheduler)
            .Subscribe(strings => actual = strings);
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1).Ticks);
       
        await File.AppendAllTextAsync(filePath, "a" + Environment.NewLine);
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(501).Ticks);
        
        File.Delete(filePath);
        actual.ShouldBe(["a"]);
    }

    [Fact]
    public void EmitOnFileChanged()
    {
        var scheduler = new TestScheduler();
        var actual = Array.Empty<string>();
        var file = new FileSystemChangedObserverShould.FileTest(Array.Empty<string>());
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileSystemChangedObserverFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.GetObservable(file.Name, (_, _) => file.GetTextAsync(), scheduler);
        sut.Subscribe(strings => actual = strings);

        scheduler.Schedule(() =>
        {
            file
                .AddLine("A")
                .AddLine("B");

            RaiseChangedEvent(fileSystemObserver, file.Name);
        });
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(501).Ticks);

        actual.ShouldBe(["A", "B"]);
    }

    [Fact]
    public void NotEmitOnOtherFileChanged()
    {
        var scheduler = new TestScheduler();
        var actual = Array.Empty<string>();
        var file = new FileSystemChangedObserverShould.FileTest(Array.Empty<string>());
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileSystemChangedObserverFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.GetObservable(file.Name, (_, _) => file.GetTextAsync(), scheduler);
        sut.Subscribe(strings => actual = strings);

        scheduler.Schedule(() =>
        {
            file
                .AddLine("A")
                .AddLine("B");

            RaiseChangedEvent(fileSystemObserver, "otherFile.txt");
        });

        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(501).Ticks);

        actual.ShouldBe(Array.Empty<string>());
    }

    [Fact]
    public void ThrottleChangedEvent()
    {
        var actual = Array.Empty<string>();
        var file = new FileSystemChangedObserverShould.FileTest(Array.Empty<string>());
        var scheduler = new TestScheduler();
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileSystemChangedObserverFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.GetObservable(file.Name, (_, _) => file.GetTextAsync(), scheduler);
        sut.Subscribe(strings => actual = strings);

        for (var i = 0; i < 10; i++)
        {
            scheduler.Schedule(() =>
            {
                file.AddLine("A");
                RaiseChangedEvent(fileSystemObserver, file.Name);
            });
            scheduler.AdvanceBy(1);
        }

        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(501).Ticks);

        actual.ShouldBe(Enumerable.Range(0, 10).Select(_ => "A"));
        file.NbCall.ShouldBe(2); // initial call + 1 call 500ms after last event
    }

    private static void RaiseChangedEvent(IFileSystemChangedWatcher fileSystemChangedWatcher, string? filePath)
    {
        fileSystemChangedWatcher.Changed += Raise.Event<FileSystemEventHandler>(
            fileSystemChangedWatcher,
            new FileSystemEventArgs(WatcherChangeTypes.Changed, "", filePath));
    }

    private class FileTest(IEnumerable<string> lines)
    {
        private readonly List<string> _lines = lines.ToList();

        public int NbCall { get; private set; }
        public string Name { get; } = $"{Guid.NewGuid()}.txt";

        public FileSystemChangedObserverShould.FileTest AddLine(string line)
        {
            _lines.Add(line);
            return this;
        }

        public Task<string[]> GetTextAsync()
        {
            NbCall++;
            return Task.FromResult(_lines.ToArray());
        }
    }
}