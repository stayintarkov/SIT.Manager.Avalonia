using SIT.Manager.Avalonia.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Interfaces
{
    public interface IInstallerService
    {
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
