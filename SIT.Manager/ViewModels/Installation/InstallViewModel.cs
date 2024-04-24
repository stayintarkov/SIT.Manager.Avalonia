using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Installation;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Installation;

public partial class InstallViewModel : InstallationViewModelBase
{
    private readonly IInstallerService _installerService;
    private readonly ILogger<InstallViewModel> _logger;
    private readonly IModService _modService;

    private readonly Progress<double> _downloadProgress = new();
    private readonly Progress<double> _extractionProgress = new();

    // We have two install steps by default for SIT (downloading and extracting)
    private double _installSteps = 2;

    [ObservableProperty]
    private double _downloadProgressPercentage = 0;

    [ObservableProperty]
    private double _extractionProgressPercentage = 0;

    [ObservableProperty]
    private double _installProgressPercentage = 0;

    public InstallViewModel(IInstallerService installerService, ILogger<InstallViewModel> logger, IModService modService) : base()
    {
        _installerService = installerService;
        _logger = logger;
        _modService = modService;

        _installSteps += CalculateAdditionalInstallSteps();

        _downloadProgress.ProgressChanged += DownloadProgress_ProgressChanged;
        _extractionProgress.ProgressChanged += ExtractionProgress_ProgressChanged;
    }

    private int CalculateAdditionalInstallSteps()
    {
        int additionalSteps = 0;

        // Count each mod install as an extra step so we adjust the progress bar scaling accordingly.
        additionalSteps += CurrentInstallProcessState.RequestedMods.Count;

        // Add an extra step if we are copying the user's eft settings.
        if (CurrentInstallProcessState.CopyEftSettings)
        {
            additionalSteps++;
        }

        return additionalSteps;
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
        InstallProgressPercentage = (DownloadProgressPercentage + ExtractionProgressPercentage) / _installSteps;
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
                await RunSitInstall();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install requested target");
            return;
        }

        ProgressInstall();
    }

    private async Task RunSitInstall()
    {
        await _installerService.InstallSit(CurrentInstallProcessState.RequestedVersion, CurrentInstallProcessState.EftInstallPath, _downloadProgress, _extractionProgress);

        if (CurrentInstallProcessState.CopyEftSettings)
        {
            _installerService.CopyEftSettings(CurrentInstallProcessState.EftInstallPath);

            // We copied settings so remove a step and force a progress bar recalculation
            _installSteps--;
            UpdateInstallProgress();
        }

        foreach (ModInfo mod in CurrentInstallProcessState.RequestedMods)
        {
            await _modService.InstallMod(CurrentInstallProcessState.EftInstallPath, mod, true, true);

            // We copied settings so remove a step and force a progress bar recalculation
            _installSteps--;
            UpdateInstallProgress();
        }
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
