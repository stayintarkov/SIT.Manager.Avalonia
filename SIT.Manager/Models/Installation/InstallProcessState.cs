using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Models.Github;
using System.Collections.Generic;

namespace SIT.Manager.Models.Installation;

public partial class InstallProcessState : ObservableObject
{
    // General / Shared Settings
    [ObservableProperty]
    private RequestedInstallOperation _requestedInstallOperation = RequestedInstallOperation.None;
    [ObservableProperty]
    private GithubRelease _requestedVersion = new();
    private List<ModInfo> _requestedMods = [];

    // EFT Install Settings
    [ObservableProperty]
    private bool _usingBsgInstallPath = false;
    [ObservableProperty]
    private string _bsgInstallPath = string.Empty;
    [ObservableProperty]
    private string _eftInstallPath = string.Empty;
    [ObservableProperty]
    private string _eftVersion = string.Empty;
    [ObservableProperty]
    private string _sitVersion = string.Empty;
    [ObservableProperty]
    private string _downloadMirrorUrl = string.Empty;
    [ObservableProperty]
    private bool _copyEftSettings = true;

    // SPT-AKI Install Settings
    [ObservableProperty]
    private string _sptAkiInstallPath = string.Empty;
    [ObservableProperty]
    private string _sptAkiVersion = string.Empty;
    [ObservableProperty]
    private string _sitModVersion = string.Empty;
}
