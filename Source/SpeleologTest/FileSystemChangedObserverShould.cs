using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Shouldly;
using SpeleoLogViewer.Service;
using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class FileSystemChangedObserverShould
{
    [Fact]
    public async Task EmitOnFileChangedIntegration()
    {
        //var scheduler = new TestScheduler();
        var actual = Array.Empty<string>();
        var filePath = Utils.CreateUniqueEmptyFile();

        var fileContentObserverProvider = new FileSystemChangedObserverFactory(directoryPath => new FileSystemChangedWatcher(directoryPath));
        var disposable = fileContentObserverProvider
            .GetObservable(filePath, new TextFileLoaderInOneRead(), Scheduler.Default)
            .Subscribe(strings => actual = strings.ToArray());
        
        await File.AppendAllLinesAsync(filePath, ["a"]);
        await Task.Delay(FileSystemChangedObserverFactory.ThrottleDuration + TimeSpan.FromMilliseconds(1 + 50)); // 1 for throttle, 50 for file loading
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        disposable.Dispose();
        File.Delete(filePath);
        actual.ShouldBe(["a"]);

    }

    [Fact]
    public void EmitOnFileChanged()
    {
        var scheduler = new TestScheduler();
        var actual = Array.Empty<string>();
        var file = new FileTest(Array.Empty<string>());
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileSystemChangedObserverFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.GetObservable(file.Name, new TextFileLoaderForTest((_, _) => file.GetTextAsync()), scheduler);
        sut.Subscribe(strings => actual = strings.ToArray());

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
        var file = new FileTest(Array.Empty<string>());
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileSystemChangedObserverFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.GetObservable(file.Name, new TextFileLoaderForTest((_, _) => file.GetTextAsync()), scheduler);
        sut.Subscribe(strings => actual = strings.ToArray());

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
        var file = new FileTest(Array.Empty<string>());
        var scheduler = new TestScheduler();
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileSystemChangedObserverFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.GetObservable(file.Name, new TextFileLoaderForTest((_, _) => file.GetTextAsync()), scheduler);
        sut.Subscribe(strings => actual = strings.ToArray());

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

        public FileTest AddLine(string line)
        {
            _lines.Add(line);
            return this;
        }

        public Task<IEnumerable<string>> GetTextAsync()
        {
            NbCall++;
            return Task.FromResult(_lines.AsEnumerable());
        }
    }
}