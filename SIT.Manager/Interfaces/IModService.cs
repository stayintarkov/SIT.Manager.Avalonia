using SIT.Manager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;

public interface IModService
{
    public List<ModInfo> ModList { get; }

    Task DownloadModsCollection();
    /// <summary>
    /// Automatically updates installed mods that are outdated.
    /// </summary>
    /// <param name="outdatedMods"><see cref="List{T}"/> of <see cref="ModInfo"/> that are outdated.</param>
    Task AutoUpdate(List<ModInfo> outdatedMods);
    Task<bool> InstallMod(ModInfo mod, bool suppressNotification = false);
    /// <summary>
    /// Load the master list of mods into memory so we know what mods are available
    /// </summary>
    /// <returns></returns>
    Task LoadMasterModList();
    Task<bool> UninstallMod(ModInfo mod);
}
