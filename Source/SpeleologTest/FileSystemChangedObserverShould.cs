using Microsoft.Reactive.Testing;
using NSubstitute;
using Shouldly;
using SpeleoLogViewer.FileChanged;

namespace SpeleologTest;

public class FileSystemChangedObserverShould
{
    private readonly TimeSpan _throttleDurationPlusOne = FileChangedObservableFactory.ThrottleDuration + TimeSpan.FromMilliseconds(1);

    [Fact]
    public async Task EmitOnFileChangedIntegration()
    {
        var actual = 0;
        var filePath = Utils.CreateUniqueEmptyFile();
        var factory = new FileChangedObservableFactory(directoryPath => new FileSystemChangedWatcher(directoryPath));
        var disposable = factory
            .BuildFileChangedObservable(filePath)
            .Subscribe(_ => actual++);
        
        await File.AppendAllLinesAsync(filePath, ["a"]);
        await Task.Delay(FileChangedObservableFactory.ThrottleDuration + TimeSpan.FromMilliseconds(100));

        actual.ShouldBe(1);
        
        // Clean resources
        disposable.Dispose();
        File.Delete(filePath);
    }

    [Fact]
    public void EmitOnFileChanged()
    {
        var scheduler = new TestScheduler();
        var actual = 0;
        var file = new FileTest();
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var factory = new FileChangedObservableFactory(_ => fileSystemObserver);
        var sut = factory.BuildFileChangedObservable(file.Name, scheduler);
        sut.Subscribe(_ => actual++);
        
        RaiseChangedEvent(fileSystemObserver, file.Name);
        scheduler.AdvanceBy(_throttleDurationPlusOne.Ticks);

        actual.ShouldBe(1);
    }

    [Fact]
    public void NotEmitOnOtherFileChanged()
    {
        var scheduler = new TestScheduler();
        var actual = 0;
        var file = new FileTest();
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileChangedObservableFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.BuildFileChangedObservable(file.Name, scheduler);
        sut.Subscribe(_ => actual++);

        RaiseChangedEvent(fileSystemObserver, "otherFile.txt");
        scheduler.AdvanceBy(_throttleDurationPlusOne.Ticks);

        actual.ShouldBe(0);
    }

    [Fact]
    public void ThrottleChangedEvent()
    {
        var actual = 0;
        var file = new FileTest();
        var scheduler = new TestScheduler();
        var fileSystemObserver = Substitute.For<IFileSystemChangedWatcher>();
        var fileContentObserverProvider = new FileChangedObservableFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.BuildFileChangedObservable(file.Name, scheduler);
        sut.Subscribe(_ => actual++);

        for (var i = 0; i < 10; i++)
        {
            RaiseChangedEvent(fileSystemObserver, file.Name);
            scheduler.AdvanceBy(1);
        }
        scheduler.AdvanceBy(_throttleDurationPlusOne.Ticks);

        actual.ShouldBe(1); // 1 call 500ms after last event
    }

    private static void RaiseChangedEvent(IFileSystemChangedWatcher fileSystemChangedWatcher, string? filePath)
    {
        fileSystemChangedWatcher.Changed += Raise.Event<FileSystemEventHandler>(
            fileSystemChangedWatcher,
            new FileSystemEventArgs(WatcherChangeTypes.Changed, "", filePath));
    }

    private record FileTest
    {
        public string Name { get; } = $"{Guid.NewGuid()}.txt";
    }
}