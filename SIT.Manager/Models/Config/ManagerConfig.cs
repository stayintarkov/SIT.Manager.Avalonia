using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SIT.Manager.Models.Config;

public partial class ManagerConfig : ObservableObject
{
    // Launcher Settings
    [ObservableProperty]
    private Color? _accentColor = Color.FromRgb(0x7f, 0x7f, 0x7f);
    [ObservableProperty]
    public bool _closeAfterLaunch = false;
    [ObservableProperty]
    public string _currentLanguageSelected = CultureInfo.CurrentCulture.Name;
    [ObservableProperty]
    public bool _enableDeveloperMode = false;
    [ObservableProperty]
    public bool _hideIpAddress = true;
    [ObservableProperty]
    private DateTime _lastManagerUpdateCheckTime = DateTime.MinValue;
    [ObservableProperty]
    public bool _lookForUpdates = true;

    // Linux specific settings
    [ObservableProperty]
    public LinuxConfig _linuxConfig = new();

    // Mods settings
    [ObservableProperty]
    public bool _acceptedModsDisclaimer = false;
    public string ModCollectionVersion { get; set; } = string.Empty;
    public Dictionary<string, string> InstalledMods { get; set; } = [];

    // SIT settings
    [ObservableProperty]
    public string _installPath = string.Empty;
    [ObservableProperty]
    public string _tarkovVersion = string.Empty;
    [ObservableProperty]
    public string _sitVersion = string.Empty;
    [ObservableProperty]
    private DateTime _lastSitUpdateCheckTime = DateTime.MinValue;
    [ObservableProperty]
    public string _lastServer = "http://127.0.0.1:6969";
    [ObservableProperty]
    public string _username = string.Empty;
    [ObservableProperty]
    public string _password = string.Empty;
    [ObservableProperty]
    public bool _rememberLogin = false;
    public List<AkiServer> BookmarkedServers { get; set; } = [];

    // SPT-AKI settings
    [ObservableProperty]
    public string _akiServerPath = string.Empty;
    [ObservableProperty]
    public string _sptAkiVersion = string.Empty;
    [ObservableProperty]
    public string _sitModVersion = string.Empty;
    [ObservableProperty]
    private Color _consoleFontColor = Colors.LightBlue;
    [ObservableProperty]
    public string _consoleFontFamily = "Consolas";
}
