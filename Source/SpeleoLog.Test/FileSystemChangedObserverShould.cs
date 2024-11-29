using Microsoft.Reactive.Testing;
using NSubstitute;
using Shouldly;
using SpeleoLog.Viewer.Core;
using FileSystemWatcher = SpeleoLog._Infrastructure.FileSystemWatcher;

namespace SpeleoLog.Test;

public class FileSystemChangedObserverShould
{
    private readonly TimeSpan _throttleDurationPlusOne = FileChangesObservableFactory.ThrottleDuration + TimeSpan.FromMilliseconds(1);

    [Fact]
    public async Task EmitOnFileChangedIntegration()
    {
        var actual = 0;
        var filePath = Utils.CreateUniqueEmptyFile();
        var factory = new FileChangesObservableFactory(directoryPath => new FileSystemWatcher(directoryPath));
        var disposable = factory
            .Build(filePath)
            .Subscribe(_ => actual++);
        
        await File.AppendAllLinesAsync(filePath, ["a"]);
        await Task.Delay(FileChangesObservableFactory.ThrottleDuration + TimeSpan.FromMilliseconds(100));

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
        var fileSystemObserver = Substitute.For<IFileSystemWatcher>();
        var factory = new FileChangesObservableFactory(_ => fileSystemObserver);
        var sut = factory.Build(file.Name, scheduler);
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
        var fileSystemObserver = Substitute.For<IFileSystemWatcher>();
        var fileContentObserverProvider = new FileChangesObservableFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.Build(file.Name, scheduler);
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
        var fileSystemObserver = Substitute.For<IFileSystemWatcher>();
        var fileContentObserverProvider = new FileChangesObservableFactory(_ => fileSystemObserver);
        var sut = fileContentObserverProvider.Build(file.Name, scheduler);
        sut.Subscribe(_ => actual++);

        for (var i = 0; i < 10; i++)
        {
            RaiseChangedEvent(fileSystemObserver, file.Name);
            scheduler.AdvanceBy(1);
        }
        scheduler.AdvanceBy(_throttleDurationPlusOne.Ticks);

        actual.ShouldBe(1); // 1 call 500ms after last event
    }

    private static void RaiseChangedEvent(IFileSystemWatcher fileSystemWatcher, string? filePath)
    {
        fileSystemWatcher.Changed += Raise.Event<FileSystemEventHandler>(
            fileSystemWatcher,
            new FileSystemEventArgs(WatcherChangeTypes.Changed, "", filePath));
    }

    private record FileTest
    {
        public string Name { get; } = $"{Guid.NewGuid()}.txt";
    }
}