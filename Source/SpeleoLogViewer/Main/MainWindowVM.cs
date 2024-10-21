using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using SpeleoLogViewer._BaseClass;
using SpeleoLogViewer.ApplicationState;
using SpeleoLogViewer.FileChanged;
using SpeleoLogViewer.LogFileViewer.V2;
using SpeleoLogViewer.SpeleologTemplate;

namespace SpeleoLogViewer.Main;

public sealed class MainWindowVM : ReactiveObject, IDropTarget, IDisposable
{
    private readonly CompositeDisposable _disposables = [];

    private readonly IStorageProvider _storageProvider;
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly ISpeleologTemplateRepositoryV2 _templateRepository;
    private readonly DockFactory _factory;
    private readonly FileChangedObservableFactory _fileChangedObservableFactory;
    private readonly Func<ITextFileLoaderV2> _fileReaderFactory;

    public IRootDock Layout { get; }
    public ObservableCollection<string> ErrorMessages { get; } = [];
    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
    public ReactiveCommand<Unit, bool> OpenTemplateFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateTemplateCommand { get; }
    private ReactiveCommand<string, IReadOnlyList<TemplateInfos>> ReadTemplateFolder { get; }

    private readonly ObservableAsPropertyHelper<IReadOnlyList<TemplateInfos>> _templateInfosList;
    public IReadOnlyList<TemplateInfos> TemplateInfosList => _templateInfosList.Value;
    public SpeleologState State { get; }

    public MainWindowVM(IStorageProvider storageProvider,
        ILauncher launcher,
        Func<string, IFileSystemChangedWatcher> fileSystemObserverFactory,
        SpeleologState state,
        ISchedulerProvider schedulerProvider,
        ISpeleologTemplateRepositoryV2 templateRepository,
        Func<ITextFileLoaderV2> fileReaderFactory)
    {
        _storageProvider = storageProvider;
        _schedulerProvider = schedulerProvider;
        _templateRepository = templateRepository;
        _fileReaderFactory = fileReaderFactory;
        _factory = new DockFactory();
        State = state with { LastOpenFiles = [] };
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout);
        _factory.DockableClosed += (_, args) =>
        {
            if (args.Dockable is not LogFileViewerV2VM logViewModel) return;

            State.LastOpenFiles.Remove(logViewModel.FilePath);
            logViewModel.Dispose();
        };

        _fileChangedObservableFactory = new FileChangedObservableFactory(fileSystemObserverFactory);

        CreateTemplateCommand = ReactiveCommand.CreateFromTask(() =>
            _templateRepository.SaveAsync(
                State.TemplateFolder,
                new SpeleologTemplate.SpeleologTemplate("_New_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"),
                    State.LastOpenFiles.ToArray())));
        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFile);
        OpenTemplateFolderCommand = ReactiveCommand.CreateFromTask(() => launcher.LaunchDirectoryInfoAsync(Directory.CreateDirectory(State.TemplateFolder)));
        ReadTemplateFolder = ReactiveCommand.Create<string, IReadOnlyList<TemplateInfos>>(_templateRepository.ReadAll);
        _templateInfosList = ReadTemplateFolder
            .ToProperty(this, nameof(TemplateInfosList))
            .DisposeWith(_disposables);

        this.WhenAnyValue(vm => vm.TemplateInfosList)
            .IsNotNull()
            .SelectMany(x => x.Select(infos => infos.Open))
            .Merge()
            .SelectAsync((templateInfos, token) => OpenFilesFromPathAsync([templateInfos.Path], token))
            .Subscribe()
            .DisposeWith(_disposables);

        Observable
            .Return(State.TemplateFolder)
            .InvokeCommand(ReadTemplateFolder)
            .DisposeWith(_disposables);

        Observable.Return(state.LastOpenFiles)
            .SelectAsync(OpenFilesFromPathAsync)
            .Subscribe()
            .DisposeWith(_disposables);

        CreateTemplateCommand
            .Select(_ => State.TemplateFolder)
            .InvokeCommand(ReadTemplateFolder)
            .DisposeWith(_disposables);
        
        CreateTemplateCommand
            .InvokeCommand(OpenTemplateFolderCommand)
            .DisposeWith(_disposables);
    }

    public void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var result = e.Data.GetFiles();
        e.Handled = true;
        if (result is null) return;

        _ = OpenFilesFromPathAsync(result
            .Where(item => item is IStorageFile)
            .Cast<IStorageFile>()
            .Select(file => file.Path.LocalPath));
    }

    private async Task OpenFile(CancellationToken token)
    {
        var files = await DoOpenFilePickerAsync();
        await OpenFilesFromPathAsync(files.Select(file => file.Path.LocalPath), token);
    }

    public void Dispose() => _disposables.Dispose();

    private async Task OpenFilesFromPathAsync(IEnumerable<string> filesPath, CancellationToken token = default)
    {
        ErrorMessages.Clear();
        try
        {
            foreach (var givenFilePath in filesPath)
            {
                if (_templateRepository.IsTemplateFile(givenFilePath))
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

    private async Task<string[]> GetFilesFromTemplate(string givenFilePath, CancellationToken token) =>
        (await _templateRepository.ReadAsync(givenFilePath, token))?.Files ?? [];

    private Task<IReadOnlyList<IStorageFile>> DoOpenFilePickerAsync() =>
        _storageProvider
            .OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select log file(s)",
                AllowMultiple = true
            });

    private void CreateAndDock(string filePath)
    {
        if (State.LastOpenFiles.Any(s => s == filePath)) return;

        State.LastOpenFiles.Add(filePath);
        AddToDock(CreateLogViewModel(filePath));
    }

    private LogFileViewerV2VM CreateLogViewModel(string path) =>
        new(
            filePath: path,
            fileChangedStream: _fileChangedObservableFactory.BuildFileChangedObservable(path, _schedulerProvider.TaskpoolScheduler),
            _fileReaderFactory(),
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
}