using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using SIT.Manager.Models.Installation;
using SIT.Manager.Models.Messages;
using SIT.Manager.Views;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

public partial class MainViewModel : ObservableRecipient, IRecipient<InstallationRunningMessage>, IRecipient<PageNavigationMessage>
{
    private readonly IActionNotificationService _actionNotificationService;
    private readonly IAppUpdaterService _appUpdaterService;
    private readonly IBarNotificationService _barNotificationService;
    private readonly IInstallerService _installerService;
    private readonly IManagerConfigService _managerConfigService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainViewModel> _logger;

    private Frame? contentFrame;

    [ObservableProperty]
    private ActionNotification? _actionPanelNotification = new(string.Empty, 0, false);

    [ObservableProperty]
    private bool _isDevloperModeEnabled = false;

    [ObservableProperty]
    private bool _updateAvailable = false;

    [ObservableProperty]
    private bool _sitUpdateAvailable = false;

    [ObservableProperty]
    private bool _isInstallRunning = false;

    public ObservableCollection<BarNotification> BarNotifications { get; } = [];

    public IRelayCommand CloseButtonCommand { get; }

    public MainViewModel(IActionNotificationService actionNotificationService,
        IAppUpdaterService appUpdaterService,
        IBarNotificationService barNotificationService,
        IInstallerService installerService,
        IManagerConfigService managerConfigService,
        ILocalizationService localizationService,
        ILogger<MainViewModel> logger)
    {
        _actionNotificationService = actionNotificationService;
        _appUpdaterService = appUpdaterService;
        _barNotificationService = barNotificationService;
        _installerService = installerService;
        _managerConfigService = managerConfigService;
        _localizationService = localizationService;
        _logger = logger;

        _localizationService.Translate(new CultureInfo(_managerConfigService.Config.CurrentLanguageSelected));

        var faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme != null) faTheme.CustomAccentColor = _managerConfigService.Config.AccentColor;

        _actionNotificationService.ActionNotificationReceived += ActionNotificationService_ActionNotificationReceived;
        _barNotificationService.BarNotificationReceived += BarNotificationService_BarNotificationReceived;

        CloseButtonCommand = new RelayCommand(() => { UpdateAvailable = false; });

        _managerConfigService.ConfigChanged += ManagerConfigService_ConfigChanged;
    }

    private async void ManagerConfigService_ConfigChanged(object? sender, ManagerConfig e)
    {
        IsDevloperModeEnabled = e.EnableDeveloperMode;
        try
        {
            await CheckForUpdate();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occured while trying to check for updates");
        }
    }

    private async Task CheckForUpdate()
    {
        UpdateAvailable = await _appUpdaterService.CheckForUpdate();
        SitUpdateAvailable = await _installerService.IsSitUpdateAvailable();
    }

    [RelayCommand]
    private void UpdateButton()
    {
        NavigateToPage(typeof(UpdatePage), false);
        UpdateAvailable = false;
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

    public void Receive(InstallationRunningMessage message)
    {
        IsInstallRunning = message.Value;
    }
}
