using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models.Installation;
using System;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class SelectViewModel : ViewModelBase
{
    private readonly IManagerConfigService _configService;
    private readonly IInstallerService _installerService;
    private readonly IVersionService _versionService;

    [ObservableProperty]
    private InstallProcessState _currentInstallProcessState;

    [ObservableProperty]
    private bool _noEftInstallPathSet = true;

    [ObservableProperty]
    private bool _noAkiInstallPathSet = true;

    public SelectViewModel(IManagerConfigService configsService,
                           IInstallerService installerService,
                           IVersionService versionService)
    {
        _configService = configsService;
        _installerService = installerService;
        _versionService = versionService;

        try
        {
            CurrentInstallProcessState = WeakReferenceMessenger.Default.Send<InstallProcessStateRequestMessage>();
        }
        catch
        {
            CurrentInstallProcessState = new();
        }

        EstablishEFTInstallStatus();
        EstablishSptAkiInstallStatus();
    }

    private void EstablishEFTInstallStatus()
    {
        string detectedBSGInstallPath = _installerService.GetEFTInstallPath();
        if (string.IsNullOrEmpty(_configService.Config.InstallPath))
        {
            if (string.IsNullOrEmpty(detectedBSGInstallPath))
            {
                NoEftInstallPathSet = true;
            }
            else
            {
                CurrentInstallProcessState.EftInstallPath = detectedBSGInstallPath;
                CurrentInstallProcessState.UsingBsgInstallPath = true;
            }
        }
        else
        {
            CurrentInstallProcessState.EftInstallPath = _configService.Config.InstallPath;
            CurrentInstallProcessState.UsingBsgInstallPath = false;
        }

        if (!NoEftInstallPathSet)
        {
            CurrentInstallProcessState.EftVersion = Version.Parse(_versionService.GetEFTVersion(CurrentInstallProcessState.EftInstallPath));
            CurrentInstallProcessState.SitVersion = Version.Parse(_versionService.GetSITVersion(CurrentInstallProcessState.EftInstallPath));
        }
    }

    private void EstablishSptAkiInstallStatus()
    {
        if (!string.IsNullOrEmpty(_configService.Config.AkiServerPath))
        {
            CurrentInstallProcessState.SptAkiInstallPath = _configService.Config.AkiServerPath;
            CurrentInstallProcessState.SptAkiVersion = Version.Parse(_versionService.GetSptAkiVersion(CurrentInstallProcessState.SptAkiInstallPath));
            CurrentInstallProcessState.SitModVersion = Version.Parse(_versionService.GetSitModVersion(CurrentInstallProcessState.SptAkiInstallPath));
            NoAkiInstallPathSet = false;
        }
    }

    [RelayCommand]
    private void ProgressInstall(RequestedInstallOperation? requestedOperation)
    {
        if (requestedOperation != null)
        {
            CurrentInstallProcessState.RequestedInstallOperation = (RequestedInstallOperation) requestedOperation;
            WeakReferenceMessenger.Default.Send(new InstallProcessStateChangedMessage(CurrentInstallProcessState));
            WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
        }
    }
}
