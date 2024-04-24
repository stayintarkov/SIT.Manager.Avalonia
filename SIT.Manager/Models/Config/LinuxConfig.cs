using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace SIT.Manager.Models.Config;

public partial class LinuxConfig : ObservableObject
{
    public static readonly string BaseDir = AppContext.BaseDirectory;
    public static readonly string RuntimeDir = Path.Combine(BaseDir, "runtime");

    [ObservableProperty]
    public string _winePrefix = string.Empty;
    [ObservableProperty]
    public string _wineRunner = string.Empty;
    [ObservableProperty]
    public Dictionary<string, string> _wineEnv = [];
    [ObservableProperty]
    public bool _isDXVKEnabled = true;
    [ObservableProperty]
    public bool _isVKD3DEnabled = false;
    [ObservableProperty]
    public bool _isD3DExtrasEnabled = false;
    [ObservableProperty]
    public bool _isDXVK_NVAPIEnabled = false;
    [ObservableProperty]
    public bool _isDGVoodoo2Enabled = false;
    [ObservableProperty]
    public bool _isEsyncEnabled = true;
    [ObservableProperty]
    public bool _isFsyncEnabled = false;
    [ObservableProperty]
    public bool _isWineFsrEnabled = false;
    [ObservableProperty]
    public bool _isMangoHudEnabled = false;
    [ObservableProperty]
    public bool _isGameModeEnabled = false;

    static LinuxConfig()
    {
        // make directories if they don't exist
        if (!Directory.Exists(RuntimeDir))
        {
            Directory.CreateDirectory(RuntimeDir);
        }
    }
}
