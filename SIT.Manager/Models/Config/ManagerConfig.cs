using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SIT.Manager.Models.Config;

public class ManagerConfig
{
    public LauncherConfig LauncherSettings { get; init; } = new();
    public LinuxConfig LinuxSettings { get; init; } = new();
    // Launcher Settings
    [ObservableProperty]
    private Color? _accentColor = Color.FromRgb(0x7f, 0x7f, 0x7f);
    [ObservableProperty]
    public bool _minimizeAfterLaunch = false;
    [ObservableProperty]
    public bool _closeAfterLaunch = false;
    [ObservableProperty]
    public string _currentLanguageSelected = CultureInfo.CurrentCulture.Name;
    [ObservableProperty]
    public bool _enableTestMode = false;
    [ObservableProperty]
    public bool _hideIpAddress = true;
    [ObservableProperty]
    private DateTime _lastManagerUpdateCheckTime = DateTime.MinValue;
    [ObservableProperty]
    public bool _lookForUpdates = true;

    public SITConfig SITSettings { get; init; } = new();

    public AkiConfig AkiSettings { get; init; } = new();
}
