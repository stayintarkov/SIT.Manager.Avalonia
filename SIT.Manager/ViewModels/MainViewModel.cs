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
    private readonly IManagerConfigService _configService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IVersionService _versionService;

    private NavigationItem? _previousFooterNavigationItem;
    private SITConfig _sitConfig => _configService.Config.SITSettings;
    private AkiConfig _akiConfig => _configService.Config.AkiSettings;
    private LauncherConfig _launcherConfig => _configService.Config.LauncherSettings;

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

    public IAsyncRelayCommand UpdateAppCommand { get; }

    public MainViewModel(IActionNotificationService actionNotificationService,
        IAppUpdaterService appUpdaterService,
        IBarNotificationService barNotificationService,
        IInstallerService installerService,
        IManagerConfigService configService,
        ILocalizationService localizationService,
        ILogger<MainViewModel> logger,
        IServiceProvider serviceProvider,
        IVersionService versionService)
    {
        _actionNotificationService = actionNotificationService;
        _appUpdaterService = appUpdaterService;
        _barNotificationService = barNotificationService;
        _installerService = installerService;
        _configService = configService;
        _localizationService = localizationService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _versionService = versionService;

        _localizationService.Translate(new CultureInfo(_launcherConfig.CurrentLanguageSelected));
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
            faTheme.CustomAccentColor = _launcherConfig.AccentColor;
        }

        _actionNotificationService.ActionNotificationReceived += ActionNotificationService_ActionNotificationReceived;
        _barNotificationService.BarNotificationReceived += BarNotificationService_BarNotificationReceived;

        CloseButtonCommand = new RelayCommand(() => { UpdateAvailable = false; });
        UpdateAppCommand = new AsyncRelayCommand(UpdateApp);
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
        if (!string.IsNullOrEmpty(_sitConfig.SitTarkovVersion))
        {
            _sitConfig.SitTarkovVersion = _versionService.GetEFTVersion(_sitConfig.SitEFTInstallPath);
            _sitConfig.SitVersion = _versionService.GetSITVersion(_sitConfig.SitEFTInstallPath);
            if (string.IsNullOrEmpty(_sitConfig.SitTarkovVersion) || string.IsNullOrEmpty(_sitConfig.SitVersion))
            {
                _sitConfig.SitEFTInstallPath = string.Empty;
            }
        }
        if (!string.IsNullOrEmpty(_akiConfig.SptAkiVersion))
        {
            _akiConfig.SptAkiVersion = _versionService.GetSptAkiVersion(_akiConfig.AkiServerPath);
            _akiConfig.SitModVersion = _versionService.GetSitModVersion(_akiConfig.AkiServerPath);
            if (string.IsNullOrEmpty(_akiConfig.SptAkiVersion) || string.IsNullOrEmpty(_akiConfig.SptAkiVersion))
            {
                _akiConfig.AkiServerPath = string.Empty;
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
        ]);
    }

    private void ManagerConfigService_ConfigChanged(object? sender, ManagerConfig e)
    {
        IsTestModeEnabled = e.LauncherSettings.EnableTestMode;
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

    private async Task UpdateApp()
    {
        ContentDialogResult updateRequestResult = await new ContentDialog()
        {
            Title = _localizationService.TranslateSource("UpdatePageViewModelUpdateConfirmationTitle"),
            Content = _localizationService.TranslateSource("UpdatePageViewModelUpdateConfirmationDescription"),
            PrimaryButtonText = _localizationService.TranslateSource("UpdatePageViewModelButtonYes"),
            CloseButtonText = _localizationService.TranslateSource("UpdatePageViewModelButtonNo")
        }.ShowAsync();
        if (updateRequestResult == ContentDialogResult.Primary)
        {
            NavigateToPage(typeof(UpdatePage));
            UpdateAvailable = false;
        }
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        CheckInstallVersion();

#if DEBUG
        // Don't run update checks in debug builds just assume they exist
        UpdateAvailable = true;
        SitUpdateAvailable = true;
        await Task.Delay(10);
#else
        await CheckForUpdate();
#endif
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
