namespace BankParser.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private bool _isBackEnabled;
    private object? _selected;
    private ICommand? _menuFileExitCommand;
    private ICommand? _menuViewsMainCommand;

    public ICommand MenuFileExitCommand => _menuFileExitCommand ??= new RelayCommand(OnMenuFileExit);

    public ICommand MenuViewsMainCommand => _menuViewsMainCommand ??= new RelayCommand(OnMenuViewsMain);

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task Search(SfAutoComplete autoComplete, CancellationToken token) => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task MenuFileOpen(SfAutoComplete autoComplete, CancellationToken token) => MainViewModel.OpenFile(token);

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task MenuFileClose(SfAutoComplete autoComplete, CancellationToken token) => MainViewModel.CloseFile();

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task MenuViewsRules(SfAutoComplete autoComplete, CancellationToken token) => Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void MenuUndo() => MainViewModel.Undo();
    public bool CanUndo() => MainViewModel.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void MenuRedo() => MainViewModel.Redo();
    public bool CanRedo() => MainViewModel.CanRedo;

    public bool AllowMenuViewsClearFilter() => true;

    [RelayCommand(CanExecute=nameof(AllowMenuViewsClearFilter))]
    private void MenuViewsClearFilter() => App.GetService<MainViewModel>()?.ClearFilter();

    public INavigationService NavigationService
    {
        get;
    }

    public bool IsBackEnabled
    {
        get => _isBackEnabled;
        set => SetProperty(ref _isBackEnabled, value);
    }

    public object? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }
    public MainViewModel MainViewModel
    {
        get;
    }

    public ShellViewModel(MainViewModel mainViewModel, INavigationService navigationService)
    {
        MainViewModel = mainViewModel;
        MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
    }

    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MenuUndoCommand.NotifyCanExecuteChanged();
        MenuRedoCommand.NotifyCanExecuteChanged();
    }

    private void OnNavigated(object sender, NavigationEventArgs e) => IsBackEnabled = NavigationService.CanGoBack;

    private void OnMenuFileExit() => Application.Current.Exit();

    private void OnMenuViewsMain() => NavigationService.NavigateTo(typeof(MainViewModel).FullName ?? nameof(MainViewModel));
}
