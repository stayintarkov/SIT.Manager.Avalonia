using SIT.Manager.Avalonia.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Interfaces
{
    public interface IInstallerService
    {
        Task<bool> DownloadAndRunPatcher(string url);
        /// <summary>
        /// Gets a dictionary of all available download mirrors for a sit version
        /// </summary>
        /// <param name="sitVersionTarget">The version to look for mirrors of</param>
        /// <returns></returns>
        Task<Dictionary<string, string>?> GetAvaiableMirrorsForVerison(string sitVersionTarget);
        /// <summary>
        /// Fetch all of the currently available Server Releases
        /// </summary>
        /// <returns>A list of the available GitHub releases</returns>
        Task<List<GithubRelease>> GetServerReleases();
        /// <summary>
        /// Fetch all of the currently available SIT Releases
        /// </summary>
        /// <returns>A list of the available GitHub releases</returns>
        Task<List<GithubRelease>> GetSITReleases();
        /// <summary>
        /// Installs the selected SPT Server version
        /// </summary>
        /// <param name="selectedVersion">The <see cref="GithubRelease"/> to install</param>
        /// <returns></returns>
        Task InstallServer(GithubRelease selectedVersion);
        /// <summary>
        /// Installs the selected SIT version
        /// </summary>
        /// <param name="selectedVersion">The <see cref="GithubRelease"/> to install</param>
        /// <returns></returns>
        Task InstallSIT(GithubRelease selectedVersion);
    }
}
