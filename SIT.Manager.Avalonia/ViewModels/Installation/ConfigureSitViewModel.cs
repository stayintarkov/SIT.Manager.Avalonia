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

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private GithubRelease? _selectedVersion;

    [ObservableProperty]
    private KeyValuePair<string, string> _selectedMirror = new();

    [ObservableProperty]
    public bool _isConfigurationValid = false;

    public ObservableCollection<GithubRelease> AvailableVersions { get; } = [];
    public ObservableCollection<KeyValuePair<string, string>> AvailableMirrors { get; } = [];

    public IAsyncRelayCommand ChangeEftInstallLocationCommand { get; }

    public ConfigureSitViewModel(IInstallerService installerService) : base()
    {
        _installerService = installerService;

        ChangeEftInstallLocationCommand = new AsyncRelayCommand(ChangeEftInstallLocation);

        this.WhenActivated(async (CompositeDisposable disposables) => await LoadAvailableVersionData());
    }

    private async Task ChangeEftInstallLocation()
    {
        // TODO set this to change install location properly.
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
            AvailableMirrors.AddRange(availableMirrors);
            if (AvailableMirrors.Any())
            {
                CurrentInstallProcessState.DownloadMirror = AvailableMirrors[0];
            }
        }

        IsLoading = false;

        // TODO add some logging here and an alert somehow in case it fails to load any mirrors for this version
    }

    [RelayCommand]
    private void Back()
    {
        RegressInstall();
    }

    // TODO make sure this is disabled until we have selections for all the necessary items
    [RelayCommand]
    private void Start()
    {
        ProgressInstall();
    }

    private void ValidateConfiguration()
    {
        // TODO
    }

    partial void OnSelectedVersionChanged(GithubRelease? value)
    {
        Task.Run(LoadAvailableMirrorsForVersion);
        ValidateConfiguration();
    }

    partial void OnSelectedMirrorChanged(KeyValuePair<string, string> value)
    {
        CurrentInstallProcessState.DownloadMirror = value;
        ValidateConfiguration();
    }
}
