using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using SIT.Manager.Models.Installation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Installation;

public partial class ConfigureSitViewModel : InstallationViewModelBase
{
    private readonly IManagerConfigService _configService;
    private readonly IInstallerService _installerService;
    private readonly ILocalizationService _localizationService;
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

    public ObservableCollection<SitInstallVersion> AvailableVersions { get; } = [];
    public ObservableCollection<KeyValuePair<string, string>> AvailableMirrors { get; } = [];
    public ObservableCollection<ModInfo> Mods { get; } = [];

    public IAsyncRelayCommand ChangeEftInstallLocationCommand { get; }

    public ConfigureSitViewModel(
        IManagerConfigService configService,
        IInstallerService installerService,
        ILocalizationService localizationService,
        IModService modService,
        IPickerDialogService pickerDialogService) : base()
    {
        _configService = configService;
        _installerService = installerService;
        _localizationService = localizationService;
        _modService = modService;
        _pickerDialogService = pickerDialogService;

        ChangeEftInstallLocationCommand = new AsyncRelayCommand(ChangeEftInstallLocation);
    }

    private async Task ChangeEftInstallLocation()
    {
        IStorageFolder? directorySelected = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (directorySelected != null)
        {
            if (directorySelected.Path.LocalPath == CurrentInstallProcessState.BsgInstallPath)
            {
                // Using the same location as the current BSG install and we don't want this the same as the SIT install.
                await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorTitle"),
                    Content = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorDescription"),
                    PrimaryButtonText = _localizationService.TranslateSource("ConfigureSitViewModelLocationSelectionErrorOk")
                }.ShowAsync();
            }
            else
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

        List<SitInstallVersion> availableVersions = await _installerService.GetAvailableSitReleases(CurrentInstallProcessState.EftVersion);
        if (!string.IsNullOrEmpty(_configService.Config.SitVersion))
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

        // Make sure we only offer versions which are actually available to use to maximize the chances the install will work
        AvailableVersions.AddRange(availableVersions.Where(x => x.IsAvailable));
        if (AvailableVersions.Count > 0)
        {
            SelectedVersion = AvailableVersions[0];
            HasVersionsAvailable = true;
        }

        // TODO add some logging here and an alert somehow in case it fails to load any versions or something

        IsVersionSelectionLoading = false;
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

    private void ValidateConfiguration()
    {
        IsConfigurationValid = true;

        if (CurrentInstallProcessState.UsingBsgInstallPath)
        {
            if (CurrentInstallProcessState.BsgInstallPath == CurrentInstallProcessState.EftInstallPath)
            {
                IsConfigurationValid = false;
                return;
            }
        }

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
    }

    private async Task LoadAvailableModsList()
    {
        IsModsSelectionLoading = true;

        await _modService.LoadMasterModList();
        if (_modService.ModList.Count <= 1)
        {
            await _modService.DownloadModsCollection();
            await _modService.LoadMasterModList();
        }

        Mods.AddRange(_modService.ModList.Where(x => _modService.RecommendedModInstalls.Contains(x.Name)));

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
