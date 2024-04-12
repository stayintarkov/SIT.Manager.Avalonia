using Avalonia.Media;
using System.Collections.Generic;

namespace SIT.Manager.Linux.DLLManagers;

// https://github.com/lutris/lutris/blob/master/lutris/util/wine/dll_manager.py
// Licensed under the GNU General Public License v3.0
public abstract class DllManager
{
    public string ComponentName { get; }
    public string BaseDir { get; }
    public List<string> ManagedDlls { get; }
    public List<string> ManagedAppdataFiles { get; } = [];
    public string VersionsPath { get; }
    public string ReleaseUrl { get; }
    public static readonly Dictionary<int, string> Archs = new();
    
    static DllManager()
    {
        Archs[32] = "x32";
        Archs[64] = "x64";
    }
    
    protected DllManager(string componentName, string baseDir, List<string> managedDlls, string versions_path, string release_url)
    {
        ComponentName = componentName;
        BaseDir = baseDir;
        ManagedDlls = managedDlls;
        VersionsPath = versions_path;
        ReleaseUrl = release_url;
        
       
    }
}
