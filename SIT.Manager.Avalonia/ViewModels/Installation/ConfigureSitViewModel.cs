using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using ReactiveUI;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Installation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class ConfigureSitViewModel : InstallationViewModelBase
{
    private readonly IInstallerService _installerService;
    private readonly IPickerDialogService _pickerDialogService;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private GithubRelease? _selectedVersion;

    [ObservableProperty]
    private KeyValuePair<string, string> _selectedMirror = new();

    [ObservableProperty]
    public bool _isConfigurationValid = false;

    [ObservableProperty]
    private bool _hasMirrorsAvailable = false;

    public ObservableCollection<GithubRelease> AvailableVersions { get; } = [];
    public ObservableCollection<KeyValuePair<string, string>> AvailableMirrors { get; } = [];

    public IAsyncRelayCommand ChangeEftInstallLocationCommand { get; }

    public ConfigureSitViewModel(IInstallerService installerService, IPickerDialogService pickerDialogService) : base()
    {
        _installerService = installerService;
        _pickerDialogService = pickerDialogService;

        ChangeEftInstallLocationCommand = new AsyncRelayCommand(ChangeEftInstallLocation);

        this.WhenActivated(async (CompositeDisposable disposables) => await LoadAvailableVersionData());
    }

    private async Task ChangeEftInstallLocation()
    {
        IStorageFolder? directorySelected = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (directorySelected != null)
        {
            if (directorySelected.Path.LocalPath == CurrentInstallProcessState.BsgInstallPath)
            {
                // TODO show an error of some kind as we don't want the legit install to be the same as the SIT install.
            }
            else
            {
                CurrentInstallProcessState.EftInstallPath = directorySelected.Path.LocalPath;
                ValidateConfiguration();
            }
        }
    }

    private async Task LoadAvailableVersionData()
    {
        IsLoading = true;
        AvailableVersions.Clear();

        List<GithubRelease> releases = await _installerService.GetSITReleases();
        if (CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateSit)
        {
            // TODO filter results for updating SIT to versions higher than currently
        }

        AvailableVersions.AddRange(releases);
        if (AvailableVersions.Any())
        {
            SelectedVersion = AvailableVersions[0];
        }

        IsLoading = false;

        // TODO add some logging here and an alert somehow in case it fails to load any versions
    }

    private async Task LoadAvailableMirrorsForVersion()
    {
        IsLoading = true;
        AvailableMirrors.Clear();

        if (SelectedVersion == null)
        {
            return;
        }

        Dictionary<string, string>? availableMirrors = await _installerService.GetAvaiableMirrorsForVerison(SelectedVersion.body, CurrentInstallProcessState.EftVersion);
        if (availableMirrors != null)
        {
            if (availableMirrors.Count != 0)
            {
                // We got mirrors successfully so we can show them here
                AvailableMirrors.AddRange(availableMirrors);
                if (AvailableMirrors.Any())
                {
                    SelectedMirror = AvailableMirrors[0];
                }
                HasMirrorsAvailable = true;
            }
            else
            {
                HasMirrorsAvailable = false;
            }
        }
        else
        {
            HasMirrorsAvailable = false;

            // TODO there was an error of some kind getting the available mirrors so we need to do something for this
        }

        IsLoading = false;

        // TODO add some logging here and an alert somehow in case it fails to load any mirrors for this version
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

        if (IsLoading)
        {
            IsConfigurationValid = false;
            return;
        }
    }

    partial void OnSelectedVersionChanged(GithubRelease? value)
    {
        Task.Run(LoadAvailableMirrorsForVersion);
        ValidateConfiguration();
    }

    partial void OnSelectedMirrorChanged(KeyValuePair<string, string> value)
    {
        CurrentInstallProcessState.DownloadMirrorUrl = value.Value;
        ValidateConfiguration();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        ValidateConfiguration();
    }
}
