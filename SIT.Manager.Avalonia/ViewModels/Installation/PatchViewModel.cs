using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Services;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class PatchViewModel : InstallationViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IInstallerService _installerService;

    private readonly Progress<double> _copyProgress = new();
    private readonly Progress<double> _downloadProgress = new();
    private readonly Progress<double> _extractionProgress = new();

    [ObservableProperty]
    private double _copyProgressPercentage = 0;

    [ObservableProperty]
    private double _downloadProgressPercentage = 0;

    [ObservableProperty]
    private double _extractionProgressPercentage = 0;

    [ObservableProperty]
    private bool _requiresPatching = false;

    public PatchViewModel(IFileService fileService, IInstallerService installerService) : base()
    {
        _fileService = fileService;
        _installerService = installerService;

        RequiresPatching = !string.IsNullOrEmpty(CurrentInstallProcessState.DownloadMirrorUrl);

        _copyProgress.ProgressChanged += CopyProgress_ProgressChanged;
        _downloadProgress.ProgressChanged += DownloadProgress_ProgressChanged;
        _extractionProgress.ProgressChanged += ExtractionProgress_ProgressChanged;

        this.WhenActivated(async (CompositeDisposable disposables) => await DownloadAndRunPatcher());
    }

    private void CopyProgress_ProgressChanged(object? sender, double e)
    {
        CopyProgressPercentage = e;
    }

    private void DownloadProgress_ProgressChanged(object? sender, double e)
    {
        DownloadProgressPercentage = e;
    }

    private void ExtractionProgress_ProgressChanged(object? sender, double e)
    {
        ExtractionProgressPercentage = e;
    }

    public async Task DownloadAndRunPatcher()
    {
        if (CurrentInstallProcessState.UsingBsgInstallPath)
        {
            await _fileService.CopyDirectory(CurrentInstallProcessState.BsgInstallPath, CurrentInstallProcessState.EftInstallPath, _copyProgress);
        }

        if (RequiresPatching)
        {
            await _installerService.DownloadAndExtractPatcher(CurrentInstallProcessState.DownloadMirrorUrl, CurrentInstallProcessState.EftInstallPath, _downloadProgress, _extractionProgress);
            // TODO Run Patcher
            // TODO show error message on failure
            // TODO progress on success
        }
        else
        {
            ProgressInstall();
        }
    }
}
