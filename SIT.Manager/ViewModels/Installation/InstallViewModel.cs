using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Installation;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Installation;

public partial class InstallViewModel : InstallationViewModelBase
{
    private readonly IInstallerService _installerService;
    private readonly ILogger<InstallViewModel> _logger;

    private readonly Progress<double> _downloadProgress = new();
    private readonly Progress<double> _extractionProgress = new();

    [ObservableProperty]
    private double _downloadProgressPercentage = 0;

    [ObservableProperty]
    private double _extractionProgressPercentage = 0;

    [ObservableProperty]
    private double _installProgressPercentage = 0;

    public InstallViewModel(IInstallerService installerService, ILogger<InstallViewModel> logger) : base()
    {
        _installerService = installerService;
        _logger = logger;

        _downloadProgress.ProgressChanged += DownloadProgress_ProgressChanged;
        _extractionProgress.ProgressChanged += ExtractionProgress_ProgressChanged;
    }

    private void DownloadProgress_ProgressChanged(object? sender, double e)
    {
        DownloadProgressPercentage = e;
        UpdateInstallProgress();
    }

    private void ExtractionProgress_ProgressChanged(object? sender, double e)
    {
        ExtractionProgressPercentage = e;
        UpdateInstallProgress();
    }

    private void UpdateInstallProgress()
    {
        InstallProgressPercentage = (DownloadProgressPercentage + ExtractionProgressPercentage) / 2;
    }

    private async Task RunInstaller()
    {
        DownloadProgressPercentage = 0;
        ExtractionProgressPercentage = 0;

        try
        {
            if (IsServerInstall)
            {
                await _installerService.InstallServer(CurrentInstallProcessState.RequestedVersion, CurrentInstallProcessState.SptAkiInstallPath, _downloadProgress, _extractionProgress);
            }
            else if (IsSitInstall)
            {
                await _installerService.InstallSit(CurrentInstallProcessState.RequestedVersion, CurrentInstallProcessState.EftInstallPath, _downloadProgress, _extractionProgress);

                if (CurrentInstallProcessState.CopyEftSettings)
                {
                    _installerService.CopyEftSettings(CurrentInstallProcessState.EftInstallPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install requested target");
            return;
        }

        ProgressInstall();
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        Messenger.Send(new InstallationRunningMessage(true));

        await RunInstaller();
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        Messenger.Send(new InstallationRunningMessage(false));
    }
}
