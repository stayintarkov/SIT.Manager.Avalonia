using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services
{
    public partial class InstallerService(IActionNotificationService actionNotificationService,
                                          IBarNotificationService barNotificationService,
                                          IManagerConfigService configService,
                                          ILocalizationService localizationService,
                                          IFileService fileService,
                                          HttpClient httpClient,
                                          ILogger<InstallerService> logger,
                                          IVersionService versionService) : IInstallerService
    {
        private readonly IActionNotificationService _actionNotificationService = actionNotificationService;
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

        /// <summary>
        /// Cleans up the EFT directory
        /// </summary>
        /// <returns></returns>
        public void CleanUpEFTDirectory()
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
        /// Clones a directory
        /// </summary>
        /// <param name="root">Root path to clone</param>
        /// <param name="dest">Destination path to clone to</param>
        /// <returns></returns>
        private void CloneDirectory(string root, string dest)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
                Directory.CreateDirectory(newDirectory);
                CloneDirectory(directory, newDirectory);
            }

            foreach (var file in Directory.GetFiles(root))
            {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }

        /// <summary>
        /// Runs the downgrade patcher
        /// </summary>
        /// <returns>string with result</returns>
        private async Task<string> RunPatcher()
        {
            _logger.LogInformation("Starting Patcher");
            _actionNotificationService.StartActionNotification();
            _actionNotificationService.UpdateActionNotification(new ActionNotification("Running Patcher...", 100));

            string[] files = Directory.GetFiles(_configService.Config.InstallPath, "Patcher.exe", new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive, MaxRecursionDepth = 0 });
            if (!files.Any())
            {
                return $"Patcher.exe not found in {_configService.Config.InstallPath}";
            }
            string patcherPath = files[0];

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
                patcherProcess.StartInfo.WorkingDirectory = _configService.Config.InstallPath;
            }

            patcherProcess.Start();
            await patcherProcess.WaitForExitAsync();

            // Success exit code
            if (patcherProcess.ExitCode == 10)
            {
                if (File.Exists(patcherPath))
                {
                    File.Delete(patcherPath);
                }

                if (File.Exists(Path.Combine(_configService.Config.InstallPath, "Patcher.log")))
                {
                    File.Delete(Path.Combine(_configService.Config.InstallPath, "Patcher.log"));
                }

                if (Directory.Exists(Path.Combine(_configService.Config.InstallPath, "Aki_Patches")))
                {
                    Directory.Delete(Path.Combine(_configService.Config.InstallPath, "Aki_Patches"), true);
                }
            }

            _actionNotificationService.StopActionNotification();

            _patcherResultMessages.TryGetValue(patcherProcess.ExitCode, out string? patcherResult);
            _logger.LogInformation($"RunPatcher: {patcherResult}");
            return patcherResult ?? "Unknown error.";
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
            if (githubReleases.Any())
            {
                foreach (GithubRelease release in githubReleases)
                {
                    var zipAsset = release.assets.Find(asset => asset.name.EndsWith(".zip"));
                    if (zipAsset != null)
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

        /// <summary>
        /// Downloads the patcher
        /// </summary>
        /// <param name="sitVersionTarget"></param>
        /// <returns></returns>
        public async Task<bool> DownloadAndRunPatcher(string url)
        {
            _logger.LogInformation("Downloading Patcher");

            if (string.IsNullOrEmpty(_configService.Config.TarkovVersion))
            {
                _logger.LogError("DownloadPatcher: TarkovVersion is 'null'");
                return false;
            }

            string patcherPath = Path.Combine(_configService.Config.InstallPath, @"Patcher.zip");
            if (File.Exists(patcherPath))
            {
                File.Delete(patcherPath);
            }

            bool downloadSuccess = await _fileService.DownloadFile("Patcher.zip", _configService.Config.InstallPath, url, true);
            if (!downloadSuccess)
            {
                _logger.LogError("Failed to download the patcher from the selected mirror.");
                return false;
            }


            if (File.Exists(patcherPath))
            {
                await _fileService.ExtractArchive(patcherPath, _configService.Config.InstallPath);
                File.Delete(patcherPath);
            }

            var patcherDir = Directory.GetDirectories(_configService.Config.InstallPath, "Patcher*").FirstOrDefault();
            if (!string.IsNullOrEmpty(patcherDir))
            {
                CloneDirectory(patcherDir, _configService.Config.InstallPath);
                Directory.Delete(patcherDir, true);
            }

            string patcherResult = await RunPatcher();
            if (patcherResult != "Patcher was successful.")
            {
                _logger.LogError($"Patcher failed: {patcherResult}");
                return false;
            }

            // If execution reaches this point, it means all necessary patchers succeeded
            _logger.LogInformation("Patcher completed successfully.");
            return true;
        }


        public async Task<Dictionary<string, string>?> GetAvaiableMirrorsForVerison(string sitVersionTarget)
        {
            Dictionary<string, string> providerLinks = new Dictionary<string, string>();
            if (_configService.Config.TarkovVersion == null)
            {
                _logger.LogError("DownloadPatcher: TarkovVersion is 'null'");
                return null;
            }

            string releasesString = await _httpClient.GetStringAsync(@"https://sitcoop.publicvm.com/api/v1/repos/SIT/Downgrade-Patches/releases");
            List<GiteaRelease> giteaReleases = JsonSerializer.Deserialize<List<GiteaRelease>>(releasesString);
            if (giteaReleases == null)
            {
                _logger.LogError("DownloadPatcher: giteaReleases is 'null'");
                return null;
            }

            string tarkovBuild = _configService.Config.TarkovVersion.Split(".").Last();
            string sitVersionTargetBuild = sitVersionTarget.Split(".").Last();

            GiteaRelease? compatibleDowngradePatcher = null;
            foreach (var release in giteaReleases)
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

            if (compatibleDowngradePatcher == null)
            {
                _logger.LogError("No applicable patcher found for the specified SIT version.");
                await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("InstallServiceErrorTitle"),
                    Content = _localizationService.TranslateSource("InstallerServiceNoDowngradePatcher"),
                    PrimaryButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk"),
                    IsPrimaryButtonEnabled = true
                }.ShowAsync();
                return null;
            }

            string mirrorsUrl = compatibleDowngradePatcher.assets.Find(q => q.name == "mirrors.json")?.browser_download_url ?? string.Empty;
            string mirrorsString = await _httpClient.GetStringAsync(mirrorsUrl);
            List<Mirrors> mirrors = JsonSerializer.Deserialize<List<Mirrors>>(mirrorsString) ?? [];

            if (mirrors == null || mirrors.Count == 0)
            {
                _logger.LogError("No download mirrors found for patcher.");
                return null;
            }

            foreach (var mirror in mirrors)
            {
                Uri uri = new(mirror.Link);
                string host = uri.Host.Replace("www.", "").Split('.')[0];
                providerLinks.TryAdd(host, mirror.Link);
            }

            if (providerLinks.Keys.Count > 0)
            {
                return providerLinks;
            }
            return null;
        }

        public async Task<List<GithubRelease>> GetSITReleases()
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

            List<GithubRelease> result = [];
            if (githubReleases.Any())
            {
                foreach (GithubRelease release in githubReleases)
                {
                    Match match = SITReleaseVersionRegex().Match(release.body);
                    if (match.Success)
                    {
                        string releasePatch = match.Value.Replace("This version works with version ", "");
                        release.tag_name = $"{release.name} - Tarkov Version: {releasePatch}";
                        release.body = releasePatch;
                        result.Add(release);
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

        public async Task InstallServer(GithubRelease selectedVersion)
        {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath))
            {
                _barNotificationService.ShowError(_localizationService.TranslateSource("InstallServiceErrorTitle"), _localizationService.TranslateSource("InstallServiceErrorInstallServerDescription"));
                return;
            }

            if (selectedVersion == null)
            {
                _logger.LogWarning("Install Server: selectVersion is 'null'");
                return;
            }

            try
            {
                // Dynamically find the asset that starts with "SITCoop" and ends with ".zip"
                var releaseAsset = selectedVersion.assets.FirstOrDefault(a => a.name.StartsWith("SITCoop") && a.name.EndsWith(".zip"));
                if (releaseAsset == null)
                {
                    _logger.LogError("No matching release asset found.");
                    return;
                }
                string releaseZipUrl = releaseAsset.browser_download_url;

                // Create the "Server" folder if SPT-Path is not configured.
                string sitServerDirectory = _configService.Config.AkiServerPath;
                if (string.IsNullOrEmpty(sitServerDirectory))
                {
                    // Navigate one level up from InstallPath
                    string baseDirectory = Directory.GetParent(_configService.Config.InstallPath)?.FullName ?? string.Empty;

                    // Define the target directory for Server within the parent directory
                    sitServerDirectory = Path.Combine(baseDirectory, "Server");
                }

                // Create SPT-AKI directory (default: Server)
                if (!Directory.Exists(sitServerDirectory))
                {
                    Directory.CreateDirectory(sitServerDirectory);
                }

                // Define the paths for download and extraction based on the SIT-Server directory
                string downloadLocation = Path.Combine(sitServerDirectory, releaseAsset.name);
                string extractionPath = sitServerDirectory;

                // Download and extract the file in Server directory
                await _fileService.DownloadFile(releaseAsset.name, sitServerDirectory, releaseZipUrl, true);
                await _fileService.ExtractArchive(downloadLocation, extractionPath);

                // Remove the downloaded Server after extraction
                File.Delete(downloadLocation);

                ManagerConfig config = _configService.Config;
                config.SitVersion = _versionService.GetSITVersion(config.InstallPath);

                // Attempt to automatically set the AKI Server Path after successful installation and save it to config
                if (!string.IsNullOrEmpty(sitServerDirectory) && string.IsNullOrEmpty(_configService.Config.AkiServerPath))
                {
                    config.AkiServerPath = sitServerDirectory;
                    _barNotificationService.ShowSuccess(_localizationService.TranslateSource("InstallServiceConfigTitle"), _localizationService.TranslateSource("InstallServiceConfigDescription", sitServerDirectory));
                }
                _configService.UpdateConfig(config);

                _barNotificationService.ShowSuccess(_localizationService.TranslateSource("InstallServiceInstallSuccessfulTitle"), _localizationService.TranslateSource("InstallServiceInstallSuccessfulDescription"));
            }
            catch (Exception ex)
            {
                // TODO ShowInfoBarWithLogButton("Install Error", "Encountered an error during installation.", InfoBarSeverity.Error, 10);
                _logger.LogError(ex, "Install Server");
            }
        }

        public async Task InstallSIT(GithubRelease selectedVersion)
        {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath))
            {
                _barNotificationService.ShowError(_localizationService.TranslateSource("InstallServiceErrorTitle"), _localizationService.TranslateSource("InstallServiceErrorInstallSITDescription"));
                return;
            }

            if (selectedVersion == null)
            {
                _logger.LogWarning("InstallSIT: selectVersion is 'null'");
                return;
            }

            // Load all the file paths up front to save my sanity of writing Path.Combine a million times
            string sitReleaseZipPath = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "CoreFiles", "StayInTarkov-Release.zip");
            string coreFilesPath = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "CoreFiles");
            string backupCoreFilesPath = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "Backup", "CoreFiles");
            string pluginsPath = Path.Combine(_configService.Config.InstallPath, "BepInEx", "plugins");
            string eftDataManagedPath = Path.Combine(_configService.Config.InstallPath, "EscapeFromTarkov_Data", "Managed");

            try
            {
                if (File.Exists(Path.Combine(_configService.Config.InstallPath, "EscapeFromTarkov_BE.exe")))
                {
                    CleanUpEFTDirectory();
                }

                if (File.Exists(sitReleaseZipPath))
                {
                    File.Delete(sitReleaseZipPath);
                }

                if (!Directory.Exists(coreFilesPath))
                    Directory.CreateDirectory(coreFilesPath);

                if (!Directory.Exists(backupCoreFilesPath))
                    Directory.CreateDirectory(backupCoreFilesPath);

                if (!Directory.Exists(pluginsPath))
                {
                    await _fileService.DownloadFile("BepInEx5.zip", Path.Combine(_configService.Config.InstallPath, "SITLauncher"), "https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip", true);
                    await _fileService.ExtractArchive(Path.Combine(_configService.Config.InstallPath, "SITLauncher", "BepInEx5.zip"), _configService.Config.InstallPath);
                    Directory.CreateDirectory(pluginsPath);
                }

                // We don't use index as they might be different from version to version
                string? releaseZipUrl = selectedVersion.assets.Find(q => q.name == "StayInTarkov-Release.zip")?.browser_download_url;

                await _fileService.DownloadFile("StayInTarkov-Release.zip", coreFilesPath, releaseZipUrl, true);
                await _fileService.ExtractArchive(Path.Combine(coreFilesPath, "StayInTarkov-Release.zip"), coreFilesPath);

                if (File.Exists(Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll")))
                    File.Copy(Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll"), Path.Combine(backupCoreFilesPath, "Assembly-CSharp.dll"), true);
                File.Copy(Path.Combine(coreFilesPath, "StayInTarkov-Release", "Assembly-CSharp.dll"), Path.Combine(eftDataManagedPath, "Assembly-CSharp.dll"), true);

                File.Copy(Path.Combine(coreFilesPath, "StayInTarkov-Release", "StayInTarkov.dll"), Path.Combine(pluginsPath, "StayInTarkov.dll"), true);

                using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT.Manager.Avalonia.Resources.Aki.Common.dll"))
                {
                    using (FileStream file = new(Path.Combine(eftDataManagedPath, "Aki.Common.dll"), FileMode.Create, FileAccess.Write))
                    {
                        if (resource != null)
                        {
                            await resource.CopyToAsync(file);
                        }
                    }
                }

                using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT.Manager.Avalonia.Resources.Aki.Reflection.dll"))
                {
                    using (FileStream file = new FileStream(Path.Combine(eftDataManagedPath, "Aki.Reflection.dll"), FileMode.Create, FileAccess.Write))
                    {
                        if (resource != null)
                        {
                            await resource.CopyToAsync(file);
                        }
                    }
                }

                ManagerConfig config = _configService.Config;
                config.SitVersion = _versionService.GetSITVersion(config.InstallPath);
                _configService.UpdateConfig(config);

                _barNotificationService.ShowSuccess(_localizationService.TranslateSource("InstallServiceInstallSuccessfulTitle"), _localizationService.TranslateSource("InstallServiceInstallSITSuccessfulDescription"));
            }
            catch (Exception ex)
            {
                // TODO ShowInfoBarWithLogButton("Install Error", "Encountered an error during installation.", InfoBarSeverity.Error, 10);
                _logger.LogError(ex, "Install SIT");
            }
        }
    }
}