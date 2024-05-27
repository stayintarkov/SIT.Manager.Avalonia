using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;

namespace SIT.Manager.Models.Config;

public partial class LauncherConfig : ObservableObject
{
    [ObservableProperty] private Color? _accentColor = Color.FromRgb(0x7f, 0x7f, 0x7f);

    [ObservableProperty] private bool _closeAfterLaunch;

    [ObservableProperty] private string _currentLanguageSelected = CultureInfo.CurrentCulture.Name;

    [ObservableProperty] private bool _enableTestMode;

    [ObservableProperty] private bool _hideIpAddress = true;

    [ObservableProperty] private DateTime _lastManagerUpdateCheckTime = DateTime.MinValue;

    [ObservableProperty] private bool _lookForUpdates = true;
}
