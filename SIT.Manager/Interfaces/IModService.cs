using SIT.Manager.Models;
using SIT.Manager.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;

public interface IModService
{
    Task DownloadModsCollection();
    /// <summary>
    /// Automatically updates installed mods that are outdated.
    /// </summary>
    /// <param name="outdatedMods"><see cref="List{T}"/> of <see cref="ModInfo"/> that are outdated.</param>
    Task AutoUpdate(List<ModInfo> outdatedMods, List<ModInfo> modList);
    Task<bool> InstallMod(ModInfo mod, List<ModInfo> modList, bool suppressNotification = false, bool installDependenciesWithoutConfirm = false, bool includeAdditionalFiles = false);
    Task<bool> UninstallMod(ModInfo mod);
    Task<bool> InstallAdditionalModFiles(ModInfo mod);
}
