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
using SpeleoLogViewer._BaseClass;
using SpeleoLogViewer.ApplicationState;
using SpeleoLogViewer.FileChanged;
using SpeleoLogViewer.LogFileViewer;
using SpeleoLogViewer.LogFileViewer.Dockable;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleoLogViewer.Main;

public sealed partial class MainWindowViewModel : ViewModelBase, IDropTarget, IDisposable
{
    private readonly CompositeDisposable _disposables = [];

    private readonly IStorageProvider _storageProvider;
    private readonly ITextFileLoader _textFileLoader;
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly ISpeleologTemplateReader _templateReader;
    private readonly DockFactory _factory;
    private readonly FileChangedObservableFactory _fileChangedObservableFactory;

    [ObservableProperty] private IRootDock? _layout;

    private readonly List<string> _openFiles = [];
    public bool AppendFromBottom { get; private set; }

    public IEnumerable<string> OpenFiles => _openFiles.AsEnumerable();

    public MainWindowViewModel(IStorageProvider storageProvider,
        ITextFileLoader textFileLoader,
        Func<string, IFileSystemChangedWatcher> fileSystemObserverFactory,
        ISpeleologStateRepository speleologStateRepository,
        ISchedulerProvider schedulerProvider,
        ISpeleologTemplateReader templateReader)
    {
        _storageProvider = storageProvider;
        _textFileLoader = textFileLoader;
        _schedulerProvider = schedulerProvider;
        _templateReader = templateReader;
        _factory = new DockFactory();
        Layout = _factory.CreateLayout();
        if (Layout is not null) _factory.InitLayout(Layout);
        _factory.DockableClosed += (_, args) =>
        {
            if (args.Dockable is DockableLogFileVM logViewModel)
                _openFiles.Remove(logViewModel.LogFileViewerVM.FilePath);
        };

        _fileChangedObservableFactory = new FileChangedObservableFactory(fileSystemObserverFactory);

        // Load state from last application use
        Observable
            .FromAsync(speleologStateRepository.GetAsync, _schedulerProvider.Default)
            .WhereNotNull()
            .ObserveOn(SynchronizationContext.Current ?? throw new InvalidOperationException())
            .Do(state =>
            {
                AppendFromBottom = state.AppendFromBottom;

                foreach (var filePath in state.LastOpenFiles)
                {
                    try
                    {
                        AddToDock(CreateLogViewModel(filePath));
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

    public void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var result = e.Data.GetFiles();
        e.Handled = true;
        if (result is null) return;

        _ = OpenFilesFromPath(result.Where(item => item is IStorageFile).Cast<IStorageFile>(), default);
    }

    [RelayCommand]
    private async Task OpenFile(CancellationToken token)
    {
        var files = await DoOpenFilePickerAsync();
        await OpenFilesFromPath(files, token);
    }

    public void Dispose() => _disposables.Dispose();

    private async Task OpenFilesFromPath(IEnumerable<IStorageFile> files, CancellationToken token)
    {
        ErrorMessages?.Clear();
        try
        {
            foreach (var givenFilePath in files.Select(file => file.Path.LocalPath))
            {
                var filePaths = await GetAllFilePathAsync(givenFilePath, token);
                foreach (var filePath in filePaths)
                    AddToDock(CreateLogViewModel(filePath));
            }
        }
        catch (Exception e)
        {
            ErrorMessages?.Add(e.Message);
        }
    }

    private async Task<IReadOnlyList<string>> GetAllFilePathAsync(string givenFilePath, CancellationToken cancellationToken)
    {
        if (!_templateReader.IsTemplateFile(givenFilePath))
            return [givenFilePath];

        var template = await _templateReader.ReadAsync(givenFilePath, cancellationToken);
        return template == null ? [] : template.Files;
    }

    private Task<IReadOnlyList<IStorageFile>> DoOpenFilePickerAsync() =>
        _storageProvider
            .OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open AllLines File",
                AllowMultiple = true
            });

    private DockableLogFileVM CreateLogViewModel(string path)
    {
        _openFiles.Add(path);
        return new DockableLogFileVM(new LogFileViewerVM(
            filePath: path,
            fileChangedStream: _fileChangedObservableFactory.BuildFileChangedObservable(path, _schedulerProvider.Default),
            _textFileLoader, 100,
            RxApp.TaskpoolScheduler));
    }

    private void AddToDock(IDockable logViewModel)
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
            dock.Close.Execute(null);
    }

    public void DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files)) return;
        e.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }
}