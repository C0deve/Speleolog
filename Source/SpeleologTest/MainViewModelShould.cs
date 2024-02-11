using Avalonia.Platform.Storage;
using NSubstitute;
using Shouldly;
using SpeleoLogViewer.Models;
using SpeleoLogViewer.Service;
using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class MainViewModelShould
{
    [Fact]
    public void AddALoginViewModelOnOpenFileCommand()
    {
        var stateProvider = Substitute.For<ISpeleologStateRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:/test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns(Task.FromResult((IReadOnlyList<IStorageFile>)new[] { file }.AsReadOnly()));
        using var sut = new MainWindowViewModel(
            storageProvider,
            (_, _) => Task.FromResult<string[]>([]),
            FileSystemObserverFactory(), 
            stateProvider, new SchedulerProvider());
        
        sut.OpenFileCommand.Execute(null);

        sut.OpenFiles.Count().ShouldBe(1);
        sut.CloseLayout();
        return;

        Func<string, IFileSystemChangedWatcher> FileSystemObserverFactory() =>
            _ => Substitute.For<IFileSystemChangedWatcher>();
    }
}