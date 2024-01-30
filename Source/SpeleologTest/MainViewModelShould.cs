using Avalonia.Platform.Storage;
using NSubstitute;
using Shouldly;
using SpeleoLogViewer.Service;
using SpeleoLogViewer.ViewModels;

namespace SpeleologTest;

public class MainViewModelShould
{
    [Fact]
    public void ToReName()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        var file = Substitute.For<IStorageFile>();
        file.Path.Returns(new Uri("c:/test.txt"));
        storageProvider
            .OpenFilePickerAsync(Arg.Any<FilePickerOpenOptions>())
            .Returns( Task.FromResult((IReadOnlyList<IStorageFile>) new[] { file }.AsReadOnly()));
        
        using var sut = new MainWindowViewModel(storageProvider, (_, _) => Task.FromResult<string[]>([]), FileSystemObserverFactory());
        sut.OpenFileCommand.Execute(null);
        
        sut.OpenFiles.Count().ShouldBe(1);
        sut.CloseLayout();
        return;

        Func<string, IFileSystemObserver> FileSystemObserverFactory() => 
            _ => Substitute.For<IFileSystemObserver>();
    }
}