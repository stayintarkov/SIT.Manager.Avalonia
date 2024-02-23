using SIT.Manager.Avalonia.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Interfaces
{
    public interface IModService
    {
        Task DownloadModsCollection();
        /// <summary>
        /// Automatically updates installed mods that are outdated.
        /// </summary>
        /// <param name="outdatedMods"><see cref="List{T}"/> of <see cref="ModInfo"/> that are outdated.</param>
        Task AutoUpdate(List<ModInfo> outdatedMods);
        Task<bool> InstallMod(ModInfo mod, bool suppressNotification = false);
        Task<bool> UninstallMod(ModInfo mod);
    }
}
