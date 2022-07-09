using Windows.ApplicationModel.Activation;
using BankParser.Activation;
using BankParser.Contracts.Services;
using BankParser.Core.Contracts.Services;
using BankParser.Core.Services;
using BankParser.Helpers;
using BankParser.Models;
using BankParser.Services;
using BankParser.ViewModels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using BankParser.Views;
using Microsoft.UI.Dispatching;

// To learn more about WinUI3, see: https://docs.microsoft.com/windows/apps/winui/winui3/.
namespace BankParser;

public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(configBuilder => configBuilder.AddUserSecrets(typeof(App).Assembly, true))
        .ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsServicePackaged>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<ShellPage>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton(_ => new Window()
            {
                Title = "AppDisplayName".GetLocalized()
            });

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        })
        .Build();

    private static readonly Window? _window = null;

    public static T? GetService<T>()
        where T : class => _host.Services.GetService(typeof(T)) as T;

    public static Window MainWindow => _window ?? GetService<Window>()!;

    public App()
    {
        var config = _host.Services.GetRequiredService<IConfiguration>();
        string? sf = config["SF"];
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(sf);
        InitializeComponent();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // For more details, see https://docs.microsoft.com/windows/winui/api/microsoft.ui.xaml.unhandledexceptioneventargs.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        IActivationService? activationService = GetService<IActivationService>();
        if (activationService is not null)
        {
            await activationService.ActivateAsync(args);
        }
    }

    internal static void TryDispatch(DispatcherQueueHandler callback)
        => App.MainWindow.Content.DispatcherQueue.TryEnqueue(callback);

}
