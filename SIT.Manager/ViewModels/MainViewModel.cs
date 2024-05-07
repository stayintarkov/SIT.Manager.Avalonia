using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Config;
using SIT.Manager.Models.Installation;
using SIT.Manager.Models.Messages;
using SIT.Manager.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IVersionService _versionService;

    private NavigationItem? _previousFooterNavigationItem;

    [ObservableProperty]
    private ActionNotification? _actionPanelNotification = new(string.Empty, 0, false);

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private bool _isTestModeEnabled = false;

    [ObservableProperty]
    private bool _updateAvailable = false;

    [ObservableProperty]
    private bool _sitUpdateAvailable = false;

    [ObservableProperty]
    private bool _isInstallRunning = false;

    [ObservableProperty]
    private NavigationItem? _selectedFooterNavigationItem;

    [ObservableProperty]
    private NavigationItem? _selectedMainNavigationItem;

    [ObservableProperty]
    public ReadOnlyCollection<NavigationItem> _footerNavigationItems;

    [ObservableProperty]
    public ReadOnlyCollection<NavigationItem> _mainNavigationItems;

    [ObservableProperty]
    private string _managerVersionString = $"v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString()}";

    public ObservableCollection<BarNotification> BarNotifications { get; } = [];

    public IRelayCommand CloseButtonCommand { get; }

    public MainViewModel(IActionNotificationService actionNotificationService,
        IAppUpdaterService appUpdaterService,
        IBarNotificationService barNotificationService,
        IInstallerService installerService,
        IManagerConfigService managerConfigService,
        ILocalizationService localizationService,
        ILogger<MainViewModel> logger,
        IServiceProvider serviceProvider,
        IVersionService versionService)
    {
        _actionNotificationService = actionNotificationService;
        _appUpdaterService = appUpdaterService;
        _barNotificationService = barNotificationService;
        _installerService = installerService;
        _managerConfigService = managerConfigService;
        _localizationService = localizationService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _versionService = versionService;

        _localizationService.Translate(new CultureInfo(_managerConfigService.Config.CurrentLanguageSelected));
        _localizationService.LocalizationChanged += LocalizationService_LocalizationChanged;

        FooterNavigationItems = new ReadOnlyCollection<NavigationItem>([
            new NavigationItem(_localizationService.TranslateSource("HelpTitle"), string.Empty, Symbol.Help, typeof(SettingsPage), Tag: "Help"),
            new NavigationItem(_localizationService.TranslateSource("SettingsTitle"), string.Empty, Symbol.Settings, typeof(SettingsPage))
        ]);
        MainNavigationItems = new ReadOnlyCollection<NavigationItem>([
            new NavigationItem(_localizationService.TranslateSource("PlayTitle"), _localizationService.TranslateSource("PlayTitle"), Symbol.Play, typeof(PlayPage)),
            new NavigationItem(_localizationService.TranslateSource("InstallTitle"), _localizationService.TranslateSource("InstallTitleToolTip"), Symbol.Sync, typeof(InstallPage)),
            new NavigationItem(_localizationService.TranslateSource("ToolsTitle"), _localizationService.TranslateSource("ToolsTitleToolTip"), Symbol.AllApps, typeof(ToolsPage)),
            new NavigationItem(_localizationService.TranslateSource("ServerTitle"), _localizationService.TranslateSource("ServerTitleToolTip"), Symbol.MapDrive, typeof(ServerPage)),
            new NavigationItem(_localizationService.TranslateSource("ModsTitle"), _localizationService.TranslateSource("ModsTitleToolTip"), Symbol.Library, typeof(ModsPage))
        ]);
        /* TODO make a new notification for updating.
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
         */

        SelectedMainNavigationItem = MainNavigationItems.First();
        NavigateToPage(SelectedMainNavigationItem.NavigationTarget);

        FluentAvaloniaTheme? faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme != null)
        {
            faTheme.CustomAccentColor = _managerConfigService.Config.AccentColor;
        }

        _actionNotificationService.ActionNotificationReceived += ActionNotificationService_ActionNotificationReceived;
        _barNotificationService.BarNotificationReceived += BarNotificationService_BarNotificationReceived;

        CloseButtonCommand = new RelayCommand(() => { UpdateAvailable = false; });

        _managerConfigService.ConfigChanged += ManagerConfigService_ConfigChanged;
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

    private void LocalizationService_LocalizationChanged(object? sender, EventArgs e)
    {
        FooterNavigationItems = new ReadOnlyCollection<NavigationItem>([
            new NavigationItem(_localizationService.TranslateSource("HelpTitle"), string.Empty, Symbol.Help, typeof(SettingsPage), Tag: "Help"),
            new NavigationItem(_localizationService.TranslateSource("SettingsTitle"), string.Empty, Symbol.Settings, typeof(SettingsPage))
        ]);
        MainNavigationItems = new ReadOnlyCollection<NavigationItem>([
            new NavigationItem(_localizationService.TranslateSource("PlayTitle"), _localizationService.TranslateSource("PlayTitle"), Symbol.Play, typeof(PlayPage)),
            new NavigationItem(_localizationService.TranslateSource("InstallTitle"), _localizationService.TranslateSource("InstallTitleToolTip"), Symbol.Sync, typeof(InstallPage)),
            new NavigationItem(_localizationService.TranslateSource("ToolsTitle"), _localizationService.TranslateSource("ToolsTitleToolTip"), Symbol.AllApps, typeof(ToolsPage)),
            new NavigationItem(_localizationService.TranslateSource("ServerTitle"), _localizationService.TranslateSource("ServerTitleToolTip"), Symbol.MapDrive, typeof(ServerPage)),
            new NavigationItem(_localizationService.TranslateSource("ModsTitle"), _localizationService.TranslateSource("ModsTitleToolTip"), Symbol.Library, typeof(ModsPage))
        ]);
    }

    private void ManagerConfigService_ConfigChanged(object? sender, ManagerConfig e)
    {
        IsTestModeEnabled = e.EnableTestMode;
    }

    private void NavigateToPage(Type page)
    {
        if (page == CurrentView?.GetType())
        {
            return;
        }
        CurrentView = (UserControl?) ActivatorUtilities.CreateInstance(_serviceProvider, page);
    }

    private async Task OpenUrlAsync(string url)
    {
        try
        {
            ContentDialog contentDialog = new()
            {
                Title = _localizationService.TranslateSource("HelpTitle"),
                Content = _localizationService.TranslateSource("HelpDialogDescription", url),
                PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes"),
                SecondaryButtonText = _localizationService.TranslateSource("ModServiceButtonNo")
            };
            ContentDialogResult result = await contentDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error lauching url ({url}).", url);
        }
    }

    [RelayCommand]
    private void UpdateButton()
    {
        NavigateToPage(typeof(UpdatePage));
        UpdateAvailable = false;
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        CheckInstallVersion();

        await CheckForUpdate();
    }

    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        base.OnPropertyChanging(e);

        if (e.PropertyName == nameof(SelectedFooterNavigationItem) && SelectedFooterNavigationItem?.Tag != "Help")
        {
            _previousFooterNavigationItem = SelectedFooterNavigationItem;
        }
    }

    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (SelectedFooterNavigationItem?.Tag == "Help")
        {
            await OpenUrlAsync("https://docs.stayintarkov.com");
        }

        if (e.PropertyName == nameof(SelectedMainNavigationItem) && SelectedMainNavigationItem != null)
        {
            SelectedFooterNavigationItem = null;
            NavigateToPage(SelectedMainNavigationItem.NavigationTarget);
        }
        if (e.PropertyName == nameof(SelectedFooterNavigationItem) && SelectedFooterNavigationItem != null)
        {
            if (SelectedFooterNavigationItem?.Tag == "Help")
            {
                SelectedFooterNavigationItem = _previousFooterNavigationItem;
                _previousFooterNavigationItem = SelectedFooterNavigationItem;
            }
            else
            {
                SelectedMainNavigationItem = null;

                if (SelectedFooterNavigationItem != null)
                {
                    NavigateToPage(SelectedFooterNavigationItem.NavigationTarget);
                }
            }
        }
    }

    public void Receive(PageNavigationMessage message)
    {
        NavigateToPage(message.Value.TargetPage);
    }

    public void Receive(InstallationRunningMessage message)
    {
        IsInstallRunning = message.Value;
    }
}
