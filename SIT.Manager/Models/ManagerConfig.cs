using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SIT.Manager.Models;

public partial class ManagerConfig : ObservableObject
{
    [ObservableProperty]
    public string _lastServer = "http://127.0.0.1:6969";
    [ObservableProperty]
    public string _username = string.Empty;
    [ObservableProperty]
    public string _password = string.Empty;
    [ObservableProperty]
    public string _installPath = string.Empty;
    [ObservableProperty]
    public string _akiServerPath = string.Empty;
    [ObservableProperty]
    public bool _rememberLogin = false;
    [ObservableProperty]
    public bool _closeAfterLaunch = false;
    [ObservableProperty]
    public string _winePrefix = string.Empty;
    [ObservableProperty]
    public string _wineRunner = string.Empty;
    [ObservableProperty]
    public string _tarkovVersion = string.Empty;
    [ObservableProperty]
    public string _sitVersion = string.Empty;
    [ObservableProperty]
    private DateTime _lastSitUpdateCheckTime = DateTime.MinValue;
    [ObservableProperty]
    public string _sptAkiVersion = string.Empty;
    [ObservableProperty]
    public string _sitModVersion = string.Empty;
    [ObservableProperty]
    public bool _lookForUpdates = true;
    [ObservableProperty]
    public string _currentLanguageSelected = CultureInfo.CurrentCulture.Name;
    [ObservableProperty]
    public bool _hideIpAddress = true;
    [ObservableProperty]
    public bool _acceptedModsDisclaimer = false;
    public string ModCollectionVersion { get; set; } = string.Empty;
    public Dictionary<string, string> InstalledMods { get; set; } = [];
    [ObservableProperty]
    private Color _consoleFontColor = Colors.LightBlue;
    [ObservableProperty]
    private Color? _accentColor = Color.FromRgb(0x7f, 0x7f, 0x7f); // 7f7f7f 
    [ObservableProperty]
    public string _consoleFontFamily = "Consolas";
}
