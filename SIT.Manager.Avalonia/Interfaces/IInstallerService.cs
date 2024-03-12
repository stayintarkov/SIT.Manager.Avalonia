using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Interfaces;

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
