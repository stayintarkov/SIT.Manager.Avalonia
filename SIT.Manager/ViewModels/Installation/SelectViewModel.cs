using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Installation;
using System.IO;

namespace SIT.Manager.ViewModels.Installation;

public partial class SelectViewModel(IManagerConfigService configsService,
                       IInstallerService installerService,
                       ILogger<SelectViewModel> logger,
                       IVersionService versionService) : InstallationViewModelBase()
{
    private readonly IManagerConfigService _configService = configsService;
    private readonly IInstallerService _installerService = installerService;
    private readonly ILogger<SelectViewModel> _logger = logger;
    private readonly IVersionService _versionService = versionService;

    [ObservableProperty]
    private bool _noAkiInstallPathSet = true;

    [ObservableProperty]
    private bool _hasSitUpdateAvailable = false;

    private void EstablishEFTInstallStatus()
    {
        string? sitEFTInstallPath = _configService.Config.SITSettings.SitEFTInstallPath;
        if (string.IsNullOrEmpty(sitEFTInstallPath))
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
            CurrentInstallProcessState.EftInstallPath = sitEFTInstallPath;
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
        string? akiServerPath = _configService.Config.AkiSettings.AkiServerPath;
        if (!string.IsNullOrEmpty(akiServerPath))
        {
            CurrentInstallProcessState.SptAkiInstallPath = akiServerPath;

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
            _logger.LogInformation("Progressing install to Configure page with requested operation of {requestedOperation}", requestedOperation);
            _logger.LogDebug("Install process state {CurrentInstallProcessState}", CurrentInstallProcessState);
            ProgressInstall();
        }
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        EstablishEFTInstallStatus();
        EstablishSptAkiInstallStatus();

        HasSitUpdateAvailable = await _installerService.IsSitUpdateAvailable();
    }
}
