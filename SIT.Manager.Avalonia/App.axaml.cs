using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Services;
using SIT.Manager.Avalonia.ViewModels;
using SIT.Manager.Avalonia.Views;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia;

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
    private static IServiceProvider ConfigureServices
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
                builder.AddJsonFile(o => o.RootPath = AppContext.BaseDirectory);
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
                        { "User-Agent", "request" }
                    }
                })
                .AddSingleton<IZlibService, ZlibService>()
                .AddSingleton<ILocalizationService, LocalizationService>();

            #endregion Services

            #region ViewModels

            services.AddTransient<LocationEditorViewModel>()
                .AddTransient<MainViewModel>()
                .AddTransient<ModsPageViewModel>()
                .AddTransient<PlayPageViewModel>()
                .AddTransient<SettingsPageViewModel>()
                .AddTransient<ServerPageViewModel>()
                .AddTransient<ToolsPageViewModel>();

            #endregion ViewModels

            #region Polly

            services.AddHttpClient<TarkovRequestingService>(client =>
            {
                foreach (string encoding in trEncodings)
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd(encoding);
                client.DefaultRequestHeaders.ExpectContinue = true;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = delegate { return true; }
            });

            services.AddResiliencePipeline("ping-pipeline", builder =>
            {
                builder.AddRetry(new RetryStrategyOptions()
                {
                    MaxRetryAttempts = 10,
                    //TODO: Imrpove curve for better detection
                    DelayGenerator = static args => new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(args.AttemptNumber, 1.5))),
                    OnRetry = static args =>
                    {
                        Console.WriteLine("Retrying ping. Attempt: {0}", args.AttemptNumber);
                        //TODO: Add logging
                        return default;
                    }
                });
            })
                .AddResiliencePipeline("get-pipeline", builder =>
                {
                    builder.AddRetry(new RetryStrategyOptions()
                    {
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromSeconds(3)
                    });
                });

            #endregion Polly

            return services.BuildServiceProvider();
        }
    }

    private static readonly string[] trEncodings = new string[] { "deflate", "gzip" };

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
