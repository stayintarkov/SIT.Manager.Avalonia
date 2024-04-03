using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using SIT.Manager.Models.Installation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
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
                                      IVersionService versionService) : IInstallerService
{
    private readonly IBarNotificationService _barNotificationService = barNotificationService;
    private readonly IManagerConfigService _configService = configService;
    private readonly IFileService _fileService = fileService;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<InstallerService> _logger = logger;
    private readonly IVersionService _versionService = versionService;
    private readonly ILocalizationService _localizationService = localizationService;

    [GeneratedRegex("This server version works with version ([0]{1,}\\.[0-9]{1,2}\\.[0-9]{1,2})\\.[0-9]{1,2}\\.[0-9]{1,5}")]
    private static partial Regex ServerReleaseVersionRegex();

    [GeneratedRegex("This version works with version [0]{1,}\\.[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,5}")]
    private static partial Regex SITReleaseVersionRegex();

    private static readonly Dictionary<int, string> _patcherResultMessages = new() {
        { 0, "Patcher was closed." },
        { 10, "Patcher was successful." },
        { 11, "Could not find 'EscapeFromTarkov.exe'." },
        { 12, "'Aki_Patches' is missing." },
        { 13, "Install folder is missing a file." },
        { 14, "Install folder is missing a folder." },
        { 15, "Patcher failed." }
    };

    private List<SitInstallVersion>? _availableSitUpdateVersions;

    /// <summary>
    /// Cleans up the EFT directory
    /// </summary>
    /// <returns></returns>
    private void CleanUpEFTDirectory()
    {
        _logger.LogInformation("Cleaning up EFT directory...");
        try
        {
            string battlEyeDir = Path.Combine(_configService.Config.InstallPath, "BattlEye");
            if (Directory.Exists(battlEyeDir))
            {
                Directory.Delete(battlEyeDir, true);
            }
            string battlEyeExe = Path.Combine(_configService.Config.InstallPath, "EscapeFromTarkov_BE.exe");
            if (File.Exists(battlEyeExe))
            {
                File.Delete(battlEyeExe);
            }
            string cacheDir = Path.Combine(_configService.Config.InstallPath, "cache");
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
            string consistencyPath = Path.Combine(_configService.Config.InstallPath, "ConsistencyInfo");
            if (File.Exists(consistencyPath))
            {
                File.Delete(consistencyPath);
            }
            string uninstallPath = Path.Combine(_configService.Config.InstallPath, "Uninstall.exe");
            if (File.Exists(uninstallPath))
            {
                File.Delete(uninstallPath);
            }
            string logsDirPath = Path.Combine(_configService.Config.InstallPath, "Logs");
            if (Directory.Exists(logsDirPath))
            {
                Directory.Delete(logsDirPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup");
        }
        _logger.LogInformation("Cleanup done.");
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
            _logger.LogError("Available mirrors has no tarkov version to use");
            return sitVersions;
        }

        ContentDialog blockedByISPWarning = new()
        {
            Title = _localizationService.TranslateSource("InstallServiceErrorTitle"),
            Content = _localizationService.TranslateSource("InstallServiceSSLError"),
            PrimaryButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonOk")
        };

        string releasesJsonString;
        try
        {
            releasesJsonString = await GetHttpStringWithRetryAsync(() => _httpClient.GetStringAsync(@"https://patcher.stayintarkov.com/api/v1/repos/SIT/Downgrade-Patches/releases"), TimeSpan.FromSeconds(1), 3);
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
            _logger.LogError("Found no available mirrors to use as a downgrade patcher");
            return sitVersions;
        }

        string tarkovBuild = tarkovVersion ?? _configService.Config.TarkovVersion;
        tarkovBuild = tarkovBuild.Split(".").Last();

        for (int i = 0; i < sitVersions.Count; i++)
        {
            string sitVersionTargetBuild = sitVersions[i].Release.body.Split(".").Last();
            if (tarkovBuild == sitVersionTargetBuild)
            {
                sitVersions[i].DownloadMirrors = [];
                continue;
            }

            GiteaRelease? compatibleDowngradePatcher = null;
            foreach (GiteaRelease? release in giteaReleases)
            {
                string[] splitRelease = release.name.Split("to");
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
                string mirrorsUrl = compatibleDowngradePatcher.assets.Find(q => q.name == "mirrors.json")?.browser_download_url ?? string.Empty;
                string mirrorsJsonString = await GetHttpStringWithRetryAsync(() => _httpClient.GetStringAsync(mirrorsUrl), TimeSpan.FromSeconds(1), 3);
                List<Mirrors> mirrors = JsonSerializer.Deserialize<List<Mirrors>>(mirrorsJsonString) ?? [];
                if (mirrors.Count == 0)
                {
                    _logger.LogError("No download mirrors found for patcher.");
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
                _logger.LogWarning($"No applicable patcher found for the specified SIT version ({sitVersions[i].SitVersion} and Tarkov version {tarkovVersion}.");
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
            string releasesJsonString = await _httpClient.GetStringAsync(@"https://api.github.com/repos/stayintarkov/StayInTarkov.Client/releases");
            githubReleases = JsonSerializer.Deserialize<List<GithubRelease>>(releasesJsonString) ?? [];

        }
        catch (Exception ex)
        {
            githubReleases = [];
            _logger.LogError(ex, "Failed to get SIT releases");
        }

        List<SitInstallVersion> result = [];
        if (githubReleases.Count != 0)
        {
            foreach (GithubRelease release in githubReleases)
            {
                Match match = SITReleaseVersionRegex().Match(release.body);
                if (match.Success)
                {
                    string releasePatch = match.Value.Replace("This version works with version ", "");
                    release.tag_name = $"{release.name} - Tarkov Version: {releasePatch}";
                    release.body = releasePatch;

                    SitInstallVersion sitVersion = new()
                    {
                        Release = release,
                        EftVersion = releasePatch,
                        SitVersion = release.name,
                    };

                    result.Add(sitVersion);
                }
                else
                {
                    _logger.LogWarning($"FetchReleases: There was a SIT release without a version defined: {release.html_url}");
                }
            }
        }
        else
        {
            _logger.LogWarning("Getting SIT releases: githubReleases was 0 for official branch");
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
        _logger.LogInformation("Starting Patcher");

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
            patcherProcess.StartInfo.FileName = _configService.Config.WineRunner;
            patcherProcess.StartInfo.Arguments = $"\"{patcherPath}\" autoclose";
            patcherProcess.StartInfo.UseShellExecute = false;

            string winePrefix = Path.GetFullPath(_configService.Config.WinePrefix);
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

    public async Task<List<GithubRelease>> GetServerReleases()
    {
        List<GithubRelease> githubReleases;
        try
        {
            string releasesJsonString = await _httpClient.GetStringAsync(@"https://api.github.com/repos/stayintarkov/SIT.Aki-Server-Mod/releases");
            githubReleases = JsonSerializer.Deserialize<List<GithubRelease>>(releasesJsonString) ?? [];
        }
        catch (Exception ex)
        {
            githubReleases = [];
            _logger.LogError(ex, "Failed to get server releases");
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

                GithubRelease.Asset? releaseAsset = release.assets.Find(asset => asset.name.EndsWith(fileExtention));
                if (releaseAsset != null)
                {
                    Match match = ServerReleaseVersionRegex().Match(release.body);
                    if (match.Success)
                    {
                        string releasePatch = match.Value.Replace("This server version works with version ", "");
                        release.tag_name = $"{release.name} - Tarkov Version: {releasePatch}";
                        release.body = releasePatch;
                        result.Add(release);
                    }
                    else
                    {
                        _logger.LogWarning($"FetchReleases: There was a server release without a version defined: {release.html_url}");
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning("Getting Server Releases: githubReleases was 0 for official branch");
        }
        return result;

    }

    /// <inheritdoc/>
    public async Task<bool> DownloadAndExtractPatcher(string url, string targetPath, IProgress<double> downloadProgress, IProgress<double> extractionProgress)
    {
        _logger.LogInformation("Downloading Patcher");

        if (string.IsNullOrEmpty(targetPath))
        {
            _logger.LogError("DownloadPatcher: targetPath is null or empty");
            return false;
        }

        string patcherPath = Path.Combine(targetPath, "Patcher.zip");
        if (File.Exists(patcherPath))
        {
            File.Delete(patcherPath);
        }

        bool downloadSuccess = await _fileService.DownloadFile("Patcher.zip", targetPath, url, downloadProgress).ConfigureAwait(false);
        if (!downloadSuccess)
        {
            _logger.LogError("Failed to download the patcher from the selected mirror.");
            return false;
        }

        if (File.Exists(patcherPath))
        {
            await _fileService.ExtractArchive(patcherPath, targetPath, extractionProgress).ConfigureAwait(false);
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

    public async Task<bool> IsSitUpateAvailable()
    {
        bool updateAvailable = false;

        // Get the list of available releases then check if one exists which is newer than the current and for the same Tarkov version
        if (!string.IsNullOrEmpty(_configService.Config.SitVersion))
        {
            // Cache this list as we will potentially use it later
            _availableSitUpdateVersions = await GetAvailableSitReleases(_configService.Config.TarkovVersion);
            _availableSitUpdateVersions = _availableSitUpdateVersions.Where(x =>
            {
                bool parsedSitVersion = Version.TryParse(x.SitVersion.Replace("StayInTarkov.Client-", ""), out Version? sitVersion);
                if (parsedSitVersion)
                {
                    Version installedSit = Version.Parse(_configService.Config.SitVersion);
                    if (sitVersion > installedSit && _configService.Config.TarkovVersion == x.EftVersion)
                    {
                        return true;
                    }
                }
                return false;
            }).ToList();
            updateAvailable = _availableSitUpdateVersions.Count != 0;
        }

        return updateAvailable;
    }

    public async Task InstallServer(GithubRelease selectedVersion, string targetInstallDir, IProgress<double> downloadProgress, IProgress<double> extractionProgress)
    {
        if (selectedVersion == null)
        {
            // TODO maybe transfer these _barNotificationErrors to only display in the install ui rather than as a disappearing bar notification?
            _barNotificationService.ShowError("Error", "No server version selected to install");
            _logger.LogWarning("Install Server: selectVersion is 'null'");
            return;
        }

        // Dynamically find the asset that starts with "SITCoop" and ends with ".zip"
        string fileExtention = ".zip";
        if (OperatingSystem.IsLinux())
        {
            fileExtention = ".tar.gz";
        }

        GithubRelease.Asset? releaseAsset = selectedVersion.assets.FirstOrDefault(a => a.name.StartsWith("SITCoop") && a.name.EndsWith(fileExtention));
        if (releaseAsset == null)
        {
            _barNotificationService.ShowError("Error", "No server release found to download");
            _logger.LogError("No matching release asset found.");
            return;
        }
        string releaseZipUrl = releaseAsset.browser_download_url;

        if (string.IsNullOrEmpty(targetInstallDir))
        {
            _barNotificationService.ShowError("Error", "Unable to use provided installation directory");
            _logger.LogError("Unable to use provided installation directory was null or empty");
            return;
        }

        // Create SPT-AKI directory (default: Server)
        if (!Directory.Exists(targetInstallDir))
        {
            Directory.CreateDirectory(targetInstallDir);
        }

        // Define the paths for download target directory
        string downloadLocation = Path.Combine(targetInstallDir, releaseAsset.name);

        try
        {
            // Download and extract the file into the target directory
            await _fileService.DownloadFile(releaseAsset.name, targetInstallDir, releaseZipUrl, downloadProgress);
            await _fileService.ExtractArchive(downloadLocation, targetInstallDir, extractionProgress);
        }
        catch (Exception ex)
        {
            _barNotificationService.ShowError("Install Error", "Encountered an error during server installation.", 10);
            _logger.LogError(ex, "Install Server");
            throw;
        }

        // Remove the downloaded Server after extraction
        File.Delete(downloadLocation);

        // Ensure that the file is marked as executable
        string executablePath = Path.Combine(targetInstallDir, "Aki.Server.exe");
        await _fileService.SetFileAsExecutable(executablePath);

        // Attempt to automatically set the AKI Server Path after successful installation and save it to config
        ManagerConfig config = _configService.Config;
        if (!string.IsNullOrEmpty(targetInstallDir))
        {
            config.AkiServerPath = targetInstallDir;
        }
        config.SptAkiVersion = _versionService.GetSptAkiVersion(targetInstallDir);
        config.SitModVersion = _versionService.GetSitModVersion(targetInstallDir);

        _configService.UpdateConfig(config);
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
            _barNotificationService.ShowError(_localizationService.TranslateSource("InstallServiceErrorTitle"), _localizationService.TranslateSource("InstallServiceErrorInstallSITDescription"));
            return;
        }

        if (selectedVersion == null)
        {
            _logger.LogWarning("InstallSIT: selectVersion is 'null'");
            return;
        }

        try
        {
            if (File.Exists(Path.Combine(targetInstallDir, "EscapeFromTarkov_BE.exe")))
            {
                CleanUpEFTDirectory();
            }

            string sitReleaseZipPath = Path.Combine(targetInstallDir, "SITLauncher", "CoreFiles", "StayInTarkov-Release.zip");
            if (File.Exists(sitReleaseZipPath))
            {
                File.Delete(sitReleaseZipPath);
            }

            string coreFilesPath = Path.Combine(targetInstallDir, "SITLauncher", "CoreFiles");
            if (!Directory.Exists(coreFilesPath))
            {
                Directory.CreateDirectory(coreFilesPath);
            }

            string backupCoreFilesPath = Path.Combine(targetInstallDir, "SITLauncher", "Backup", "CoreFiles");
            if (!Directory.Exists(backupCoreFilesPath))
            {
                Directory.CreateDirectory(backupCoreFilesPath);
            }

            string pluginsPath = Path.Combine(targetInstallDir, "BepInEx", "plugins");
            Directory.CreateDirectory(pluginsPath);

            string bepinexPath = Path.Combine(targetInstallDir, "SITLauncher");
            await _fileService.DownloadFile("BepInEx5.zip", bepinexPath, "https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip", internalDownloadProgress);
            await _fileService.ExtractArchive(Path.Combine(bepinexPath, "BepInEx5.zip"), targetInstallDir, internalExtractionProgress);

            // We don't use index as they might be different from version to version
            string? releaseZipUrl = selectedVersion.assets.Find(q => q.name == "StayInTarkov-Release.zip")?.browser_download_url;
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

                await _fileService.DownloadFile("StayInTarkov-Release.zip", coreFilesPath, releaseZipUrl, internalDownloadProgress);
                await _fileService.ExtractArchive(Path.Combine(coreFilesPath, "StayInTarkov-Release.zip"), coreFilesPath, internalExtractionProgress);
            }

            string eftDataManagedPath = Path.Combine(targetInstallDir, "EscapeFromTarkov_Data", "Managed");
            if (File.Exists(Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll")))
            {
                File.Copy(Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll"), Path.Combine(backupCoreFilesPath, "Assembly-CSharp.dll"), true);
            }
            File.Copy(Path.Combine(coreFilesPath, "StayInTarkov-Release", "Assembly-CSharp.dll"), Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll"), true);
            File.Copy(Path.Combine(coreFilesPath, "StayInTarkov-Release", "StayInTarkov.dll"), Path.Combine(pluginsPath, "StayInTarkov.dll"), true);

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

            ManagerConfig config = _configService.Config;
            config.InstallPath = targetInstallDir;
            config.TarkovVersion = _versionService.GetEFTVersion(targetInstallDir);
            config.SitVersion = _versionService.GetSITVersion(targetInstallDir);
            _configService.UpdateConfig(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install SIT");
            throw;
        }
    }
}
