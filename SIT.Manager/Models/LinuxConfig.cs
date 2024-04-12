using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace SIT.Manager.Models;

public partial class LinuxConfig : ManagerConfig
{
    public static readonly string BaseDir = AppContext.BaseDirectory;
    public static readonly string RuntimeDir = Path.Combine(BaseDir, "runtime");
    
    [ObservableProperty]
    public bool _isDXVKEnabled = true;
    [ObservableProperty]
    public bool _isVKD3DEnabled = true;
    [ObservableProperty]
    public bool _isD3DExtrasEnabled = true;
    [ObservableProperty]
    public bool _isDXVK_NVAPIEnabled = true;
    [ObservableProperty]
    public bool _isDGVoodoo2Enabled = false;
    [ObservableProperty]
    public bool _isEsyncEnabled = true;
    [ObservableProperty]
    public bool _isFsyncEnabled = false;
    [ObservableProperty]
    public bool _isWineFsrEnabled = false;
    [ObservableProperty]
    public List<string> _dxvkVersions = [];
    [ObservableProperty]
    public int _selectedDxvkVersionIndex = 0;
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
