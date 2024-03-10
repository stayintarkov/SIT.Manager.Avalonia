using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Installation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class ConfigureViewModel : ViewModelBase
{
    private readonly IInstallerService _installerService;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private InstallProcessState _currentInstallProcessState;

    [ObservableProperty]
    private bool _isLoadingVersionSelection = false;

    [ObservableProperty]
    private GithubRelease? _selectedVersion;

    public ObservableCollection<GithubRelease> AvailableVersions { get; } = [];
    public ObservableCollection<KeyValuePair<string, string>> AvailableMirrors { get; } = [];

    public ConfigureViewModel(IInstallerService installerService)
    {
        _installerService = installerService;

        try
        {
            CurrentInstallProcessState = WeakReferenceMessenger.Default.Send<InstallProcessStateRequestMessage>();
        }
        catch
        {
            CurrentInstallProcessState = new();
        }

        Task.Run(LoadAvailableVersionData);
    }

    private async Task LoadAvailableVersionData()
    {
        AvailableVersions.Clear();

        List<GithubRelease> releases;
        if (CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallSit || CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateSit)
        {
            releases = await _installerService.GetSITReleases();
        }
        else if (CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallServer || CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateServer)
        {
            releases = await _installerService.GetServerReleases();
        }
        else
        {
            return;
        }

        AvailableVersions.AddRange(releases);
        if (AvailableVersions.Any())
        {
            SelectedVersion = AvailableVersions[0];
        }

        // TODO add some logging here and an alert somehow in case it fails to load any versions
    }

    private async Task LoadAvailableMirrorsForVersion()
    {
        if (SelectedVersion == null)
        {
            return;
        }

        Dictionary<string, string>? availableMirrors = await _installerService.GetAvaiableMirrorsForVerison(SelectedVersion.body);
        if (availableMirrors != null)
        {
            AvailableMirrors.AddRange(availableMirrors);
            if (AvailableMirrors.Any())
            {
                CurrentInstallProcessState.DownloadMirror = AvailableMirrors[0];
            }
        }

        // TODO add some logging here and an alert somehow in case it fails to load any mirrors for this version
    }

    [RelayCommand]
    private void Start()
    {
        WeakReferenceMessenger.Default.Send(new InstallProcessStateChangedMessage(CurrentInstallProcessState));
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
    }

    partial void OnSelectedVersionChanged(GithubRelease? value)
    {
        Task.Run(LoadAvailableMirrorsForVersion);
    }
}
