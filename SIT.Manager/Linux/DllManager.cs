using SIT.Manager.Linux.Managers;
using SIT.Manager.Models;
using System.Collections.Generic;
using System.Linq;

namespace SIT.Manager.Linux;

public abstract class DllManager(string component, IEnumerable<string> dlls, string baseDir, string releaseUrl, IEnumerable<string>? managedAppDataFiles=null)
{
    public readonly string Component = component;
    protected string BaseDir = baseDir;
    protected IEnumerable<string>? ManagedAppDataFiles = managedAppDataFiles; // TODO: implement this
    protected string ReleaseUrl = releaseUrl; // TODO: do something with this
    
    private static readonly Dictionary<string, DllManager> Managers = [];
    
    static DllManager()
    {
        Managers.Add("DXVK", new DxvkManager());
        Managers.Add("VKD3D", new Vkd3DManager());
        Managers.Add("D3DExtras", new D3DExtrasManager());
        // // TODO: implement these \/ \/ \/
        Managers.Add("DXVK-NVAPI", new DxvkNvapiManager());
        Managers.Add("dgvoodoo2", new Dgvoodoo2Manager());
    }

    private string GetDllOverrideString()
    {
        return dlls.Aggregate("", (current, dll) => current + "," + dll).Remove(0, 1);
    }
    
    public static string GetDllOverride(LinuxConfig config)
    {
        // Collect every DLL override string from every manager and collect them per Mode if the setting is enabled
        // Then all dlls with the same mode are grouped together and separated by a comma with the last one ending with an = sign and then the mode and a semicolon
        // Example: "d3d9=,d3d10,d3d10core,d3d11,d3d12,dxgi=n;other_dlls,...,="
        // If no DLLs are enabled for a mode, it will be omitted
        string result = "";
        // TODO: There must be a better way to do this, right?
        if (config.IsDXVKEnabled)
        {
            result += Managers["DXVK"].GetDllOverrideString();
        }
        if (config.IsVKD3DEnabled)
        {
            result += Managers["VKD3D"].GetDllOverrideString();
        }
        if (config.IsD3DExtrasEnabled)
        {
            result += Managers["D3DExtras"].GetDllOverrideString();
        }
        if (config.IsDXVK_NVAPIEnabled)
        {
            result += Managers["DXVK-NVAPI"].GetDllOverrideString();
        }
        if (config.IsDGVoodoo2Enabled)
        {
            result += Managers["dgvoodoo2"].GetDllOverrideString();
        }
        
        return string.IsNullOrEmpty(result) ? "winemenubuilder=" : result + "=n;winemenubuilder=" ;
    }
}
