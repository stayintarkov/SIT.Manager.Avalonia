using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using Microsoft.Extensions.Logging;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class MainViewModel : ObservableRecipient, IRecipient<PageNavigationMessage>
{
    private readonly IActionNotificationService _actionNotificationService;
    private readonly IAppUpdaterService _appUpdaterService;
    private readonly IBarNotificationService _barNotificationService;
    private readonly IManagerConfigService _managerConfigService;
    private readonly ILocalizationService _localizationService;

    private Frame? contentFrame;

    [ObservableProperty]
    private ActionNotification? _actionPanelNotification = new(string.Empty, 0, false);

    [ObservableProperty]
    private bool _updateAvailable = false;

    public ObservableCollection<BarNotification> BarNotifications { get; } = [];

    public IAsyncRelayCommand UpdateButtonCommand { get; }
    public IRelayCommand CloseButtonCommand { get; }

    public MainViewModel(IActionNotificationService actionNotificationService,
        IAppUpdaterService appUpdaterService,
        IBarNotificationService barNotificationService,
        IManagerConfigService managerConfigService,
        ILocalizationService localizationService)        
    {
        _actionNotificationService = actionNotificationService;
        _appUpdaterService = appUpdaterService;
        _barNotificationService = barNotificationService;
        _managerConfigService = managerConfigService;
        _localizationService = localizationService;

        _localizationService.Translate(new CultureInfo(_managerConfigService.Config.CurrentLanguageSelected));

        var faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme != null) faTheme.CustomAccentColor = _managerConfigService.Config.AccentColor;

        _actionNotificationService.ActionNotificationReceived += ActionNotificationService_ActionNotificationReceived;
        _barNotificationService.BarNotificationReceived += BarNotificationService_BarNotificationReceived;

        UpdateButtonCommand = new AsyncRelayCommand(UpdateButton);
        CloseButtonCommand = new RelayCommand(() => { UpdateAvailable = false; });

        _managerConfigService.ConfigChanged += async (o, c) => await CheckForUpdate();
    }

    private async Task CheckForUpdate()
    {
        UpdateAvailable = await _appUpdaterService.CheckForUpdate();
    }

    private async Task UpdateButton()
    {
        ContentDialogResult updateResult = await new ContentDialog()
        {
            Title = _localizationService.TranslateSource("MainPageViewModelUpdateConfirmationTitle"),
            Content = _localizationService.TranslateSource("MainPageViewModelUpdateConfirmationDescription"),
            PrimaryButtonText = _localizationService.TranslateSource("MainPageViewModelButtonYes"),
            CloseButtonText = _localizationService.TranslateSource("MainPageViewModelButtonNo")
        }.ShowAsync();

        if (updateResult == ContentDialogResult.Primary)
        {
            //TODO: Add a way to update for linux users
            if (OperatingSystem.IsWindows())
            {
                //TODO: Change this to use a const
                string updaterPath = Path.Combine(AppContext.BaseDirectory, "SIT.Manager.Updater.exe");
                if (File.Exists(updaterPath))
                {
                    Process.Start(updaterPath);
                    IApplicationLifetime? lifetime = Application.Current?.ApplicationLifetime;
                    if (lifetime != null && lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                    {
                        desktopLifetime.Shutdown();
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }
            else
            {
                await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("MainPageViewModelUnsupportedTitle"),
                    Content = _localizationService.TranslateSource("MainPageViewModelUnsupportedDescription"),
                    CloseButtonText = _localizationService.TranslateSource("MainPageViewModelButtonOk")
                }.ShowAsync();
            }
        }
    }

    private void ActionNotificationService_ActionNotificationReceived(object? sender, ActionNotification e)
    {
        ActionPanelNotification = e;
    }

    private async void BarNotificationService_BarNotificationReceived(object? sender, BarNotification e)
    {
        BarNotifications.Add(e);
        if (e.Delay > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(e.Delay));
            BarNotifications.Remove(e);
        }
    }

    private bool NavigateToPage(Type page, bool suppressTransition = false)
    {
        object? currentPage = contentFrame?.Content;
        if (page == currentPage?.GetType())
        {
            return false;
        }
        return contentFrame?.Navigate(page, null, suppressTransition ? new SuppressNavigationTransitionInfo() : null) ?? false;
    }

    protected override async void OnActivated()
    {
        base.OnActivated();
        await CheckForUpdate();
    }

    public void RegisterContentFrame(Frame frame)
    {
        contentFrame = frame;
    }

    public void Receive(PageNavigationMessage message)
    {
        NavigateToPage(message.Value.TargetPage, message.Value.SuppressTransition);
    }
}
