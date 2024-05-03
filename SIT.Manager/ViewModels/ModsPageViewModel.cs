using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Config;
using SIT.Manager.Models.Messages;
using SIT.Manager.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

public partial class ModsPageViewModel : ObservableRecipient
{
    private readonly IBarNotificationService _barNotificationService;
    private readonly IManagerConfigService _managerConfigService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger _logger;
    private readonly IModService _modService;

    [ObservableProperty]
    private bool _showModsDisclaimer = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowModInfo))]
    private ModInfo? _selectedMod = null;

    [ObservableProperty]
    private bool _enableInstall = false;

    public bool ShowModInfo => SelectedMod != null;

    public ObservableCollection<ModInfo> ModList { get; } = [];

    public IAsyncRelayCommand DownloadModPackageCommand { get; }

    public IAsyncRelayCommand InstallModCommand { get; }

    public IAsyncRelayCommand UninstallModCommand { get; }

    public ModsPageViewModel(IManagerConfigService managerConfigService,
                             ILocalizationService localizationService,
                             IBarNotificationService barNotificationService,
                             ILogger<ModsPageViewModel> logger,
                             IModService modService)
    {
        _barNotificationService = barNotificationService;
        _managerConfigService = managerConfigService;
        _localizationService = localizationService;
        _logger = logger;
        _modService = modService;

        if (_managerConfigService.Config.AcceptedModsDisclaimer)
        {
            ShowModsDisclaimer = false;
        }

        DownloadModPackageCommand = new AsyncRelayCommand(DownloadModPackage);
        InstallModCommand = new AsyncRelayCommand(InstallMod);
        UninstallModCommand = new AsyncRelayCommand(UninstallMod);
    }

    private async Task LoadMasterList()
    {
        if (string.IsNullOrEmpty(_managerConfigService.Config.SitEftInstallPath))
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModsPageViewModelErrorTitle"), _localizationService.TranslateSource("ModsPageViewModelErrorInstallPathDescription"));
            return;
        }

        await _modService.LoadMasterModList();

        ModList.Clear();
        List<ModInfo> outdatedMods = [];
        foreach (ModInfo mod in _modService.ModList)
        {
            ModList.Add(mod);

            var keyValuePair = _managerConfigService.Config.InstalledMods.Where(x => x.Key == mod.Name).FirstOrDefault();

            if (!keyValuePair.Equals(default(KeyValuePair<string, string>)))
            {
                Version installedVersion = new(keyValuePair.Value);
                Version currentVersion = new(mod.PortVersion);

                int result = installedVersion.CompareTo(currentVersion);
                if (result < 0)
                {
                    outdatedMods.Add(mod);
                }
            }
        }

        if (ModList.Count > 0)
        {
            SelectedMod = ModList[0];
        }

        if (outdatedMods.Count > 0)
        {
            await _modService.AutoUpdate(outdatedMods);
        }
    }

    [RelayCommand]
    private void AcceptModsDisclaimer()
    {
        ShowModsDisclaimer = false;

        ManagerConfig config = _managerConfigService.Config;
        config.AcceptedModsDisclaimer = true;
        _managerConfigService.UpdateConfig(config);
    }

    private async Task DownloadModPackage()
    {
        if (string.IsNullOrEmpty(_managerConfigService.Config.SitEftInstallPath))
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModsPageViewModelErrorTitle"), _localizationService.TranslateSource("ModsPageViewModelErrorInstallPathDescription"));
            return;
        }
        _logger.LogInformation("DownloadModPack: Starting download of mod package.");

        try
        {
            await _modService.DownloadModsCollection();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DownloadModPack");
        }

        await LoadMasterList();
    }

    partial void OnSelectedModChanged(ModInfo? value)
    {
        if (value == null)
        {
            return;
        }

        bool isInstalled = _managerConfigService.Config.InstalledMods.ContainsKey(value.Name);
        EnableInstall = !isInstalled;
    }

    private async Task InstallMod()
    {
        if (SelectedMod == null)
        {
            return;
        }

        bool installSuccessful = await _modService.InstallMod(_managerConfigService.Config.SitEftInstallPath, SelectedMod);
        EnableInstall = !installSuccessful;
    }

    private async Task UninstallMod()
    {
        if (SelectedMod == null)
        {
            return;
        }

        bool uninstallSuccessful = await _modService.UninstallMod(_managerConfigService.Config.SitEftInstallPath, SelectedMod);
        EnableInstall = uninstallSuccessful;
    }

    protected override async void OnActivated()
    {
        // Test mode enabled but we are still trying to go to the mods page so 
        // force them to a different page
        if (_managerConfigService.Config.EnableTestMode)
        {
            PageNavigation pageNavigation = new(typeof(PlayPage), false);
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
        }

        await LoadMasterList();
    }
}
