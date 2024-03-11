using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace SIT.Manager.Avalonia.Models.Installation;

public partial class InstallProcessState : ObservableObject
{
    // General / Shared Settings
    [ObservableProperty]
    private RequestedInstallOperation _requestedInstallOperation = RequestedInstallOperation.None;
    [ObservableProperty]
    private GithubRelease _requestedVersion = new();

    // EFT Install Settings
    [ObservableProperty]
    private bool _usingBsgInstallPath = false;
    [ObservableProperty]
    private string _eftInstallPath = string.Empty;
    [ObservableProperty]
    private Version _eftVersion = new();
    [ObservableProperty]
    private Version _sitVersion = new();
    [ObservableProperty]
    private KeyValuePair<string, string> _downloadMirror = new();

    // SPT-AKI Install Settings
    [ObservableProperty]
    private string _sptAkiInstallPath = string.Empty;
    [ObservableProperty]
    private Version _sptAkiVersion = new();
    [ObservableProperty]
    private Version _sitModVersion = new();
}
