using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Avalonia.Extentions;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Installation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class ConfigureServerViewModel : InstallationViewModelBase
{
    private readonly IInstallerService _installerService;
    private readonly IPickerDialogService _pickerDialogService;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private GithubRelease? _selectedVersion;

    [ObservableProperty]
    private bool _isConfigurationValid = false;

    public ObservableCollection<GithubRelease> AvailableVersions { get; } = [];

    public IAsyncRelayCommand ChangeServerInstallLocationCommand { get; }

    public ConfigureServerViewModel(IInstallerService installerService, IPickerDialogService pickerDialogService)
    {
        _installerService = installerService;
        _pickerDialogService = pickerDialogService;

        ChangeServerInstallLocationCommand = new AsyncRelayCommand(ChangeServerInstallLocation);
    }

    private async Task ChangeServerInstallLocation()
    {
        IStorageFolder? directorySelected = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (directorySelected != null)
        {
            CurrentInstallProcessState.SptAkiInstallPath = directorySelected.Path.LocalPath;
            ValidateConfiguration();
        }
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
        else
        {
            // TODO add some logging here and an alert somehow in case it fails to load any versions
        }

        IsLoading = false;
        ValidateConfiguration();
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
        if (string.IsNullOrEmpty(CurrentInstallProcessState.SptAkiInstallPath))
        {
            IsConfigurationValid = false;
            return;
        }
        if (SelectedVersion == null)
        {
            IsConfigurationValid = false;
        }
        IsConfigurationValid = true;
    }

    protected override async void OnActivated()
    {
        base.OnActivated();
        await LoadAvailableVersionData();
    }

    partial void OnSelectedVersionChanged(GithubRelease? value)
    {
        if (value != null)
        {
            CurrentInstallProcessState.RequestedVersion = value;
            ValidateConfiguration();
        }
    }
}
