using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models.Installation;
using System.IO;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class SelectViewModel : InstallationViewModelBase
{
    private readonly IManagerConfigService _configService;
    private readonly IInstallerService _installerService;
    private readonly IVersionService _versionService;

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
        if (string.IsNullOrEmpty(_configService.Config.InstallPath))
        {
            string detectedBSGInstallPath = Path.GetDirectoryName(_installerService.GetEFTInstallPath()) ?? string.Empty;
            if (!string.IsNullOrEmpty(detectedBSGInstallPath))
            {
                CurrentInstallProcessState.BsgInstallPath = detectedBSGInstallPath;
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
            CurrentInstallProcessState.EftVersion = _versionService.GetEFTVersion(CurrentInstallProcessState.EftInstallPath);
            CurrentInstallProcessState.SitVersion = _versionService.GetSITVersion(CurrentInstallProcessState.EftInstallPath);
        }
    }

    private void EstablishSptAkiInstallStatus()
    {
        if (!string.IsNullOrEmpty(_configService.Config.AkiServerPath))
        {
            CurrentInstallProcessState.SptAkiInstallPath = _configService.Config.AkiServerPath;

            CurrentInstallProcessState.SptAkiVersion = _versionService.GetSptAkiVersion(CurrentInstallProcessState.SptAkiInstallPath);
            CurrentInstallProcessState.SitModVersion = _versionService.GetSitModVersion(CurrentInstallProcessState.SptAkiInstallPath);

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
