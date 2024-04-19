using SIT.Manager.Linux.Managers;
using SIT.Manager.Models.Config;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIT.Manager.Linux;

public abstract class DllManager(string component, IEnumerable<string> dlls, string baseDir, string releaseUrl, IEnumerable<string>? managedAppDataFiles = null)
{
    public readonly string Component = component;
    protected string BaseDir = baseDir;
    protected IEnumerable<string>? ManagedAppDataFiles = managedAppDataFiles; // TODO: implement this
    protected string ReleaseUrl = releaseUrl; // TODO: do something with this

    private static readonly Dictionary<string, DllManager> Managers = new()
    {
        { "IsDXVKEnabled", new DxvkManager() },
        { "IsVKD3DEnabled", new Vkd3DManager() },
        { "IsD3DExtrasEnabled", new D3DExtrasManager() },
        // // TODO: implement these \/ \/ \/
        { "IsDXVK_NVAPIEnabled", new DxvkNvapiManager() },
        { "IsDGVoodoo2Enabled", new Dgvoodoo2Manager() }
    };

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
        StringBuilder sb = new();
        foreach (KeyValuePair<string, DllManager> manager in Managers.Where(manager =>
                     config.GetType().GetProperty(manager.Key)?.GetValue(config) is true))
        {
            sb.Append(manager.Value.GetDllOverrideString());
        }
        string result = sb.ToString();

        return string.IsNullOrEmpty(result) ? "winemenubuilder=" : result + "=n;winemenubuilder=";
    }
}
