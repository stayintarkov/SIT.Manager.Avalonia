using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Installation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Installation;

public partial class ConfigureSitViewModel : InstallationViewModelBase
{
    private readonly IManagerConfigService _configService;
    private readonly IInstallerService _installerService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<ConfigureSitViewModel> _logger;
    private readonly IModService _modService;
    private readonly IPickerDialogService _pickerDialogService;

    [ObservableProperty]
    private bool _isVersionSelectionLoading = false;

    [ObservableProperty]
    private bool _isModsSelectionLoading = false;

    [ObservableProperty]
    private bool _overridenBsgInstallPath = false;

    [ObservableProperty]
    private SitInstallVersion? _selectedVersion;

    [ObservableProperty]
    private KeyValuePair<string, string>? _selectedMirror;

    [ObservableProperty]
    public bool _isConfigurationValid = false;

    [ObservableProperty]
    private bool _hasVersionsAvailable = false;

    [ObservableProperty]
    private bool _hasMirrorsAvailable = false;

    [ObservableProperty]
    private bool _hasRecommendedModsAvailable = false;

    [ObservableProperty]
    private bool _showNoAvailableSitVersionSelectionError = false;

    [ObservableProperty]
    private bool _showGenericSitVersionSelectionError = false;

    public ObservableCollection<SitInstallVersion> AvailableVersions { get; } = [];
    public ObservableCollection<KeyValuePair<string, string>> AvailableMirrors { get; } = [];
    public ObservableCollection<ModInfo> Mods { get; } = [];

    public IAsyncRelayCommand ChangeEftInstallLocationCommand { get; }

    public ConfigureSitViewModel(
        IManagerConfigService configService,
        IInstallerService installerService,
        ILocalizationService localizationService,
        ILogger<ConfigureSitViewModel> logger,
        IModService modService,
        IPickerDialogService pickerDialogService) : base()
    {
        _configService = configService;
        _installerService = installerService;
        _localizationService = localizationService;
        _logger = logger;
        _modService = modService;
        _pickerDialogService = pickerDialogService;

        ChangeEftInstallLocationCommand = new AsyncRelayCommand(ChangeEftInstallLocation);
    }

    private async Task ChangeEftInstallLocation()
    {
        IStorageFolder? directorySelected = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (directorySelected != null)
        {
            bool usingInvalidLocatiion = false;
            if (directorySelected.Path.LocalPath == CurrentInstallProcessState.BsgInstallPath)
            {
                // Using the same location as the current BSG install and we don't want this the same as the SIT install.
                await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorTitle"),
                    Content = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorDescription"),
                    PrimaryButtonText = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorOk")
                }.ShowAsync();
                usingInvalidLocatiion = true;
            }

            if (HasSelectedSPTInstallPath(directorySelected.Path.LocalPath))
            {
                // Using the same location as an existing SPT install and we don't want this the same as the SIT install.
                await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorTitle"),
                    Content = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionSPTErrorDescription"),
                    PrimaryButtonText = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorOk")
                }.ShowAsync();
                usingInvalidLocatiion = true;
            }

