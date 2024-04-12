using SIT.Manager.Models;

namespace SIT.Manager.Linux.DLLManagers;

public class DXVKManager : DllManager
{
    // TODO: Fix basedir
    public DXVKManager() : base("DXVK", 
        System.IO.Path.Join(LinuxConfig.RuntimeDir, "dxvk"),
        ["dxgi", "d3d11", "d3d10core", "d3d9"], 
        System.IO.Path.Join(LinuxConfig.BaseDir, "versions", "dxvk_versions.json"),
        "https://api.github.com/repos/lutris/dxvk/releases")
    {
        
    }
}
