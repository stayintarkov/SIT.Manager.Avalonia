using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Interfaces;

public interface IInstallerService
{
    /// <summary>
    /// Downloads and extract the patcher from the requested url providing progress as it goes
    /// </summary>
    /// <param name="url">The download mirror containing the required patcher</param>
    /// <param name="targetPath">The directory to download and extract the patcher into</param>
    /// <param name="downloadProgress"></param>
    /// <param name="extractionProgress"></param>
    /// <returns></returns>
    Task<bool> DownloadAndExtractPatcher(string url, string targetPath, IProgress<double> downloadProgress, IProgress<double> extractionProgress);
    Task<bool> DownloadAndRunPatcher(string url);
    /// <summary>
    /// Gets a dictionary of all available download mirrors for a sit version
    /// </summary>
    /// <param name="sitVersionTarget">The version to look for mirrors of</param>
    /// <param name="tarkovVersion">The optional provided tarkov version, if none is provided then will try and use the version provided in ManagerConfig</param>
    /// <returns></returns>
    Task<Dictionary<string, string>?> GetAvaiableMirrorsForVerison(string sitVersionTarget, string? tarkovVersion = null);
    /// <summary>
    /// Gets the file path for the BSG Install of EFT if it is able to be detected
    /// </summary>
    /// <returns>The path to the BSG Install of EFT or string.Empty</returns>
    string GetEFTInstallPath();
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
    /// Installs the selected SPT Server version reporting all progress on the way
    /// </summary>
    /// <param name="selectedVersion">The <see cref="GithubRelease"/> to install</param>
    /// <param name="targetInstallDir"></param>
    /// <param name="downloadProgress"></param>
    /// <param name="extractionProgress"></param>
    /// <returns></returns>
    Task InstallServer(GithubRelease selectedVersion, string targetInstallDir, IProgress<double> downloadProgress, IProgress<double> extractionProgress);
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
