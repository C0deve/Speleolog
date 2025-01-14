using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using SpeleoLog.Viewer;

namespace SpeleoLog.Main;

public sealed class MainWindowVM : ReactiveObject, IDropTarget, IDisposable
{
    private readonly CompositeDisposable _disposables = [];

    private readonly IStorageProvider _storageProvider;
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly ISpeleologTemplateRepositoryV2 _templateRepository;
    private readonly DockFactory _dockFactory;
    private readonly FileChangesObservableFactory _fileChangesObservableFactory;
    private readonly Func<IFileLoader> _fileReaderFactory;

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
        Func<string, IFileSystemWatcher> fileSystemObserverFactory,
        SpeleologState state,
        ISchedulerProvider schedulerProvider,
        ISpeleologTemplateRepositoryV2 templateRepository,
        Func<IFileLoader> fileReaderFactory)
    {
        _storageProvider = storageProvider;
        _schedulerProvider = schedulerProvider;
        _templateRepository = templateRepository;
        _fileReaderFactory = fileReaderFactory;
        _dockFactory = new DockFactory();
        State = state with { LastOpenFiles = [] };
        Layout = _dockFactory.CreateLayout();
        _dockFactory.InitLayout(Layout);
        _dockFactory.DockableClosed += (_, args) =>
        {
            if (args.Dockable is not ViewerVM logViewModel) return;

            State.LastOpenFiles.Remove(logViewModel.FilePath);
            logViewModel.Dispose();
        };

        _fileChangesObservableFactory = new FileChangesObservableFactory(fileSystemObserverFactory);

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
        
        SetupCommand(CreateTemplateCommand, OpenTemplateFolderCommand, ReadTemplateFolder, OpenFileCommand);
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
        AddToDock(CreateViewerVM(filePath));
    }

    private ViewerVM CreateViewerVM(string path) =>
        new(
            filePath: path,
            fileChangedStream: _fileChangesObservableFactory.Build(path, _schedulerProvider.TaskpoolScheduler),
            _fileReaderFactory(),
            "error",
            _schedulerProvider.TaskpoolScheduler);


    private void AddToDock(IDockable logViewModel)
    {
        var files = _dockFactory.GetDockable<IDocumentDock>("Files");
        if (files is null) return;

        _dockFactory.AddDockable(files, logViewModel);
        _dockFactory.SetActiveDockable(logViewModel);
        _dockFactory.SetFocusedDockable(Layout, logViewModel);
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
    
    private void SetupCommand(params IReactiveCommand[] commands) =>
        commands
            .Select(command => command.ThrownExceptions)
            .Merge()
            .Do(exception => ErrorMessages.Add(exception.Message))
            .Subscribe()
            .DisposeWith(_disposables);
}