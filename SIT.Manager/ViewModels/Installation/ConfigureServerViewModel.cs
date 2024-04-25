using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Github;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Installation;

public partial class ConfigureServerViewModel : InstallationViewModelBase
{
    private readonly IInstallerService _installerService;
    private readonly ILogger<ConfigureServerViewModel> _logger;
    private readonly IPickerDialogService _pickerDialogService;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private GithubRelease? _selectedVersion;

    [ObservableProperty]
    private bool _isConfigurationValid = false;

    [ObservableProperty]
    private bool _hasVersionsAvailable = false;

    public ObservableCollection<GithubRelease> AvailableVersions { get; } = [];

    public IAsyncRelayCommand ChangeServerInstallLocationCommand { get; }

    public ConfigureServerViewModel(IInstallerService installerService, ILogger<ConfigureServerViewModel> logger, IPickerDialogService pickerDialogService)
    {
        _installerService = installerService;
        _logger = logger;
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
        HasVersionsAvailable = false;

        List<GithubRelease> releases = await _installerService.GetServerReleases();
        AvailableVersions.AddRange(releases);

        if (AvailableVersions.Any())
        {
            SelectedVersion = AvailableVersions[0];
            HasVersionsAvailable = true;
        }
        else
        {
            _logger.LogWarning("Available SIT version count {availableVersions} and 0 marked as available to use so will display error message", AvailableVersions.Count);
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
        IsConfigurationValid = true;

        if (string.IsNullOrEmpty(CurrentInstallProcessState.SptAkiInstallPath))
        {
            IsConfigurationValid = false;
            return;
        }

        if (SelectedVersion == null || AvailableVersions.Count == 0)
        {
            IsConfigurationValid = false;
            return;
        }
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
