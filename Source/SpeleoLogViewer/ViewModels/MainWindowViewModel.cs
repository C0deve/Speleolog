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

namespace SpeleoLogViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDropTarget
{
    private readonly NotepadFactory _factory;
    private readonly FileObserverProvider _fileObserverProvider;

    [ObservableProperty] private IRootDock? _layout;
    [ObservableProperty] private string? _fileText;
    private readonly List<string> _openFiles = [];

    public IEnumerable<string> OpenFiles => _openFiles.AsEnumerable();

    public MainWindowViewModel()
    {
        _factory = new NotepadFactory();
        Layout = _factory.CreateLayout();
        if (Layout is not null) _factory.InitLayout(Layout);

        _fileObserverProvider = new FileObserverProvider();

        // Load state from last application use
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
            var files = await DoOpenFilePickerAsync();
            foreach (var file in files)
                AddFileViewModel(OpenFileViewModel(file.Path.LocalPath));
        }
        catch (Exception e)
        {
            ErrorMessages?.Add(e.Message);
        }
    }

    private static Task<IReadOnlyList<IStorageFile>> DoOpenFilePickerAsync() =>
        GetFileProvider()
            .OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open AllLines File",
                AllowMultiple = true
            });

    private static IStorageProvider GetFileProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        return provider;
    }

    private LogViewModel? OpenFileViewModel(string path)
    {
        if (!Path.Exists(path))
        {
            Console.WriteLine($"Le fichier {path} n''existe pas.");
            return null;
        }

        _openFiles.Add(path);

        return new LogViewModel(
            path,
            _fileObserverProvider.GetObservable(path),
            File.ReadAllLinesAsync
        );
    }

    private void AddFileViewModel(LogViewModel? logViewModel)
    {
        if (logViewModel is null) return;

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