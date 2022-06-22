using System.Windows.Input;

using BankParser.Contracts.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

using Syncfusion.UI.Xaml.Editors;

namespace BankParser.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private bool _isBackEnabled = false;
    private object? _selected;
    private ICommand? _menuFileExitCommand;
    private ICommand? _menuViewsMainCommand;

    public ICommand MenuFileExitCommand => _menuFileExitCommand ??= new RelayCommand(OnMenuFileExit);

    public ICommand MenuViewsMainCommand => _menuViewsMainCommand ??= new RelayCommand(OnMenuViewsMain);

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task Search(SfAutoComplete autoComplete, CancellationToken token) => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task MenuFileOpen(SfAutoComplete autoComplete, CancellationToken token) => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task MenuFileClose(SfAutoComplete autoComplete, CancellationToken token) => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task MenuViewsRules(SfAutoComplete autoComplete, CancellationToken token) => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false, FlowExceptionsToTaskScheduler = true, IncludeCancelCommand = true)]
    private Task MenuViewsColumns(SfAutoComplete autoComplete, CancellationToken token) => Task.CompletedTask;

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

    public ShellViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs e) => IsBackEnabled = NavigationService.CanGoBack;

    private void OnMenuFileExit() => Application.Current.Exit();

    private void OnMenuViewsMain() => NavigationService.NavigateTo(typeof(MainViewModel).FullName ?? nameof(MainViewModel));
}
