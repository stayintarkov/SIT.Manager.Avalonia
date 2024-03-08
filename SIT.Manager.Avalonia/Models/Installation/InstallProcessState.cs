using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace SIT.Manager.Avalonia.Models.Installation;

public partial class InstallProcessState : ObservableObject
{
    // General Settings
    [ObservableProperty]
    private RequestedInstallOperation _requestedInstallOperation = RequestedInstallOperation.None;

    // EFT Install Settings
    [ObservableProperty]
    private bool _usingBsgInstallPath = false;
    [ObservableProperty]
    private string _eftInstallPath = string.Empty;
    [ObservableProperty]
    private Version _eftVersion = new();
    [ObservableProperty]
    private Version _sitVersion = new();

    // SPT-AKI Install Settings
    [ObservableProperty]
    private string _sptAkiInstallPath = string.Empty;
    [ObservableProperty]
    private Version _sptAkiVersion = new();
    [ObservableProperty]
    private Version _sitModVersion = new();
}
