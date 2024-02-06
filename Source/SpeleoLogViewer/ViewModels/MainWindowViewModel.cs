using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using Dock.Model.Core;
using ReactiveUI;
using SpeleoLogViewer.Models;
using SpeleoLogViewer.Service;

namespace SpeleoLogViewer.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IDropTarget, IDisposable
{
    private readonly CompositeDisposable _disposables = [];

    private readonly IStorageProvider _storageProvider;
    private readonly Func<string, CancellationToken, Task<string[]>> _getTextAsync;
    private readonly DockFactory _factory;
    private readonly FileContentObserverProvider _fileContentObserverProvider;

    [ObservableProperty] private IRootDock? _layout;
    [ObservableProperty] private string? _fileText;
    
    private readonly List<string> _openFiles = [];
    public bool AppendFromBottom { get; private set; }

    public IEnumerable<string> OpenFiles => _openFiles.AsEnumerable();

    public MainWindowViewModel(
        IStorageProvider storageProvider, 
        Func<string, CancellationToken, Task<string[]>> getTextAsync,
        Func<string, IFileSystemObserver> fileSystemObserverFactory)
    {
        _storageProvider = storageProvider;
        _getTextAsync = getTextAsync;
        _factory = new DockFactory();
        Layout = _factory.CreateLayout();
        if (Layout is not null) _factory.InitLayout(Layout);

        _fileContentObserverProvider = new FileContentObserverProvider(fileSystemObserverFactory);

        // Load state from last application use
        Observable
            .FromAsync(SpeleologStateRepository.GetAsync)
            .WhereNotNull()
            .ObserveOn(SynchronizationContext.Current ?? throw new InvalidOperationException())
            .Do(state =>
            {
                AppendFromBottom = state.AppendFromBottom;
                
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
            .Subscribe()
            .DisposeWith(_disposables);
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
    
    private Task<IReadOnlyList<IStorageFile>> DoOpenFilePickerAsync() =>
        _storageProvider
            .OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open AllLines File",
                AllowMultiple = true
            });

    private LogViewModel OpenFileViewModel(string path)
    {
        _openFiles.Add(path);

        return new LogViewModel(
            filePath: path,
            fileChangedStream: _fileContentObserverProvider.GetObservable(path, _getTextAsync),
            appendFromBottom: AppendFromBottom);
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
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}