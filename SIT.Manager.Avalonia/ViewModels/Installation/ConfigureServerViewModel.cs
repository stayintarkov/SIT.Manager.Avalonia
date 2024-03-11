using Avalonia.Platform.Storage;
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
    private readonly IPickerDialogService _pickerDialogService;

    [ObservableProperty]
    private InstallProcessState _currentInstallProcessState;

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

        try
        {
            CurrentInstallProcessState = WeakReferenceMessenger.Default.Send<InstallProcessStateRequestMessage>();
        }
        catch
        {
            CurrentInstallProcessState = new();
        }

        ChangeServerInstallLocationCommand = new AsyncRelayCommand(ChangeServerInstallLocation);

        this.WhenActivated(async (CompositeDisposable disposables) => await LoadAvailableVersionData());
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
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(false));
    }

    [RelayCommand]
    private void Start()
    {
        WeakReferenceMessenger.Default.Send(new InstallProcessStateChangedMessage(CurrentInstallProcessState));
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
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

    partial void OnSelectedVersionChanged(GithubRelease? value)
    {
        if (value != null)
        {
            CurrentInstallProcessState.RequestedVersion = value;
            ValidateConfiguration();
        }
    }
}
