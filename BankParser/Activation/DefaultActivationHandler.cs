using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using BankParser.Contracts.Services;
using BankParser.ViewModels;

using Microsoft.UI.Xaml;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace BankParser.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;

    public DefaultActivationHandler(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args) =>
        // None of the ActivationHandlers has handled the activation.
        _navigationService.Frame?.Content == null;

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        string? a = args?.Arguments ?? string.Empty;
        _navigationService.NavigateTo(typeof(MainViewModel).FullName ?? nameof(MainViewModel), a);

        await Task.CompletedTask;
    }
}
