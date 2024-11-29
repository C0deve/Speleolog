using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Platform.Storage;
using Dock.Model.ReactiveUI.Controls;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Shouldly;
using SpeleoLog._BaseClass;
using SpeleoLog.ApplicationState;
using SpeleoLog.Main;
using SpeleoLog.SpeleologTemplate;
using SpeleoLog.Viewer.Core;

namespace SpeleoLog.Test;

public class MainViewModelShould
{
    [Fact]
    public void AddFilePathToOpenFilesOnOpenFileCommand()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:/test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult((IReadOnlyList<IStorageFile>)new[] { file }.AsReadOnly()));
        using var sut = new MainWindowVM(
            storageProvider, 
            Substitute.For<ILauncher>(), 
            _ => Substitute.For<IFileSystemWatcher>(),
            SpeleologState.Default, 
            new SchedulerProvider(), 
            Substitute.For<ISpeleologTemplateRepositoryV2>(),
            () => new FileLoaderForTest("log content"));

        sut.OpenFileCommand.Execute().Subscribe();

        sut.State.LastOpenFiles.Count.ShouldBe(1);
        sut.CloseLayout();
    }

    [Fact]
    public void RemoveFilePathFromOpenFilesOnCloseDocument()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:/test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult((IReadOnlyList<IStorageFile>)new[] { file }.AsReadOnly()));
        using var sut = new MainWindowVM(
            storageProvider, 
            Substitute.For<ILauncher>(), 
            _ => Substitute.For<IFileSystemWatcher>(),
            SpeleologState.Default,
            new SchedulerProvider(),
            Substitute.For<ISpeleologTemplateRepositoryV2>(),
            () => new FileLoaderForTest("log content"));
        sut.OpenFileCommand.Execute().Subscribe();
        var documentDock = ((DocumentDock)sut.Layout.ActiveDockable!).ActiveDockable!;

        sut.Layout.Factory!.CloseDockable(documentDock);

        sut.State.LastOpenFiles.Count.ShouldBe(0);
        sut.CloseLayout();
    }
    
    [Fact]
    public async Task OpenTemplateFileOnOpenFileCommand() // OpenFilesProvidedByTemplateFileOnOpenFileCommand
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        var templateFile = Substitute.For<IStorageFile>();
        templateFile.Path.Returns(new Uri($"file://{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/MyTemplate.speleolog"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult<IReadOnlyList<IStorageFile>>(new[] { templateFile }.AsReadOnly()));
        using var sut = new MainWindowVM(
            storageProvider,
            Substitute.For<ILauncher>(),
            _ => Substitute.For<IFileSystemWatcher>(),
            SpeleologState.Default, 
            new SchedulerProvider(), 
            new SpeleologTemplateRepositoryV2(),
            () => new FileLoaderForTest("log content"));

        await sut.OpenFileCommand.Execute();
        sut.State.LastOpenFiles.Count.ShouldBe(1);
        sut.CloseLayout();
    }
    
    [Fact]
    public void PreventOpeningTheSameFileTwice()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:/test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult<IReadOnlyList<IStorageFile>>(new[] { file }.AsReadOnly()));
        using var sut = new MainWindowVM(
            storageProvider,
            Substitute.For<ILauncher>(),
            _ => Substitute.For<IFileSystemWatcher>(),
            SpeleologState.Default, 
            new SchedulerProvider(), 
            Substitute.For<ISpeleologTemplateRepositoryV2>(),
            () => new FileLoaderForTest("log content"));

        sut.OpenFileCommand.Execute().Subscribe();
        sut.OpenFileCommand.Execute().Subscribe();  

        sut.State.LastOpenFiles.Count.ShouldBe(1);
        sut.CloseLayout();
    }
    
    [Fact]
    public void ReturnCurrentState()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:\\test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult<IReadOnlyList<IStorageFile>>(new[] { file }.AsReadOnly()));
        var scheduler = new TestScheduler();
        
        using var sut = new MainWindowVM(
            storageProvider, 
            Substitute.For<ILauncher>(),
            _ => Substitute.For<IFileSystemWatcher>(),
            new SpeleologState(["c:\\test.txt"], false, "Folder"), 
            new TestSchedulerProvider(scheduler), 
            Substitute.For<ISpeleologTemplateRepositoryV2>(),
            () => new FileLoaderForTest("log content"));
        
        scheduler.AdvanceBy(10000);
        sut.State.ShouldSatisfyAllConditions(x =>
        {
            x.LastOpenFiles.ShouldBe(["c:\\test.txt"]);
            x.TemplateFolder.ShouldBe("Folder");
            x.AppendFromBottom.ShouldBe(false);
        });
        sut.CloseLayout();
    }
}