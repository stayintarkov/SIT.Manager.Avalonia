using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Config;
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
    private readonly IVersionService _versionService;

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

    public ReadOnlyCollection<NavigationItem> MainNavigationItems { get; }

    public IRelayCommand CloseButtonCommand { get; }

    public MainViewModel(IActionNotificationService actionNotificationService,
        IAppUpdaterService appUpdaterService,
        IBarNotificationService barNotificationService,
        IInstallerService installerService,
        IManagerConfigService managerConfigService,
        ILocalizationService localizationService,
        ILogger<MainViewModel> logger,
        IVersionService versionService)
    {
        _actionNotificationService = actionNotificationService;
        _appUpdaterService = appUpdaterService;
        _barNotificationService = barNotificationService;
        _installerService = installerService;
        _managerConfigService = managerConfigService;
        _localizationService = localizationService;
        _logger = logger;
        _versionService = versionService;

        _localizationService.Translate(new CultureInfo(_managerConfigService.Config.CurrentLanguageSelected));

        MainNavigationItems = new ReadOnlyCollection<NavigationItem>([
            new NavigationItem(_localizationService.TranslateSource("PlayTitle"), _localizationService.TranslateSource("PlayTitle"), Symbol.Play, typeof(PlayPage))
        ]);
        /* TODO
						<ListBoxItem>
							<ui:NavigationViewItem IconSource="Sync"
											   Content="{DynamicResource InstallTitle}"
											   IsEnabled="{Binding !IsInstallRunning}"
											   Tag="SIT.Manager.Views.InstallPage"
											   ToolTip.Tip="{DynamicResource InstallTitleToolTip}">
								<ui:NavigationViewItem.InfoBadge>
									<ui:InfoBadge IconSource="ChevronUp"
												  IsVisible="{Binding SitUpdateAvailable}"/>
								</ui:NavigationViewItem.InfoBadge>
							</ui:NavigationViewItem>
						</ListBoxItem>
						<ListBoxItem>
							<ui:NavigationViewItem IconSource="AllApps"
											   Content="{DynamicResource ToolsTitle}"
											   IsEnabled="{Binding !IsInstallRunning}"
											   Tag="SIT.Manager.Views.ToolsPage"
											   ToolTip.Tip="{DynamicResource ToolsTitleToolTip}"/>
						</ListBoxItem>
						<ListBoxItem>
							<ui:NavigationViewItem IconSource="MapDrive"
											   Content="{DynamicResource ServerTitle}"
											   IsEnabled="{Binding !IsInstallRunning}"
											   Tag="SIT.Manager.Views.ServerPage"
											   ToolTip.Tip="{DynamicResource ServerTitleToolTip}"/>
						</ListBoxItem>
						<ListBoxItem>
							<ui:NavigationViewItem IconSource="Library"
											   IsVisible="{Binding !IsDevloperModeEnabled}"
											   IsEnabled="{Binding !IsInstallRunning}"
											   Content="{DynamicResource ModsTitle}"
											   Tag="SIT.Manager.Views.ModsPage"
											   ToolTip.Tip="{DynamicResource ModsTitleToolTip}"/>
						</ListBoxItem>
         */

        FluentAvaloniaTheme? faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme != null) faTheme.CustomAccentColor = _managerConfigService.Config.AccentColor;

        _actionNotificationService.ActionNotificationReceived += ActionNotificationService_ActionNotificationReceived;
        _barNotificationService.BarNotificationReceived += BarNotificationService_BarNotificationReceived;

        CloseButtonCommand = new RelayCommand(() => { UpdateAvailable = false; });

        _managerConfigService.ConfigChanged += ManagerConfigService_ConfigChanged;
    }

    private void CheckInstallVersion()
    {
        ManagerConfig config = _managerConfigService.Config;
        if (!string.IsNullOrEmpty(_managerConfigService.Config.SitTarkovVersion))
        {
            config.SitTarkovVersion = _versionService.GetEFTVersion(config.SitEftInstallPath);
            config.SitVersion = _versionService.GetSITVersion(config.SitEftInstallPath);
            if (string.IsNullOrEmpty(config.SitTarkovVersion) || string.IsNullOrEmpty(config.SitVersion))
            {
                config.SitEftInstallPath = string.Empty;
                _managerConfigService.UpdateConfig(config);
            }
        }
        if (!string.IsNullOrEmpty(_managerConfigService.Config.SptAkiVersion))
        {
            config.SptAkiVersion = _versionService.GetSptAkiVersion(config.AkiServerPath);
            config.SitModVersion = _versionService.GetSitModVersion(config.AkiServerPath);
            if (string.IsNullOrEmpty(config.SptAkiVersion) || string.IsNullOrEmpty(config.SptAkiVersion))
            {
                config.AkiServerPath = string.Empty;
                _managerConfigService.UpdateConfig(config);
            }
        }
    }

    private void ManagerConfigService_ConfigChanged(object? sender, ManagerConfig e)
    {
        IsDevloperModeEnabled = e.EnableDeveloperMode;
    }

    private async Task CheckForUpdate()
    {
        try
        {
            UpdateAvailable = await _appUpdaterService.CheckForUpdate();
            SitUpdateAvailable = await _installerService.IsSitUpdateAvailable();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for update to either Manager or SIT");
            _barNotificationService.ShowError(_localizationService.TranslateSource("MainViewModelUpdateErrorTitle"), _localizationService.TranslateSource("MainViewModelUpdateErrorMessage"));
        }
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

        CheckInstallVersion();

        await CheckForUpdate();
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
