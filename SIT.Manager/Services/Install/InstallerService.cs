using FluentAvalonia.UI.Controls;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Config;
using SIT.Manager.Models.Gitea;
using SIT.Manager.Models.Github;
using SIT.Manager.Models.Installation;
using SIT.Manager.Services.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIT.Manager.Services.Install;

public partial class InstallerService(IBarNotificationService barNotificationService,
                                      IManagerConfigService configService,
                                      ILocalizationService localizationService,
                                      IFileService fileService,
                                      HttpClient httpClient,
                                      ILogger<InstallerService> logger,
                                      IVersionService versionService,
                                      ICachingService cachingService) : IInstallerService
{
    // Base 64 encoded url :)
    private const string PATCHER_URL = "aHR0cHM6Ly9wYXRjaGVyLnN0YXlpbnRhcmtvdi5jb20vYXBpL3YxL3JlcG9zL1NJVC9Eb3duZ3JhZGUtUGF0Y2hlcy9yZWxlYXNlcw==";

    private readonly ICachingService _cachingService = cachingService;

    [GeneratedRegex("This server version works with version ([0]{1,}\\.[0-9]{1,2}\\.[0-9]{1,2})\\.[0-9]{1,2}\\.[0-9]{1,5}")]
    private static partial Regex ServerReleaseVersionRegex();

    [GeneratedRegex("This version works with version [0]{1,}\\.[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,5}")]
    private static partial Regex SITReleaseVersionRegex();

    private List<SitInstallVersion>? _availableSitUpdateVersions;

    /// <summary>
    /// Cleans up the EFT directory
    /// </summary>
    /// <returns></returns>
    private void CleanUpEFTDirectory()
    {
        logger.LogInformation("Cleaning up EFT directory...");
        try
        {
            string battlEyeDir = Path.Combine(configService.Config.SitEftInstallPath, "BattlEye");
            if (Directory.Exists(battlEyeDir))
            {
                Directory.Delete(battlEyeDir, true);
            }
            string battlEyeExe = Path.Combine(configService.Config.SitEftInstallPath, "EscapeFromTarkov_BE.exe");
            if (File.Exists(battlEyeExe))
            {
                File.Delete(battlEyeExe);
            }
            string cacheDir = Path.Combine(configService.Config.SitEftInstallPath, "cache");
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
            string consistencyPath = Path.Combine(configService.Config.SitEftInstallPath, "ConsistencyInfo");
            if (File.Exists(consistencyPath))
            {
                File.Delete(consistencyPath);
            }
            string uninstallPath = Path.Combine(configService.Config.SitEftInstallPath, "Uninstall.exe");
            if (File.Exists(uninstallPath))
            {
                File.Delete(uninstallPath);
            }
            string logsDirPath = Path.Combine(configService.Config.SitEftInstallPath, "Logs");
            if (Directory.Exists(logsDirPath))
            {
                Directory.Delete(logsDirPath, true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cleanup");
        }
        logger.LogInformation("Cleanup done.");
    }

    /// <summary>
    /// Gets all available download mirrors for downgrade patchers required to run the given SIT version
    /// </summary>
    /// <param name="sitVersions">The available versions of SIT to check for mirrors of</param>
    /// <param name="tarkovVersion">The provided tarkov version to check agains</param>
    /// <returns></returns>
    private async Task<List<SitInstallVersion>> GetAvaiableMirrorsForVerison(List<SitInstallVersion> sitVersions, string tarkovVersion)
    {
        if (string.IsNullOrEmpty(tarkovVersion))
        {
            logger.LogError("Available mirrors has no tarkov version to use");
            return sitVersions;
        }

        ContentDialog blockedByISPWarning = new()
        {
            Title = localizationService.TranslateSource("InstallServiceErrorTitle"),
            Content = localizationService.TranslateSource("InstallServiceSSLError"),
            PrimaryButtonText = localizationService.TranslateSource("PlayPageViewModelButtonOk")
        };

        string releasesJsonString;
        try
        {
            CacheValue<string> releaseStrValue = await _cachingService.OnDisk.GetOrComputeAsync("downpatcher str",
                async (key) =>
                {
                    return await GetHttpStringWithRetryAsync(
                        () => httpClient.GetStringAsync(Encoding.UTF8.GetString(Convert.FromBase64String(PATCHER_URL))),
                        TimeSpan.FromSeconds(3), 3);
                }, TimeSpan.FromHours(6));
            
            releasesJsonString = releaseStrValue.Value!;
        }
        catch (AuthenticationException)
        {
            await blockedByISPWarning.ShowAsync();
            return [];
        }

        List<GiteaRelease> giteaReleases;
        try
        {
            giteaReleases = JsonSerializer.Deserialize<List<GiteaRelease>>(releasesJsonString) ?? [];
        }
        catch (JsonException)
        {
            await blockedByISPWarning.ShowAsync();
            return [];
        }

        if (giteaReleases.Count == 0)
        {
            logger.LogError("Found no available mirrors to use as a downgrade patcher");
            return sitVersions;
        }

        string tarkovBuild = tarkovVersion ?? configService.Config.SitTarkovVersion;
        tarkovBuild = tarkovBuild.Split(".").Last();

        for (int i = 0; i < sitVersions.Count; i++)
        {
            string sitVersionTargetBuild = sitVersions[i].Release.Body.Split(".").Last();
            if (tarkovBuild == sitVersionTargetBuild)
            {
                sitVersions[i].DownloadMirrors = [];
                continue;
            }

            GiteaRelease? compatibleDowngradePatcher = null;
            foreach (GiteaRelease? release in giteaReleases)
            {
                string[] splitRelease = release.Name.Split("to");
                if (splitRelease.Length != 2)
                {
                    continue;
                }

                string patcherFrom = splitRelease[0].Trim();
                string patcherTo = splitRelease[1].Trim();

                if (patcherFrom == tarkovBuild && patcherTo == sitVersionTargetBuild)
                {
                    compatibleDowngradePatcher = release;
                    break;
                }
            }

            if (compatibleDowngradePatcher != null)
            {
                string mirrorsUrl = compatibleDowngradePatcher.Assets.Find(q => q.Name == "mirrors.json")?.BrowserDownloadUrl ?? string.Empty;
                string mirrorsJsonString = await GetHttpStringWithRetryAsync(() => httpClient.GetStringAsync(mirrorsUrl), TimeSpan.FromSeconds(3), 3);
                List<Mirrors> mirrors = JsonSerializer.Deserialize<List<Mirrors>>(mirrorsJsonString) ?? [];
                if (mirrors.Count == 0)
                {
                    logger.LogError("No download mirrors found for patcher.");
                    continue;
                }

                Dictionary<string, string> providerLinks = [];
                foreach (Mirrors mirror in mirrors)
                {
                    Uri uri = new(mirror.Link);
                    string host = uri.Host.Replace("www.", "").Split('.')[0];
                    providerLinks.TryAdd(host, mirror.Link);
                }

                if (providerLinks.Count > 0)
                {
                    sitVersions[i].DownloadMirrors = providerLinks;
                }
            }
            else
            {
                logger.LogWarning("No applicable patcher found for the specified SIT version ({sitVersion} and Tarkov version {tarkovVersion}.", sitVersions[i].SitVersion, tarkovVersion);
            }
        }

        return sitVersions;
    }

    private static async Task<string> GetHttpStringWithRetryAsync(Func<Task<string>> action, TimeSpan sleepPeriod, int tryCount = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tryCount);

        List<Exception> exceptions = [];
        for (int attempted = 0; attempted < tryCount; attempted++)
        {
            try
            {
                if (attempted > 0)
                {
                    await Task.Delay(sleepPeriod);
                }
                return await action();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }
        throw new AggregateException(exceptions);
    }

    private async Task<List<SitInstallVersion>> GetSitReleases()
    {
        List<GithubRelease> githubReleases;
        try
        {
            string releasesJsonString = await httpClient.GetStringAsync(@"https://api.github.com/repos/stayintarkov/StayInTarkov.Client/releases");
            githubReleases = JsonSerializer.Deserialize<List<GithubRelease>>(releasesJsonString) ?? [];

        }
        catch (Exception ex)
        {
            githubReleases = [];
            logger.LogError(ex, "Failed to get SIT releases");
        }

        List<SitInstallVersion> result = [];
        if (githubReleases.Count != 0)
        {
            foreach (GithubRelease release in githubReleases)
            {
                Match match = SITReleaseVersionRegex().Match(release.Body);
                if (match.Success)
                {
                    string releasePatch = match.Value.Replace("This version works with version ", "");
                    release.TagName = $"{release.Name} - Tarkov Version: {releasePatch}";
                    release.Body = releasePatch;

                    SitInstallVersion sitVersion = new()
                    {
                        Release = release,
                        EftVersion = releasePatch,
                        SitVersion = release.Name,
                    };

                    if (release.Prerelease && configService.Config.EnableTestMode)
                    {
                        result.Add(sitVersion);
                    }
                    else if (!release.Prerelease && !configService.Config.EnableTestMode)
                    {
                        result.Add(sitVersion);
                    }
                }
                else
                {
                    logger.LogWarning("FetchReleases: There was a SIT release without a version defined: {url}", release.HtmlUrl);
                }
            }
        }
        else
        {
            logger.LogWarning("Getting SIT releases: githubReleases was 0 for official branch");
        }

        return result;
    }

    private static async Task CloneFileAsync(string sourceFile, string destinationFile)
    {
        using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
    }

    /// <summary>
    /// Clones a directory
    /// </summary>
    /// <param name="root">Root path to clone</param>
    /// <param name="dest">Destination path to clone to</param>
    /// <returns></returns>
    private static async Task CloneDirectoryAsync(string root, string dest)
    {
        foreach (var directory in Directory.GetDirectories(root))
        {
            var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
            Directory.CreateDirectory(newDirectory);
            await CloneDirectoryAsync(directory, newDirectory);
        }

        foreach (var file in Directory.GetFiles(root))
        {
            await CloneFileAsync(file, Path.Combine(dest, Path.GetFileName(file)));
        }
    }

    /// <inheritdoc/>
    public Process CreatePatcherProcess(string patcherPath)
    {
        logger.LogInformation("Starting Patcher");

        Process patcherProcess = new()
        {
            StartInfo = new()
            {
                FileName = patcherPath,
                Arguments = "autoclose"
            },
            EnableRaisingEvents = true
        };
        if (OperatingSystem.IsLinux())
        {
            // TODO: actually improve this (will probably be done after fixing launching problems)
            LinuxConfig config = configService.Config.LinuxConfig;
            string winePrefix = Path.GetFullPath(config.WinePrefix);
            // Update the wine prefix and install any required components
            UpdateWinePrefix(winePrefix);

            patcherProcess.StartInfo.FileName = config.WineRunner;
            patcherProcess.StartInfo.Arguments = $"\"{patcherPath}\" autoclose";
            patcherProcess.StartInfo.UseShellExecute = false;

            if (!Path.EndsInDirectorySeparator(winePrefix))
            {
                winePrefix = $"{winePrefix}{Path.DirectorySeparatorChar}";
            }
            patcherProcess.StartInfo.EnvironmentVariables.Add("WINEPREFIX", winePrefix);
        }
        else
        {
            patcherProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(patcherPath);
        }

        return patcherProcess;
    }

    private void UpdateWinePrefix(string configWinePrefix)
    {
        try
        {
            using Process process = new();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c \"WINEPREFIX=" + configWinePrefix + " winetricks -q arial times dotnetdesktop6 dotnetdesktop8 win81\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            process.WaitForExit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while installing .NET Desktop Runtime 6.0");
        }
    }

    public async Task<List<GithubRelease>> GetServerReleases()
    {
        List<GithubRelease> githubReleases;
        try
        {
            string releasesJsonString = await httpClient.GetStringAsync(@"https://api.github.com/repos/stayintarkov/SIT.Aki-Server-Mod/releases");
            githubReleases = JsonSerializer.Deserialize<List<GithubRelease>>(releasesJsonString) ?? [];
        }
        catch (Exception ex)
        {
            githubReleases = [];
            logger.LogError(ex, "Failed to get server releases");
        }

        List<GithubRelease> result = [];
        if (githubReleases.Count != 0)
        {
            foreach (GithubRelease release in githubReleases)
            {
                // Check there is an asset available for this OS
                string fileExtention = ".zip";
                if (OperatingSystem.IsLinux())
                {
                    fileExtention = ".tar.gz";
                }

                GithubAsset? releaseAsset = release.Assets.Find(asset => asset.Name.EndsWith(fileExtention));
                if (releaseAsset != null)
                {
                    Match match = ServerReleaseVersionRegex().Match(release.Body);
                    if (match.Success)
                    {
                        string releasePatch = match.Value.Replace("This server version works with version ", "");
                        release.TagName = $"{release.Name} - Tarkov Version: {releasePatch}";
                        release.Body = releasePatch;

                        if (release.Prerelease && configService.Config.EnableTestMode)
                        {
                            result.Add(release);
                        }
                        else if (!release.Prerelease && !configService.Config.EnableTestMode)
                        {
                            result.Add(release);
                        }
                    }
                    else
                    {
                        logger.LogWarning("FetchReleases: There was a server release without a version defined: {url}", release.HtmlUrl);
                    }
                }
            }
        }
        else
        {
            logger.LogWarning("Getting Server Releases: githubReleases was 0 for official branch");
        }
        return result;

    }

    /// <inheritdoc/>
    public async Task<bool> DownloadAndExtractPatcher(string url, string targetPath, IProgress<double> downloadProgress, IProgress<double> extractionProgress)
    {
        logger.LogInformation("Downloading Patcher");

        if (string.IsNullOrEmpty(targetPath))
        {
            logger.LogError("DownloadPatcher: targetPath is null or empty");
            return false;
        }

        string patcherPath = Path.Combine(targetPath, "Patcher.zip");
        if (File.Exists(patcherPath))
        {
            File.Delete(patcherPath);
        }

        bool downloadSuccess = await fileService.DownloadFile("Patcher.zip", targetPath, url, downloadProgress).ConfigureAwait(false);
        if (!downloadSuccess)
        {
            logger.LogError("Failed to download the patcher from the selected mirror.");
            return false;
        }

        if (File.Exists(patcherPath))
        {
            await fileService.ExtractArchive(patcherPath, targetPath, extractionProgress).ConfigureAwait(false);
            File.Delete(patcherPath);
        }

        var patcherDir = Directory.GetDirectories(targetPath, "Patcher*").FirstOrDefault();
        if (!string.IsNullOrEmpty(patcherDir))
        {
            await CloneDirectoryAsync(patcherDir, targetPath).ConfigureAwait(false);
            Directory.Delete(patcherDir, true);
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<SitInstallVersion>> GetAvailableSitReleases(string tarkovVersion)
    {
        List<SitInstallVersion> availableVersions = await GetSitReleases();
        availableVersions = await GetAvaiableMirrorsForVerison(availableVersions, tarkovVersion);

        // Evaluate the availability of the SIT versions based on the current tarkov version
        for (int i = 0; i < availableVersions.Count; i++)
        {
            if (availableVersions[i].EftVersion == tarkovVersion)
            {
                availableVersions[i].DowngradeRequired = false;
                availableVersions[i].IsAvailable = true;
            }
            else if (availableVersions[i].DownloadMirrors.Count != 0)
            {
                availableVersions[i].DowngradeRequired = true;
                availableVersions[i].IsAvailable = true;
            }
        }
        return availableVersions;
    }

    public string GetEFTInstallPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }
        return EFTGameFinder.FindOfficialGamePath();
    }

    public SitInstallVersion? GetLatestAvailableSitRelease()
    {
        return _availableSitUpdateVersions?.MaxBy(x => x.SitVersion);
    }

    public async Task<bool> IsSitUpdateAvailable()
    {
        if (string.IsNullOrEmpty(configService.Config.SitTarkovVersion) || string.IsNullOrEmpty(configService.Config.SitVersion)) return false;

        TimeSpan timeSinceLastCheck = DateTime.Now - configService.Config.LastSitUpdateCheckTime;

        if (timeSinceLastCheck.TotalHours >= 1)
        {
            _availableSitUpdateVersions = await GetAvailableSitReleases(configService.Config.SitTarkovVersion);

            if (_availableSitUpdateVersions != null)
            {
                _availableSitUpdateVersions = _availableSitUpdateVersions
                    .Where(x => Version.TryParse(x.SitVersion.Replace("StayInTarkov.Client-", ""), out Version? sitVersion) &&
                                sitVersion > Version.Parse(configService.Config.SitVersion) &&
                                configService.Config.SitTarkovVersion == x.EftVersion)
                    .ToList();
            }

            configService.Config.LastSitUpdateCheckTime = DateTime.Now;
            configService.UpdateConfig(configService.Config);
        }

        return _availableSitUpdateVersions != null && _availableSitUpdateVersions.Count != 0;
    }


    public async Task InstallServer(GithubRelease selectedVersion, string targetInstallDir, IProgress<double> downloadProgress, IProgress<double> extractionProgress)
    {
        if (selectedVersion == null)
        {
            // TODO maybe transfer these _barNotificationErrors to only display in the install ui rather than as a disappearing bar notification?
            barNotificationService.ShowError("Error", "No server version selected to install");
            logger.LogWarning("Install Server: selectVersion is 'null'");
            return;
        }

        // Dynamically find the asset that starts with "SITCoop" and ends with ".zip"
        string fileExtention = ".zip";
        if (OperatingSystem.IsLinux())
        {
            fileExtention = ".tar.gz";
        }

        GithubAsset? releaseAsset = selectedVersion.Assets.FirstOrDefault(a => a.Name.StartsWith("SITCoop") && a.Name.EndsWith(fileExtention));
        if (releaseAsset == null)
        {
            barNotificationService.ShowError("Error", "No server release found to download");
            logger.LogError("No matching release asset found.");
            return;
        }
        string releaseZipUrl = releaseAsset.BrowserDownloadUrl;

        if (string.IsNullOrEmpty(targetInstallDir))
        {
            barNotificationService.ShowError("Error", "Unable to use provided installation directory");
            logger.LogError("Unable to use provided installation directory was null or empty");
            return;
        }

        // Create SPT-AKI directory (default: Server)
        if (!Directory.Exists(targetInstallDir))
        {
            Directory.CreateDirectory(targetInstallDir);
        }

        // Define the paths for download target directory
        string downloadLocation = Path.Combine(targetInstallDir, releaseAsset.Name);

        try
        {
            // Download and extract the file into the target directory
            await fileService.DownloadFile(releaseAsset.Name, targetInstallDir, releaseZipUrl, downloadProgress);
            await fileService.ExtractArchive(downloadLocation, targetInstallDir, extractionProgress);
        }
        catch (Exception ex)
        {
            barNotificationService.ShowError("Install Error", "Encountered an error during server installation.", 10);
            logger.LogError(ex, "Install Server");
            throw;
        }

        // Remove the downloaded Server after extraction
        File.Delete(downloadLocation);

        // Ensure that the file is marked as executable
        string executablePath = Path.Combine(targetInstallDir, "Aki.Server.exe");
        await fileService.SetFileAsExecutable(executablePath);

        // Attempt to automatically set the AKI Server Path after successful installation and save it to config
        ManagerConfig config = configService.Config;
        if (!string.IsNullOrEmpty(targetInstallDir))
        {
            config.AkiServerPath = targetInstallDir;
        }
        config.SptAkiVersion = versionService.GetSptAkiVersion(targetInstallDir);
        config.SitModVersion = versionService.GetSitModVersion(targetInstallDir);

        configService.UpdateConfig(config);
    }

    public async Task InstallSit(GithubRelease selectedVersion, string targetInstallDir, IProgress<double> downloadProgress, IProgress<double> extractionProgress)
    {
        const int downloadAndExtractionSteps = 2;
        double internalDownloadProgressPercentage = 0;
        double internalExtractionProgressPercentage = 0;

        Progress<double> internalDownloadProgress = new(progress =>
        {
            internalDownloadProgressPercentage = progress / downloadAndExtractionSteps;
            downloadProgress.Report(internalDownloadProgressPercentage);
        });
        Progress<double> internalExtractionProgress = new(progress =>
        {
            internalExtractionProgressPercentage = progress / downloadAndExtractionSteps;
            extractionProgress.Report(internalExtractionProgressPercentage);
        });

        if (string.IsNullOrEmpty(targetInstallDir))
        {
            barNotificationService.ShowError(localizationService.TranslateSource("InstallServiceErrorTitle"), localizationService.TranslateSource("InstallServiceErrorInstallSITDescription"));
            return;
        }

        if (selectedVersion == null)
        {
            logger.LogWarning("InstallSIT: selectVersion is 'null'");
            return;
        }

        try
        {
            if (File.Exists(Path.Combine(targetInstallDir, "EscapeFromTarkov_BE.exe")))
            {
                CleanUpEFTDirectory();
            }

            var coreFilesPath = Path.Combine(targetInstallDir, "SITLauncher", "CoreFiles");

            // Recursively delete all downloaded files / folders
            if(Directory.Exists(coreFilesPath))
                Directory.Delete(coreFilesPath, true);

            // Recreate directory for downloaded files / folders
            Directory.CreateDirectory(coreFilesPath);


            string backupCoreFilesPath = Path.Combine(targetInstallDir, "SITLauncher", "Backup", "CoreFiles");
            if (!Directory.Exists(backupCoreFilesPath))
                Directory.CreateDirectory(backupCoreFilesPath);

            string pluginsPath = Path.Combine(targetInstallDir, "BepInEx", "plugins");
            Directory.CreateDirectory(pluginsPath);

            string patchersPath = Path.Combine(targetInstallDir, "BepInEx", "patchers");
            Directory.CreateDirectory(patchersPath);

            string bepinexPath = Path.Combine(targetInstallDir, "SITLauncher");
            await fileService.DownloadFile("BepInEx5.zip", bepinexPath, "https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip", internalDownloadProgress);
            await fileService.ExtractArchive(Path.Combine(bepinexPath, "BepInEx5.zip"), targetInstallDir, internalExtractionProgress);

            CopyEftSettings(targetInstallDir);

            // We don't use index as they might be different from version to version
            string? releaseZipUrl = selectedVersion.Assets.Find(q => q.Name == "StayInTarkov-Release.zip")?.BrowserDownloadUrl;
            if (!string.IsNullOrEmpty(releaseZipUrl))
            {
                internalDownloadProgress = new(progress =>
                {
                    internalDownloadProgressPercentage = 50 + (progress / downloadAndExtractionSteps);
                    downloadProgress.Report(internalDownloadProgressPercentage);
                });
                internalExtractionProgress = new(progress =>
                {
                    internalExtractionProgressPercentage = 50 + (progress / downloadAndExtractionSteps);
                    extractionProgress.Report(internalExtractionProgressPercentage);
                });

                await fileService.DownloadFile("StayInTarkov-Release.zip", coreFilesPath, releaseZipUrl, internalDownloadProgress);
                await fileService.ExtractArchive(Path.Combine(coreFilesPath, "StayInTarkov-Release.zip"), coreFilesPath, internalExtractionProgress);
            }

            // Find Assembly-CSharp file
            var assemblyCSharpFiles = Directory.GetFiles(coreFilesPath, "*Assembly-CSharp.dll");
            if (assemblyCSharpFiles.Length == 0)
                throw new IndexOutOfRangeException("No Assembly-CSharp found in download!");
            if (assemblyCSharpFiles.Length > 1)
                throw new IndexOutOfRangeException("There are more than one Assembly-CSharp files found!");

            // Find StayInTarkov.dll
            var sitFiles = Directory.GetFiles(coreFilesPath, "*StayInTarkov.dll");
            if (sitFiles.Length == 0)
                throw new IndexOutOfRangeException("No StayInTarkov.dll found in download!");
            if (sitFiles.Length > 1)
                throw new IndexOutOfRangeException("There are more than one StayInTarkov.dll files found!");

            // Find SIT.WildSpawnType.PrePatcher.dll
            var prePatcherFiles = Directory.GetFiles(coreFilesPath, "*PrePatch*");

            string eftDataManagedPath = Path.Combine(targetInstallDir, "EscapeFromTarkov_Data", "Managed");
            if (File.Exists(Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll")))
            {
                File.Copy(Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll"), Path.Combine(backupCoreFilesPath, "Assembly-CSharp.dll"), true);
            }

            if(Directory.Exists(eftDataManagedPath))
                File.Copy(assemblyCSharpFiles[0], Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll"), true);

            if(Directory.Exists(pluginsPath))
                File.Copy(sitFiles[0], Path.Combine(pluginsPath, "StayInTarkov.dll"), true);

            foreach (var ppFI in prePatcherFiles.Select(x => new FileInfo(x)))
            {
                var ppFilePath = ppFI.Name;
                File.Copy(ppFI.FullName, Path.Combine(patchersPath, ppFilePath), true);
            }

            using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT.Manager.Resources.Aki.Common.dll"))
            {
                using (FileStream file = new(Path.Combine(eftDataManagedPath, "Aki.Common.dll"), FileMode.Create, FileAccess.Write))
                {
                    if (resource != null)
                    {
                        await resource.CopyToAsync(file);
                    }
                }
            }

            using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT.Manager.Resources.Aki.Reflection.dll"))
            {
                using (FileStream file = new(Path.Combine(eftDataManagedPath, "Aki.Reflection.dll"), FileMode.Create, FileAccess.Write))
                {
                    if (resource != null)
                    {
                        await resource.CopyToAsync(file);
                    }
                }
            }

            downloadProgress.Report(1);
            extractionProgress.Report(100);

            ManagerConfig config = configService.Config;
            config.SitEftInstallPath = targetInstallDir;
            config.SitTarkovVersion = versionService.GetEFTVersion(targetInstallDir);
            config.SitVersion = versionService.GetSITVersion(targetInstallDir);
            configService.UpdateConfig(config);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Install SIT");
            throw;
        }
    }
    public void CopyEftSettings(string targetInstallDir)
    {
        var sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Battlestate Games", "Escape from Tarkov", "Settings");
        var destinationPath = Path.Combine(targetInstallDir, "user", "sptSettings");

        var filesToCopy = new[] { "Control.ini", "Game.ini", "Graphics.ini", "PostFx.ini", "Sound.ini" };
        try
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            foreach (var fileName in filesToCopy)
            {
                var sourceFile = Path.Combine(sourcePath, fileName);
                if (File.Exists(sourceFile))
                {
                    var destFile = Path.Combine(destinationPath, fileName);
                    File.Copy(sourceFile, destFile, overwrite: true);
                }
            }

            logger.LogInformation("Successfully copied EFT settings to '{destinationPath}'.", destinationPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to copy EFT settings.");
        }
    }
}
