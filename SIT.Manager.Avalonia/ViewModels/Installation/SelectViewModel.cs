using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models.Installation;
using System;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class SelectViewModel : InstallationViewModelBase
{
    private readonly IManagerConfigService _configService;
    private readonly IInstallerService _installerService;
    private readonly IVersionService _versionService;

    [ObservableProperty]
    private bool _noEftInstallPathSet = true;

    [ObservableProperty]
    private bool _noAkiInstallPathSet = true;

    public SelectViewModel(IManagerConfigService configsService,
                           IInstallerService installerService,
                           IVersionService versionService) : base()
    {
        _configService = configsService;
        _installerService = installerService;
        _versionService = versionService;

        EstablishEFTInstallStatus();
        EstablishSptAkiInstallStatus();
    }

    private void EstablishEFTInstallStatus()
    {
        string detectedBSGInstallPath = _installerService.GetEFTInstallPath();
        if (string.IsNullOrEmpty(_configService.Config.InstallPath))
        {
            if (!string.IsNullOrEmpty(detectedBSGInstallPath))
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

        if (!string.IsNullOrEmpty(CurrentInstallProcessState.EftInstallPath))
        {
            if (Version.TryParse(_versionService.GetEFTVersion(CurrentInstallProcessState.EftInstallPath), out Version? eftVersion))
            {
                CurrentInstallProcessState.EftVersion = eftVersion;
            }
            else
            {
                CurrentInstallProcessState.EftVersion = new();
            }
            if (Version.TryParse(_versionService.GetSITVersion(CurrentInstallProcessState.EftInstallPath), out Version? sitVersion))
            {
                CurrentInstallProcessState.SitVersion = sitVersion;
            }
            else
            {
                CurrentInstallProcessState.SitVersion = new();
            }

            NoEftInstallPathSet = false;
        }
    }

    private void EstablishSptAkiInstallStatus()
    {
        if (!string.IsNullOrEmpty(_configService.Config.AkiServerPath))
        {
            CurrentInstallProcessState.SptAkiInstallPath = _configService.Config.AkiServerPath;

            if (Version.TryParse(_versionService.GetSptAkiVersion(CurrentInstallProcessState.SptAkiInstallPath), out Version? sptAkiVersion))
            {
                CurrentInstallProcessState.SptAkiVersion = sptAkiVersion;
            }
            else
            {
                CurrentInstallProcessState.SptAkiVersion = new();
            }
            if (Version.TryParse(_versionService.GetSitModVersion(CurrentInstallProcessState.SptAkiInstallPath), out Version? sitModVersion))
            {
                CurrentInstallProcessState.SitModVersion = sitModVersion;
            }
            else
            {
                CurrentInstallProcessState.SitModVersion = new();
            }

            NoAkiInstallPathSet = false;
        }
    }

    [RelayCommand]
    private void ProgressInstall(RequestedInstallOperation? requestedOperation)
    {
        if (requestedOperation != null)
        {
            CurrentInstallProcessState.RequestedInstallOperation = (RequestedInstallOperation) requestedOperation;
            ProgressInstall();
        }
    }
}
