using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpeleoLogViewer.Service;

namespace SpeleoLogViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string? _fileText;

    [RelayCommand]
    private async Task OpenFile(CancellationToken token)
    {
        ErrorMessages?.Clear();
        try
        {
            var file = await DoOpenFilePickerAsync();
            if (file is null) return;

            FileSystemObserver
                .Observe(file.Path.AbsolutePath)
                .SelectMany(fileSystemEventArgs => GetFileProvider().TryGetFileFromPathAsync(fileSystemEventArgs.FullPath))
                .StartWith(file)
                .Where(storageFile => storageFile is not null)
                .Select(storageFile => storageFile!)
                .SelectMany(storageFile => Observable.FromAsync(async () =>
                {
                    try
                    {
                        // Limit the text file to 1MB so that the demo won't lag.
                        if ((await storageFile.GetBasicPropertiesAsync()).Size <= 1024 * 1024 * 1)
                        {
                            await using var readStream = await file.OpenReadAsync();
                            using var reader = new StreamReader(readStream);
                            FileText = await reader.ReadToEndAsync(token);
                        }
                        else
                        {
                            throw new Exception("File exceeded 1MB limit.");
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorMessages?.Add(e.Message);
                    }
                }))
                .Subscribe();
        }
        catch (Exception e)
        {
            ErrorMessages?.Add(e.Message);
        }
    }

    private static async Task<IStorageFile?> DoOpenFilePickerAsync()
    {
        // For learning purposes, we opted to directly get the reference
        // for StorageProvider APIs here inside the ViewModel. 

        // For your real-world apps, you should follow the MVVM principles
        // by making service classes and locating them with DI/IoC.

        // See IoCFileOps project for an example of how to accomplish this.
        var provider = GetFileProvider();

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false
        });

        return files?.Count >= 1 ? files[0] : null;
    }

    private static IStorageProvider GetFileProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");
        
        return provider;
    }
}