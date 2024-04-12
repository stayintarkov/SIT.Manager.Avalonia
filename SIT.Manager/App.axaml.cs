using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Services;
using SIT.Manager.Services.Install;
using SIT.Manager.ViewModels;
using SIT.Manager.ViewModels.Installation;
using SIT.Manager.Views;
using System;
using System.Net.Http;

namespace SIT.Manager;

public sealed partial class App : Application
{
    /// <summary>
    /// Gets the current <see cref="App"/> instance in use
    /// </summary>
    public new static App Current => Application.Current as App ?? throw new ArgumentNullException(nameof(Application.Current));

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddJsonFile(o => o.RootPath = AppContext.BaseDirectory);
        });

        // Services
        services.AddSingleton<IActionNotificationService, ActionNotificationService>();
        services.AddSingleton<IAkiServerService, AkiServerService>();
        services.AddSingleton<IAppUpdaterService, AppUpdaterService>();
        services.AddSingleton<ITarkovClientService, TarkovClientService>();
        services.AddSingleton<IBarNotificationService, BarNotificationService>();
        services.AddTransient<IFileService, FileService>();
        services.AddSingleton<IInstallerService, InstallerService>();
        services.AddSingleton<IManagerConfigService, ManagerConfigService>();
        services.AddSingleton<IModService, ModService>();
        services.AddTransient<IPickerDialogService>(x =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow?.StorageProvider is not { } provider)
            {
                return new PickerDialogService(new MainWindow());
            }
            return new PickerDialogService(desktop.MainWindow);
        });
        services.AddSingleton<ITarkovClientService, TarkovClientService>();
        services.AddSingleton<IVersionService, VersionService>();
        services.AddSingleton(new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            ServerCertificateCustomValidationCallback = delegate { return true; }
        });
        services.AddSingleton(provider => new HttpClient(provider.GetService<HttpClientHandler>() ?? throw new ArgumentNullException())
        {
            DefaultRequestHeaders = {
                { "X-GitHub-Api-Version", "2022-11-28" },
                { "User-Agent", "request" }
            }
        });
        services.AddSingleton<IZlibService, ZlibService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IDiagnosticService, DiagnosticService>();

        // Page Viewmodels
        services.AddTransient<InstallPageViewModel>();
        services.AddTransient<LocationEditorViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ModsPageViewModel>();
        services.AddTransient<PlayPageViewModel>();
        services.AddTransient<LinuxSettingsPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<ServerPageViewModel>();
        services.AddTransient<ToolsPageViewModel>();
        services.AddTransient<UpdatePageViewModel>();

        // Installation View Models
        services.AddTransient<CompleteViewModel>();
        services.AddTransient<ConfigureSitViewModel>();
        services.AddTransient<ConfigureServerViewModel>();
        services.AddTransient<InstallViewModel>();
        services.AddTransient<PatchViewModel>();
        services.AddTransient<SelectViewModel>();

        return services.BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Current.Services.GetService<MainViewModel>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = Current.Services.GetService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
