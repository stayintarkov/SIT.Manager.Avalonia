using SIT.Manager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;

public interface IModService
{
    /// <summary>
    /// Looks in the configured EFT folder and evaluates what mods are currently installed.
    /// </summary>
    /// <returns>List of mods currently installed</returns>
    List<ModInfo> GetInstalledMods();
    /// <summary>
    /// Downloads (unless it's cached already) and installs the latest configuration manager from GitHub
    /// </summary>
    /// <param name="targetPath">Base location of where the EFT install is to put the mod</param>
    /// <returns>True if it was successfully installed; otherwise False</returns>
    Task<bool> InstallConfigurationManager(string targetPath);
}
