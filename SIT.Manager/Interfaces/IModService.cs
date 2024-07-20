using SIT.Manager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;

public interface IModService
{
    const string DISABLED_MODS_DIR = "DisabledMods";

    /// <summary>
    /// Checks if all the dlls required to support mods are installed
    /// </summary>
    /// <param name="targetPath">Base location of where the EFT install check for compatibility</param>
    /// <returns>True if all the DLLs are installed; otherwise false</returns>
    bool CheckModCompatibilityLayerInstalled(string targetPath);
    /// <summary>
    /// Disable a mod which is currently active for the users EFT
    /// </summary>
    /// <param name="mod">Metadata about the mod to disable</param>
    /// <param name="eftDir">The users base EFT directory</param>
    /// <returns>The updated ModInfo object</returns>
    ModInfo DisableMod(ModInfo mod, string eftDir);
    /// <summary>
    /// Enable a currently disabled mod for the users EFT
    /// </summary>
    /// <param name="mod">Metadata about the mod to enable</param>
    /// <param name="eftDir">The users base EFT directory</param>
    /// <returns>The updated ModInfo object</returns>
    ModInfo EnableMod(ModInfo mod, string eftDir);
    /// <summary>
    /// Looks in the configured EFT folder and evaluates what mods are currently installed.
    /// </summary>
    /// <param name="targetPath">Base location of where the EFT install check for installed mods</param>
    /// <returns>List of mods currently installed</returns>
    List<ModInfo> GetInstalledMods(string targetPath);
    /// <summary>
    /// Downloads (unless it's cached already) and installs the latest configuration manager from GitHub
    /// </summary>
    /// <param name="targetPath">Base location of where the EFT install is to put the mod</param>
    /// <returns>True if it was successfully installed; otherwise False</returns>
    Task<bool> InstallConfigurationManager(string targetPath);
    /// <summary>
    /// Downloads (unless it's cached already) and installs the latest Mod compatibility layer
    /// </summary>
    /// <param name="targetPath">Base location of where the EFT install is to put the mod compat layer</param>
    /// <returns>True if it was successfully installed; otherwise False</returns>
    Task<bool> InstallModCompatLayer(string targetPath);
}