            if (HasSelectedOneDriveInstallPath(CurrentInstallProcessState.EftInstallPath))
            {
                // Using OneDrive install location which is known to cause issues so we don't want this as a location for the SIT install.
                await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorTitle"),
                    Content = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionOneDriveErrorDescription"),
                    PrimaryButtonText = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorOk")
                }.ShowAsync();
                usingInvalidLocatiion = true;
            }

            if (!usingInvalidLocatiion)
            {
                CurrentInstallProcessState.EftInstallPath = directorySelected.Path.LocalPath;
                OverridenBsgInstallPath = true;
                ValidateConfiguration();
            }
        }
    }

    /// <summary>
    /// Fetch the available versions and the download mirrors for these versions so we can populate the necesarry selections
    /// and save loading time later
    /// </summary>
    /// <returns></returns>
    private async Task FetchVersionAndMirrorMatrix()
    {
        IsVersionSelectionLoading = true;

        // Clear the collections
        HasVersionsAvailable = false;
        HasMirrorsAvailable = false;

        AvailableVersions.Clear();
        AvailableMirrors.Clear();

        try
        {
            List<SitInstallVersion> availableVersions = await _installerService.GetAvailableSitReleases(CurrentInstallProcessState.EftVersion);
            if (!string.IsNullOrEmpty(_configService.Config.SitVersion))
            {
                // Don't filter down the available versions if user has enabled developer mode.
                if (!_configService.Config.EnableTestMode)
                {
                    availableVersions = availableVersions.Where(x =>
                    {
                        bool parsedSitVersion = Version.TryParse(x.SitVersion.Replace("StayInTarkov.Client-", ""), out Version? sitVersion);
                        if (parsedSitVersion)
                        {
                            Version installedSit = Version.Parse(_configService.Config.SitVersion);
                            if (sitVersion >= installedSit)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).ToList();
                }
            }

            // Make sure we only offer versions which are actually available to use to maximize the chances the install will work
            AvailableVersions.AddRange(availableVersions.Where(x => x.IsAvailable));
            if (AvailableVersions.Count > 0)
            {
                SelectedVersion = AvailableVersions[0];
                HasVersionsAvailable = true;
            }
            else
            {
                ShowNoAvailableSitVersionSelectionError = true;
                _logger.LogWarning("Available SIT version count {availableVersions} and 0 marked as available to use so will display error message", availableVersions.Count);
            }
        }
        catch (Exception ex)
        {
            ShowGenericSitVersionSelectionError = true;
            _logger.LogError(ex, "Issue trying to determine versions available to install for SIT");
        }

        IsVersionSelectionLoading = false;

        // Validate the configuration to allow the user to start the installation without having to change the mirror
        // This way the user is able to use the first available mirror without having to change it
        ValidateConfiguration();
    }

    /// <summary>
    /// Check if the currently selected EFT install directory has indicators of a OneDrive install lication and if so return true, otherwise return false
    /// </summary>
    /// <returns>True if this is a OneDrive directory otherwise false</returns>
    private static bool HasSelectedOneDriveInstallPath(string requestedDirectory)
    {
        if (requestedDirectory.Contains("OneDrive"))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if the currently selected EFT diretory has indicators of an SPT install and if so return true, otherwise return false
    /// </summary>
    /// <returns>True if this is an SPT install directory otherwise false</returns>
    private static bool HasSelectedSPTInstallPath(string requestedDirectory)
    {
        string sptLauncherPath = Path.Combine(requestedDirectory, "Aki.Launcher.exe");
        string sptServerPath = Path.Combine(requestedDirectory, "Aki.Server.exe");
        if (File.Exists(sptLauncherPath) || File.Exists(sptServerPath))
        {
            return true;
        }
        return false;
    }

    [RelayCommand]
    private void Back()
    {
        RegressInstall();
    }

    [RelayCommand]
    private void Start()
    {
        ProgressInstall();
    }

    // TODO convert this view model into some kind of validator or something to make this part shorter and easier to handle
    private void ValidateConfiguration()
    {
        IsConfigurationValid = true;

        if (string.IsNullOrEmpty(CurrentInstallProcessState.EftInstallPath))
        {
            IsConfigurationValid = false;
            return;
        }

        if (AvailableMirrors.Count != 0 && string.IsNullOrEmpty(CurrentInstallProcessState.DownloadMirrorUrl))
        {
            IsConfigurationValid = false;
            return;
        }

        if (SelectedVersion == null)
        {
            IsConfigurationValid = false;
            return;
        }

        if (IsModsSelectionLoading || IsVersionSelectionLoading)
        {
            IsConfigurationValid = false;
            return;
        }

        if (CurrentInstallProcessState.BsgInstallPath == CurrentInstallProcessState.EftInstallPath)
        {
            IsConfigurationValid = false;
            return;
        }
        if (HasSelectedSPTInstallPath(CurrentInstallProcessState.EftInstallPath))
        {
            IsConfigurationValid = false;
            return;
        }
        if (HasSelectedOneDriveInstallPath(CurrentInstallProcessState.EftInstallPath))
        {
            IsConfigurationValid = false;
            return;
        }
    }

    private async Task LoadAvailableModsList()
    {
        IsModsSelectionLoading = true;

        try
        {
            await _modService.LoadMasterModList();
            if (_modService.ModList.Count <= 1)
            {
                await _modService.DownloadModsCollection();
                await _modService.LoadMasterModList();
            }

            Mods.Clear();
            Mods.AddRange(_modService.ModList.Where(x => _modService.RecommendedModInstalls.Contains(x.Name)));

            // Make sure that all the recommended mods are selected to start with
            CurrentInstallProcessState.RequestedMods = [.. Mods];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when trying to evaluate available mod list");

            Mods.Clear();
            CurrentInstallProcessState.RequestedMods = [];
        }

        HasRecommendedModsAvailable = Mods.Count > 0;
        IsModsSelectionLoading = false;
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        if (CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateSit)
        {
            SitInstallVersion? availableVersion = _installerService.GetLatestAvailableSitRelease();
            if (availableVersion != null)
            {
                CurrentInstallProcessState.RequestedVersion = availableVersion.Release;
                ProgressInstall();
                return;
            }
        }

        // Reset the error messages
        ShowNoAvailableSitVersionSelectionError = false;
        ShowGenericSitVersionSelectionError = false;

        OverridenBsgInstallPath = CurrentInstallProcessState.BsgInstallPath != CurrentInstallProcessState.EftInstallPath;
        await Task.WhenAll(LoadAvailableModsList(), FetchVersionAndMirrorMatrix());
    }

    partial void OnSelectedVersionChanged(SitInstallVersion? value)
    {
        if (value != null)
        {
            CurrentInstallProcessState.RequestedVersion = value.Release;

            AvailableMirrors.Clear();
            if (value.DownloadMirrors.Count > 0)
            {
                AvailableMirrors.AddRange(value.DownloadMirrors);
                SelectedMirror = AvailableMirrors[0];
                HasMirrorsAvailable = true;
            }
            else
            {
                SelectedMirror = null;
                HasMirrorsAvailable = false;
            }
        }
        ValidateConfiguration();
    }

    partial void OnSelectedMirrorChanged(KeyValuePair<string, string>? value)
    {
        CurrentInstallProcessState.DownloadMirrorUrl = value?.Value ?? string.Empty;
        ValidateConfiguration();
    }
}
