using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Platform.Storage;
using Dock.Model.ReactiveUI.Controls;
using NSubstitute;
using Shouldly;
using SpeleoLogViewer.ApplicationState;
using SpeleoLogViewer.FileChanged;
using SpeleoLogViewer.Main;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleologTest;

public class MainViewModelShould
{
    [Fact]
    public void AddFilePathToOpenFilesOnOpenFileCommand()
    {
        var stateProvider = Substitute.For<ISpeleologStateRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:/test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult((IReadOnlyList<IStorageFile>)new[] { file }.AsReadOnly()));
        using var sut = new MainWindowVM(
            storageProvider, 
            new EmptyFileLoader(), 
            _ => Substitute.For<IFileSystemChangedWatcher>(), 
            stateProvider, 
            new SchedulerProvider(), 
            Substitute.For<ISpeleologTemplateReader>());

        sut.OpenFileCommand.Execute().Subscribe();

        sut.OpenFiles.Count().ShouldBe(1);
        sut.CloseLayout();
    }

    [Fact]
    public void RemoveFilePathFromOpenFilesOnCloseDocument()
    {
        var stateProvider = Substitute.For<ISpeleologStateRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:/test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult((IReadOnlyList<IStorageFile>)new[] { file }.AsReadOnly()));
        using var sut = new MainWindowVM(
            storageProvider,
            new EmptyFileLoader(),
            _ => Substitute.For<IFileSystemChangedWatcher>(),
            stateProvider,
            new SchedulerProvider(),
            Substitute.For<ISpeleologTemplateReader>());
        sut.OpenFileCommand.Execute().Subscribe();
        var documentDock = ((DocumentDock)sut.Layout!.ActiveDockable!).ActiveDockable!;

        sut.Layout!.Factory!.CloseDockable(documentDock);

        sut.OpenFiles.Count().ShouldBe(0);
        sut.CloseLayout();
    }
    
    [Fact]
    public async Task OpenTemplateFileOnOpenFileCommand() // OpenFilesProvidedByTemplateFileOnOpenFileCommand
    {
        var stateProvider = Substitute.For<ISpeleologStateRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var templateFile = Substitute.For<IStorageFile>();
        templateFile.Path.Returns(new Uri($"file://{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/MyTemplate.speleolog"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult((IReadOnlyList<IStorageFile>)new[] { templateFile }.AsReadOnly()));
        using var sut = new MainWindowVM(
            storageProvider, 
            new EmptyFileLoader(), 
            _ => Substitute.For<IFileSystemChangedWatcher>(), 
            stateProvider, 
            new SchedulerProvider(), 
            new SpeleologTemplateReader());

        await sut.OpenFileCommand.Execute();
        sut.OpenFiles.Count().ShouldBe(1);
        sut.CloseLayout();
    }
}