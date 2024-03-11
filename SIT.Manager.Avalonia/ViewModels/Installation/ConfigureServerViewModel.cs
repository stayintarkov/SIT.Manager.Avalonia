using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

public partial class ConfigureServerViewModel : ViewModelBase
{
    private readonly IInstallerService _installerService;

    [ObservableProperty]
    private InstallProcessState _currentInstallProcessState;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private GithubRelease? _selectedVersion;

    [ObservableProperty]
    private bool _isConfigurationValid = false;

    [ObservableProperty]
    private string _serverInstallPath = string.Empty;

    public ObservableCollection<GithubRelease> AvailableVersions { get; } = [];

    public IAsyncRelayCommand ChangeServerInstallLocationCommand { get; }

    public ConfigureServerViewModel(IInstallerService installerService)
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

        ServerInstallPath = CurrentInstallProcessState.SptAkiInstallPath;

        ChangeServerInstallLocationCommand = new AsyncRelayCommand(ChangeServerInstallLocation);

        this.WhenActivated(async (CompositeDisposable disposables) => await LoadAvailableVersionData());
    }

    private async Task ChangeServerInstallLocation()
    {
        // TODO set this to change install location properly.
    }

    private async Task LoadAvailableVersionData()
    {
        IsLoading = true;
        AvailableVersions.Clear();

        List<GithubRelease> releases = await _installerService.GetServerReleases();
        if (CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateServer)
        {
            // TODO filter results for updating server to versions higher than currently
        }

        AvailableVersions.AddRange(releases);
        if (AvailableVersions.Any())
        {
            SelectedVersion = AvailableVersions[0];
        }

        IsLoading = false;

        // TODO add some logging here and an alert somehow in case it fails to load any versions
    }

    [RelayCommand]
    private void Back()
    {
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(false));
    }

    [RelayCommand]
    private void Start()
    {
        // TODO make sure this is disabled until we have selections for all the necessary items
        WeakReferenceMessenger.Default.Send(new InstallProcessStateChangedMessage(CurrentInstallProcessState));
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(ServerInstallPath))
        {
            IsConfigurationValid = false;
            return;
        }
    }

    partial void OnSelectedVersionChanged(GithubRelease? value)
    {
        ValidateConfiguration();
    }
}
