using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Services;
using SIT.Manager.Services.Install;
using SIT.Manager.ViewModels;
using SIT.Manager.ViewModels.Installation;
using SIT.Manager.ViewModels.Play;
using SIT.Manager.Views;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;

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
        Services = ConfigureServices;
    }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static ServiceProvider ConfigureServices
    {
        get
        {
            var services = new ServiceCollection();

            #region Logging

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
            });

            #endregion Logging

            #region Services

            services.AddSingleton<IActionNotificationService, ActionNotificationService>()
                .AddSingleton<IAkiServerService, AkiServerService>()
                .AddSingleton<ITarkovClientService, TarkovClientService>()
                .AddSingleton<IBarNotificationService, BarNotificationService>()
                .AddTransient<IFileService, FileService>()
                .AddTransient<IInstallerService, InstallerService>()
                .AddSingleton<IManagerConfigService, ManagerConfigService>()
                .AddTransient<IModService, ModService>()
                .AddTransient<IPickerDialogService>(x =>
                {
                    if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow?.StorageProvider is not { } provider)
                    {
                        return new PickerDialogService(new MainWindow());
                    }
                    return new PickerDialogService(desktop.MainWindow);
                })
                .AddSingleton<ITarkovClientService, TarkovClientService>()
                .AddSingleton<IVersionService, VersionService>()
                //TODO: Move this to httpclient factory with proper configuration
                .AddSingleton(new HttpClientHandler
                {
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                    ServerCertificateCustomValidationCallback = delegate { return true; }
                })
                .AddSingleton(provider => new HttpClient(provider.GetService<HttpClientHandler>() ?? throw new ArgumentNullException())
                {
                    DefaultRequestHeaders =
                    {
                        { "X-GitHub-Api-Version", "2022-11-28" },
                        { "User-Agent", $"SITManager/{Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.1a"}" }
                    }
                })
                .AddSingleton<ILocalizationService, LocalizationService>()
                .AddSingleton<IAkiServerRequestingService, AkiServerRequestingService>()
                .AddSingleton<IAppUpdaterService, AppUpdaterService>()
                .AddSingleton<IDiagnosticService, DiagnosticService>();

            #endregion Services

            #region ViewModels

            // Page Viewmodels
            services.AddTransient<InstallPageViewModel>()
                .AddTransient<LocationEditorViewModel>()
                .AddTransient<MainViewModel>()
                .AddTransient<ModsPageViewModel>()
                .AddTransient<PlayPageViewModel>()
                .AddTransient<SettingsPageViewModel>()
                .AddTransient<ServerPageViewModel>()
                .AddTransient<ToolsPageViewModel>()
                .AddTransient<UpdatePageViewModel>();

            // Installation View Models
            services.AddTransient<CompleteViewModel>()
                .AddTransient<ConfigureSitViewModel>()
                .AddTransient<ConfigureServerViewModel>()
                .AddTransient<InstallViewModel>()
                .AddTransient<PatchViewModel>()
                .AddTransient<SelectViewModel>();

            // Play View Models
            services.AddTransient<DirectConnectViewModel>();

            #endregion ViewModels

            #region Polly

            services.AddHttpClient<AkiServerRequestingService>(client =>
            {
                foreach (string encoding in trEncodings)
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd(encoding);
                client.DefaultRequestHeaders.ExpectContinue = true;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = delegate { return true; },
                MaxConnectionsPerServer = 10
            });

            services.AddResiliencePipeline<string, HttpResponseMessage>("default-pipeline", builder =>
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(3)
                });
            })
            .AddResiliencePipeline<string, HttpResponseMessage>("ping-pipeline", builder =>
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
                {
                    MaxRetryAttempts = 6,
                    BackoffType = DelayBackoffType.Exponential,
                    MaxDelay = TimeSpan.FromSeconds(30),
                    OnRetry = static args =>
                    {
                        Debug.WriteLine("Retrying ping. Attempt: {0}", args.AttemptNumber);
                        //TODO: Add logging
                        return default;
                    },
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(response => response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                });
            });

            #endregion Polly

            return services.BuildServiceProvider();
        }
    }

    private static readonly string[] trEncodings = ["deflate", "gzip"];

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
