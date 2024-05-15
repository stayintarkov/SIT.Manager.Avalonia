using SIT.Manager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;

public interface IModService
{
    /// <summary>
    /// Array of recommended mod install names.
    /// </summary>
    string[] RecommendedModInstalls { get; }
    /// <summary>
    /// The currently loaded list of mods which we have available to install/uninstall
    /// </summary>
    List<ModInfo> ModList { get; }

    /// <summary>
    /// Automatically updates installed mods that are outdated.
    /// </summary>
    /// <param name="outdatedMods"><see cref="List{T}"/> of <see cref="ModInfo"/> that are outdated.</param>
    Task AutoUpdate(List<ModInfo> outdatedMods);
    /// <summary>
    /// Clears the locally downloaded cache of mods which will force a refresh of available mods.
    /// </summary>
    void ClearCache();
    /// <summary>
    /// Download the collection of ported and compatible (probably) SIT mods.
    /// </summary>
    /// <returns></returns>
    Task DownloadModsCollection();
    /// <summary>
    /// Downloads (unless it's cached already) and installs the latest configuration manager from GitHub
    /// </summary>
    /// <param name="targetPath">Base location of where the EFT install is to put the mod</param>
    /// <returns>True if it was successfully installed; otherwise False</returns>
    Task<bool> InstallConfigurationManager(string targetPath);
    /// <summary>
    /// Install a mod into the given target location
    /// </summary>
    /// <param name="targetPath">Base location of where the EFT install is to put the mod</param>
    /// <param name="mod">The meta data for the mod we want to install</param>
    /// <param name="suppressNotification">Supress notifications of the mods installation status</param>
    /// <param name="suppressCompatibilityWarning">Supress the warning about a mods compatibility</param>
    /// <returns>True if it was successfully installed; otherwise False</returns>
    Task<bool> InstallMod(string targetPath, ModInfo mod, bool suppressNotification = false, bool suppressCompatibilityWarning = false);
    /// <summary>
    /// Load the master list of mods into memory so we know what mods are available
    /// </summary>
    /// <returns></returns>
    Task LoadMasterModList();
    /// <summary>
    /// Uninstall a mod from the given target location
    /// </summary>
    /// <param name="mod">The meta data for the mod we want to uninstall</param>
    /// <param name="targetPath">The target location to search where the mod is installed</param>
    /// <returns></returns>
    Task<bool> UninstallMod(string targetPath, ModInfo mod);
}
