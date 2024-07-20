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
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models.Config;
using SIT.Manager.Services;
using SIT.Manager.Services.Caching;
using SIT.Manager.Services.Install;
using SIT.Manager.Services.ManagedProcesses;
using SIT.Manager.ViewModels;
using SIT.Manager.ViewModels.Installation;
using SIT.Manager.ViewModels.Play;
using SIT.Manager.ViewModels.Settings;
using SIT.Manager.ViewModels.Tools;
using SIT.Manager.Views;
using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

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

    public App() : this([]) { }

    public App(string[] args)
    {
        Services = ConfigureServices;

        if (args.Length > 0)
        {
            ParseArguments(args);
        }
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
                .AddSingleton<ICachingService, CachingService>()
                .AddSingleton<ITarkovClientService, TarkovClientService>()
                .AddSingleton<IBarNotificationService, BarNotificationService>()
                .AddTransient<IFileService, FileService>()
                .AddSingleton<IInstallerService, InstallerService>()
                .AddSingleton<IManagerConfigService, ManagerConfigService>()
                .AddTransient<IPickerDialogService>(x =>
                {
                    if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow?.StorageProvider is not { } provider)
                    {
                        return new PickerDialogService(new MainWindow());
                    }
                    return new PickerDialogService(desktop.MainWindow);
                })
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
            services.AddTransient<CharacterSelectionViewModel>()
                .AddTransient<DirectConnectViewModel>()
                .AddTransient<ServerSelectionViewModel>();

            // Settings View Models
            services.AddTransient<EftViewModel>()
                .AddTransient<LauncherViewModel>()
                .AddTransient<LinuxViewModel>()
                .AddTransient<SptAkiViewModel>();

            // Tools view Models
            services.AddTransient<GeneralToolsViewModel>()
                .AddTransient<NetworkToolsViewModel>();

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

            services.AddHttpClient<NetworkToolsViewModel>(client =>
            {
                client.BaseAddress = new Uri("http://backend.sitcoop.org/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"SIT Manager {Assembly.GetEntryAssembly()?.GetName().Version?.ToString()}");
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
                            .HandleResult(response =>
                                response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    });
                })
                .AddResiliencePipeline<string, HttpResponseMessage>("port-checker-pipeline", builder =>
                {
                    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
                    {
                        MaxRetryAttempts = 3,
                        BackoffType = DelayBackoffType.Exponential,
                        MaxDelay = TimeSpan.FromSeconds(15),
                        OnRetry = static args =>
                        {
                            //TODO: Log failure
                            return default;
                        },
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutRejectedException>()
                    });
                });

            #endregion Polly

            return services.BuildServiceProvider();
        }
    }

    private static readonly ImmutableArray<string> trEncodings = [..new[]{"deflate", "gzip"}];

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Current.Services.GetService<MainViewModel>()
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private Task<int> ParseArguments(string[] args)
    {
        Option<string> addressOption = new(["--address", "-a"], () => "http://127.0.0.1",
            "Address of AKI server to connect to");

        Option<string> usernameOption =
            new(["--username", "-u"], "Username of the account to connect with") { IsRequired = true };

        Option<string> passwordOption =
            new(["--password", "-p"], () => string.Empty, "Password of the account to connect with");

        RootCommand rootCommand = [addressOption, usernameOption, passwordOption];

        rootCommand.SetHandler(async (addressValue, usernameValue, passwordValue) =>
        {
            DirectConnectViewModel dcvm = Services.GetRequiredService<DirectConnectViewModel>();
            await dcvm.ConnectToServer(addressValue, usernameValue, passwordValue);
        }, addressOption, usernameOption, passwordOption);

        return rootCommand.InvokeAsync(args);
    }
}
