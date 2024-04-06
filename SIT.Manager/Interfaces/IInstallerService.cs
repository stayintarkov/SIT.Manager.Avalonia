using SIT.Manager.Models;
using SIT.Manager.Models.Installation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;

public interface IInstallerService
{
    /// <summary>
    /// Creates the process which runs the downgrade patcher
    /// </summary>
    /// <returns>Process for the downpatcher</returns>
    Process CreatePatcherProcess(string patcherPath);
    /// <summary>
    /// Downloads and extract the patcher from the requested url providing progress as it goes
    /// </summary>
    /// <param name="url">The download mirror containing the required patcher</param>
    /// <param name="targetPath">The directory to download and extract the patcher into</param>
    /// <param name="downloadProgress"></param>
    /// <param name="extractionProgress"></param>
    /// <returns></returns>
    Task<bool> DownloadAndExtractPatcher(string url, string targetPath, IProgress<double> downloadProgress, IProgress<double> extractionProgress);
    /// <summary>
    /// Get a list of the available installs of SIT and the necesary information to download the downgrade patcher required.
    /// </summary>
    /// <param name="tarkovVersion">The current Escape from Tarkov version to find version for</param>
    /// <returns></returns>
    Task<List<SitInstallVersion>> GetAvailableSitReleases(string tarkovVersion);
    /// <summary>
    /// Gets the file path for the BSG Install of EFT if it is able to be detected
    /// </summary>
    /// <returns>The path to the BSG Install of EFT or string.Empty</returns>
    string GetEFTInstallPath();
    /// <summary>
    /// Get the most up to date version of SIT Install the user can run
    /// </summary>
    /// <returns></returns>
    SitInstallVersion? GetLatestAvailableSitRelease();
    /// <summary>
    /// Check to see if there is an update available for the currently installed version of SIT or not
    /// </summary>
    /// <returns>True if there is an update; otherwise False</returns>
    Task<bool> IsSitUpateAvailable();
    /// <summary>
    /// Fetch all of the currently available Server Releases
    /// </summary>
    /// <returns>A list of the available GitHub releases</returns>
    Task<List<GithubRelease>> GetServerReleases();
    /// <summary>
    /// Installs the selected SPT Server version reporting all progress on the way
    /// </summary>
    /// <param name="selectedVersion">The <see cref="GithubRelease"/> to install</param>
    /// <param name="targetInstallDir"></param>
    /// <param name="downloadProgress"></param>
    /// <param name="extractionProgress"></param>
    /// <returns></returns>
    Task InstallServer(GithubRelease selectedVersion, string targetInstallDir, IProgress<double> downloadProgress, IProgress<double> extractionProgress);
    /// <summary>
    /// Installs the selected SIT version
    /// </summary>
    /// <param name="selectedVersion">The <see cref="GithubRelease"/> to install</param>
    /// <param name="targetInstallDir"></param>
    /// <param name="downloadProgress"></param>
    /// <param name="extractionProgress"></param>
    /// <returns></returns>
    Task InstallSit(GithubRelease selectedVersion, string targetInstallDir, IProgress<double> downloadProgress, IProgress<double> extractionProgress);
    /// <summary>
    /// Imports over the EFT Settings from Live to SIT
    /// </summary>
    /// <param name="targetInstallDir"></param>
    /// <returns></returns>
    void CopyEftSettings(string targetInstallDir);
}
