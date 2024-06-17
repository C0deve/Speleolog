using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Dock.Model.Controls;
using Dock.Model.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SpeleoLogViewer._BaseClass;
using SpeleoLogViewer.ApplicationState;
using SpeleoLogViewer.FileChanged;
using SpeleoLogViewer.LogFileViewer;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleoLogViewer.Main;

public sealed class MainWindowVM : ReactiveObject, IDropTarget, IDisposable
{
    private readonly CompositeDisposable _disposables = [];

    private readonly IStorageProvider _storageProvider;
    private readonly ITextFileLoader _textFileLoader;
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly ISpeleologTemplateReader _templateReader;
    private readonly DockFactory _factory;
    private readonly FileChangedObservableFactory _fileChangedObservableFactory;
    

    public IRootDock Layout { get; }
    public ObservableCollection<string> ErrorMessages { get; } = [];
    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
    private ReactiveCommand<Unit, SpeleologState?> OpenStateCommand { get; }
    private ReactiveCommand<string, IReadOnlyList<TemplateInfos>> ReadTemplateFolder { get; }
    [Reactive] public TemplateInfos? CurrentTemplate { get; set; }

    private readonly ObservableAsPropertyHelper<IReadOnlyList<TemplateInfos>> _templateInfosList;
    public IReadOnlyList<TemplateInfos> TemplateInfosList => _templateInfosList.Value;

    private readonly ObservableAsPropertyHelper<SpeleologState> _state;
    public SpeleologState State => _state.Value;
    
    public MainWindowVM(IStorageProvider storageProvider,
        ITextFileLoader textFileLoader,
        Func<string, IFileSystemChangedWatcher> fileSystemObserverFactory,
        ISpeleologStateRepository speleologStateRepository,
        ISchedulerProvider schedulerProvider,
        ISpeleologTemplateReader templateReader,
        FolderTemplateReader folderTemplateReader)
    {
        _storageProvider = storageProvider;
        _textFileLoader = textFileLoader;
        _schedulerProvider = schedulerProvider;
        _templateReader = templateReader;
        _factory = new DockFactory();
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout);
        _factory.DockableClosed += (_, args) =>
        {
            if (args.Dockable is LogFileViewerVM logViewModel)
                State.LastOpenFiles.Remove(logViewModel.FilePath);
        };

        _fileChangedObservableFactory = new FileChangedObservableFactory(fileSystemObserverFactory);

        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFile);
        
        ReadTemplateFolder = ReactiveCommand.Create<string, IReadOnlyList<TemplateInfos>>(folderTemplateReader.Read);
        _templateInfosList = ReadTemplateFolder
            .ToProperty(this, nameof(TemplateInfosList))
            .DisposeWith(_disposables);

        OpenStateCommand = ReactiveCommand.CreateFromTask(speleologStateRepository.GetAsync);
        OpenStateCommand
            .IsNotNull()
            .Select(state => state.TemplateFolder)
            .IsNotEmpty()
            .InvokeCommand(ReadTemplateFolder)
            .DisposeWith(_disposables);

        OpenStateCommand
            .IsNotNull()
            .SelectMany(state => state.LastOpenFiles)
            .Do(CreateAndDock)
            .Subscribe()
            .DisposeWith(_disposables);

        _state = OpenStateCommand
            .IsNotNull()
            .ToProperty(this, nameof(State))
            .DisposeWith(_disposables);
        
        OpenStateCommand
            .Execute()
            .Subscribe()
            .DisposeWith(_disposables);

        this.WhenAnyValue(vm => vm.CurrentTemplate)
            .WhereNotNull()
            .Select(infos => infos.Path)
            .Do(CreateAndDock)
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

    private async Task OpenFile(CancellationToken token)
    {
        var files = await DoOpenFilePickerAsync();
        await OpenFilesFromPath(files, token);
    }

    public void Dispose() => _disposables.Dispose();

    private async Task OpenFilesFromPath(IEnumerable<IStorageFile> files, CancellationToken token)
    {
        ErrorMessages.Clear();
        try
        {
            foreach (var givenFilePath in files.Select(file => file.Path.LocalPath))
            {
                if (_templateReader.IsTemplateFile(givenFilePath))
                    foreach (var pathFromTemplate in await GetFilesFromTemplate(givenFilePath, token))
                        CreateAndDock(pathFromTemplate);
                else
                    CreateAndDock(givenFilePath);
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add(e.Message);
        }
    }

    private async Task<List<string>> GetFilesFromTemplate(string givenFilePath, CancellationToken token) =>
        (await _templateReader.ReadAsync(givenFilePath, token))?.Files ?? [];

    private Task<IReadOnlyList<IStorageFile>> DoOpenFilePickerAsync() =>
        _storageProvider
            .OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open AllLines File",
                AllowMultiple = true
            });

    private void CreateAndDock(string filePath)
    {
        if (State.LastOpenFiles.Any(s => s == filePath)) return;

        State.LastOpenFiles.Add(filePath);
        AddToDock(CreateLogViewModel(filePath));
    }

    private LogFileViewerVM CreateLogViewModel(string path) =>
        new(
            filePath: path,
            fileChangedStream: _fileChangedObservableFactory.BuildFileChangedObservable(path, _schedulerProvider.TaskpoolScheduler),
            _textFileLoader,
            100,
            "error",
            RxApp.TaskpoolScheduler);

    private void AddToDock(IDockable logViewModel)
    {
        var files = _factory.GetDockable<IDocumentDock>("Files");
        if (files is null) return;

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

    public SpeleologState GetState() => State;
}