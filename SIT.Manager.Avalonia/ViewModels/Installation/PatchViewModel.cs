using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Models.Installation;
using SIT.Manager.Avalonia.Services;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class PatchViewModel : InstallationViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IInstallerService _installerService;

    private Progress<double> _copyProgress = new();
    private Progress<double> _downloadProgress = new();
    private Progress<double> _extractionProgress = new();

    [ObservableProperty]
    private double _copyProgressPercentage = 0;

    [ObservableProperty]
    private double _downloadProgressPercentage = 0;

    [ObservableProperty]
    private double _extractionProgressPercentage = 0;

    public PatchViewModel(IFileService fileService, IInstallerService installerService) : base()
    {
        _fileService = fileService;
        _installerService = installerService;

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

        await _installerService.DownloadAndExtractPatcher(CurrentInstallProcessState.DownloadMirrorUrl, CurrentInstallProcessState.EftInstallPath, _downloadProgress, _extractionProgress);

        // TODO Run Patcher
        // TODO progress on success
        // TODO show error message on failure
    }

    [RelayCommand]
    private void Progress()
    {
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
    }
}
