using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using Dock.Model.Core;
using ReactiveUI;
using SpeleoLogViewer.Models;
using SpeleoLogViewer.Service;

namespace SpeleoLogViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDropTarget
{
    private readonly NotepadFactory _factory;

    [ObservableProperty] private IRootDock? _layout;
    [ObservableProperty] private string? _fileText;
    private readonly List<string> _openFiles = [];

    public IEnumerable<string> OpenFiles => _openFiles.AsEnumerable();

    public MainWindowViewModel()
    {
        _factory = new NotepadFactory();
        Layout = _factory.CreateLayout();
        if (Layout is not null) _factory.InitLayout(Layout);

        Observable
            .FromAsync(SpeleologStateRepository.GetAsync)
            .WhereNotNull()
            .ObserveOn(SynchronizationContext.Current ?? throw new InvalidOperationException())
            .Do(state =>
            {
                foreach (var filePath in state.LastOpenFiles)
                {
                    try
                    {
                        AddFileViewModel(OpenFileViewModel(filePath));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            })
            .Subscribe();
    }


    [RelayCommand]
    private async Task OpenFile(CancellationToken token)
    {
        ErrorMessages?.Clear();
        try
        {
            var file = await DoOpenFilePickerAsync();
            if (file is null) return;

            AddFileViewModel(OpenFileViewModel(file.Path.LocalPath));
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
            Title = "Open AllLines File",
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }

    private static IStorageProvider GetFileProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        return provider;
    }

    private LogViewModel OpenFileViewModel(string path)
    {
        _openFiles.Add(path);
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Impossible de trouver le repertoir du fichier {path}");
        return new LogViewModel(
            path,
            FileSystemObserver.ObserveFolder(directory, FileSystemObserver.FileSystemWatcherFactory),
            File.ReadAllLinesAsync
        );
    }

    private void AddFileViewModel(LogViewModel logViewModel)
    {
        var files = _factory.GetDockable<IDocumentDock>("Files");
        if (Layout is null || files is null) return;

        _factory.AddDockable(files, logViewModel);
        _factory.SetActiveDockable(logViewModel);
        _factory.SetFocusedDockable(Layout, logViewModel);
    }

    public void CloseLayout()
    {
        if (Layout is not IDock dock) return;
        if (dock.Close.CanExecute(null))
        {
            dock.Close.Execute(null);
        }
    }

    public void DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files)) return;
        e.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }

    public void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var result = e.Data.GetFiles();
        if (result is not null)
        {
            foreach (var item in result)
            {
                if (item is not IStorageFile storageFile) continue;
                var openFileViewModel = OpenFileViewModel(storageFile.Path.LocalPath);
                AddFileViewModel(openFileViewModel);
            }
        }

        e.Handled = true;
    }
}