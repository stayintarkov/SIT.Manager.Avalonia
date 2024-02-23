using Avalonia.Controls;
using Avalonia.Layout;
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
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services
{
    public class InstallerService(IBarNotificationService barNotificationService,
                                  IManagerConfigService configService,
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
        public void CleanUpEFTDirectory() {
            _logger.LogInformation("Cleaning up EFT directory...");
            try {
                string battlEyeDir = Path.Combine(_configService.Config.InstallPath, "BattlEye");
                if (Directory.Exists(battlEyeDir)) {
                    Directory.Delete(battlEyeDir, true);
                }
                string battlEyeExe = Path.Combine(_configService.Config.InstallPath, "EscapeFromTarkov_BE.exe");
                if (File.Exists(battlEyeExe)) {
                    File.Delete(battlEyeExe);
                }
                string cacheDir = Path.Combine(_configService.Config.InstallPath, "cache");
                if (Directory.Exists(cacheDir)) {
                    Directory.Delete(cacheDir, true);
                }
                string consistencyPath = Path.Combine(_configService.Config.InstallPath, "ConsistencyInfo");
                if (File.Exists(consistencyPath)) {
                    File.Delete(consistencyPath);
                }
                string uninstallPath = Path.Combine(_configService.Config.InstallPath, "Uninstall.exe");
                if (File.Exists(uninstallPath)) {
                    File.Delete(uninstallPath);
                }
                string logsDirPath = Path.Combine(_configService.Config.InstallPath, "Logs");
                if (Directory.Exists(logsDirPath)) {
                    Directory.Delete(logsDirPath);
                }
            }
            catch (Exception ex) {
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
        private void CloneDirectory(string root, string dest) {
            foreach (var directory in Directory.GetDirectories(root)) {
                var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
                Directory.CreateDirectory(newDirectory);
                CloneDirectory(directory, newDirectory);
            }

            foreach (var file in Directory.GetFiles(root)) {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }

        /// <summary>
        /// Downloads the patcher
        /// </summary>
        /// <param name="sitVersionTarget"></param>
        /// <returns></returns>
        private async Task<bool> DownloadAndRunPatcher(string sitVersionTarget = "") {
            _logger.LogInformation("Downloading Patcher");

            if (string.IsNullOrEmpty(_configService.Config.TarkovVersion)) {
                _logger.LogError("DownloadPatcher: TarkovVersion is 'null'");
                return false;
            }

            string releasesString = await _httpClient.GetStringAsync(@"https://sitcoop.publicvm.com/api/v1/repos/SIT/Downgrade-Patches/releases");
            List<GiteaRelease>? giteaReleases = JsonSerializer.Deserialize<List<GiteaRelease>>(releasesString);
            if (giteaReleases == null) {
                _logger.LogError("DownloadPatcher: giteaReleases is 'null'");
                return false;
            }

            List<GiteaRelease> patcherList = [];
            string tarkovBuild = _configService.Config.TarkovVersion.Split(".").Last();
            string sitBuild = sitVersionTarget.Split(".").Last();
            string tarkovVersionToDowngrade = tarkovBuild != sitBuild ? tarkovBuild : "";

            if (string.IsNullOrEmpty(tarkovVersionToDowngrade)) {
                _logger.LogError("DownloadPatcher: tarkovVersionToDowngrade is 'null'");
                return false;
            }

            foreach (GiteaRelease release in giteaReleases) {
                string[] splitRelease = release.name.Split("to");
                if (splitRelease.Length != 2) {
                    return false;
                }

                string patcherFrom = splitRelease[0].Trim();
                string patcherTo = splitRelease[1].Trim();

                if (patcherFrom == tarkovVersionToDowngrade) {
                    patcherList.Add(release);
                    tarkovVersionToDowngrade = patcherTo;
                }
            }

            if (patcherList.Count == 0 && _configService.Config.SitVersion != sitVersionTarget) {
                _logger.LogError("No applicable patcher found for the specified SIT version.");
                return false;
            }

            foreach (var patcher in patcherList) {
                string mirrorsUrl = patcher.assets.Find(q => q.name == "mirrors.json")?.browser_download_url ?? string.Empty;
                if (string.IsNullOrEmpty(mirrorsUrl)) {
                    _logger.LogError("No mirrors url found in mirrors.json.");
                    return false;
                }

                string mirrorsString = await _httpClient.GetStringAsync(mirrorsUrl);
                List<Mirrors>? mirrors = JsonSerializer.Deserialize<List<Mirrors>>(mirrorsString);
                if (mirrors == null || mirrors.Count == 0) {
                    _logger.LogError("No download mirrors found for patcher.");
                    return false;
                }

                string selectedMirrorUrl = await ShowMirrorSelectionDialog(mirrors);
                if (string.IsNullOrEmpty(selectedMirrorUrl)) {
                    _logger.LogWarning("Mirror selection was canceled or no mirror was selected.");
                    return false;
                }

                bool downloadSuccess = await _fileService.DownloadFile("Patcher.zip", _configService.Config.InstallPath, selectedMirrorUrl, true);
                if (!downloadSuccess) {
                    _logger.LogError("Failed to download the patcher from the selected mirror.");
                    return false;
                }

                await _fileService.ExtractArchive(Path.Combine(_configService.Config.InstallPath, @"Patcher.zip"), _configService.Config.InstallPath);
                var patcherDir = Directory.GetDirectories(_configService.Config.InstallPath, "Patcher*").FirstOrDefault();
                if (!string.IsNullOrEmpty(patcherDir)) {
                    CloneDirectory(patcherDir, _configService.Config.InstallPath);
                    Directory.Delete(patcherDir, true);
                }

                string patcherResult = await RunPatcher();
                if (patcherResult != "Patcher was successful.") {
                    _logger.LogError($"Patcher failed: {patcherResult}");
                    return false;
                }
            }

            // If execution reaches this point, it means all necessary patchers succeeded
            _logger.LogInformation("Patcher completed successfully.");
            return true;
        }

        /// <summary>
        /// Runs the downgrade patcher
        /// </summary>
        /// <returns>string with result</returns>
        private async Task<string> RunPatcher() {
            _logger.LogInformation("Starting Patcher");
            string patcherPath = Path.Combine(_configService.Config.InstallPath, "Patcher.exe");
            if (!File.Exists(patcherPath)) {
                return $"Patcher.exe not found at {patcherPath}";
            }

            Process patcherProcess = new() {
                StartInfo = new() {
                    FileName = patcherPath,
                    WorkingDirectory = _configService.Config.InstallPath,
                    Arguments = "autoclose"
                },
                EnableRaisingEvents = true
            };
            patcherProcess.Start();
            await patcherProcess.WaitForExitAsync();

            // Success exit code
            if (patcherProcess.ExitCode == 10) {
                if (File.Exists(patcherPath)) {
                    File.Delete(patcherPath);
                }

                if (File.Exists(Path.Combine(_configService.Config.InstallPath, "Patcher.log"))) {
                    File.Delete(Path.Combine(_configService.Config.InstallPath, "Patcher.log"));
                }

                if (Directory.Exists(Path.Combine(_configService.Config.InstallPath, "Aki_Patches"))) {
                    Directory.Delete(Path.Combine(_configService.Config.InstallPath, "Aki_Patches"), true);
                }
            }

            _patcherResultMessages.TryGetValue(patcherProcess.ExitCode, out string? patcherResult);
            _logger.LogInformation($"RunPatcher: {patcherResult}");
            return patcherResult ?? "Unknown error.";
        }

        /// <summary>
        /// Shows a dialog for the user to select a download mirror.
        /// </summary>
        /// <param name="mirrors">List of mirrors to choose from.</param>
        /// <returns>The URL of the selected mirror or null if canceled.</returns>
        private async Task<string> ShowMirrorSelectionDialog(List<Mirrors> mirrors) {
            Dictionary<string, string> providerLinks = [];
            foreach (var mirror in mirrors) {
                Uri uri = new(mirror.Link);
                string host = uri.Host.Replace("www.", "").Split('.')[0];
                providerLinks.TryAdd(host, mirror.Link);
            }

            // Wrap the ComboBox in a StackPanel for alignment
            StackPanel contentPanel = new() {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            ComboBox mirrorComboBox = new() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 300
            };
            foreach (var provider in providerLinks.Keys) {
                mirrorComboBox.Items.Add(new ComboBoxItem { Content = provider });
            }
            contentPanel.Children.Add(mirrorComboBox);

            ContentDialog selectionDialog = new() {
                Title = "Select Download Mirror",
                PrimaryButtonText = "Download",
                CloseButtonText = "Cancel",
                Content = contentPanel
            };

            var result = await selectionDialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                string? selectedProvider = (mirrorComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(selectedProvider)) {
                    return providerLinks[selectedProvider];
                }
            }
            return string.Empty;
        }

        public async Task InstallServer(GithubRelease selectedVersion) {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath)) {
                _barNotificationService.ShowError("Error", "Install Path is not set. Configure it in Settings.");
                return;
            }

            if (selectedVersion == null) {
                _logger.LogWarning("Install Server: selectVersion is 'null'");
                return;
            }

            ManagerConfig config;
            try {
                if (_configService.Config.TarkovVersion != selectedVersion.body) {
                    await DownloadAndRunPatcher(selectedVersion.body);

                    string tarkovVersion = _versionService.GetEFTVersion(_configService.Config.InstallPath);

                    config = _configService.Config;
                    config.TarkovVersion = tarkovVersion;
                    _configService.UpdateConfig(config);
                }

                string targetFileName = "Aki-Server-win-with-SITCoop.zip";

                // We don't use index as they might be different from version to version
                string? releaseZipUrl = selectedVersion.assets.Find(q => q.name == targetFileName)?.browser_download_url;

                // Navigate one level up from InstallPath
                string baseDirectory = Directory.GetParent(_configService.Config.InstallPath)?.FullName ?? string.Empty;

                // Define the target directory for SIT-Server within the parent directory
                string sitServerDirectory = Path.Combine(baseDirectory, "SIT-Server");

                Directory.CreateDirectory(sitServerDirectory);

                // Define the paths for download and extraction based on the SIT-Server directory
                string downloadLocation = Path.Combine(sitServerDirectory, targetFileName);
                string extractionPath = sitServerDirectory;

                // Download and extract the file in SIT-Server directory
                await _fileService.DownloadFile(targetFileName, sitServerDirectory, releaseZipUrl, true);
                await _fileService.ExtractArchive(downloadLocation, extractionPath);

                // Remove the downloaded SIT-Server after extraction
                File.Delete(downloadLocation);

                config = _configService.Config;

                string sitVersion = _versionService.GetSITVersion(_configService.Config.InstallPath);
                config.SitVersion = sitVersion;

                // Attempt to automatically set the AKI Server Path after successful installation
                if (!string.IsNullOrEmpty(sitServerDirectory)) {
                    config.AkiServerPath = sitServerDirectory;
                    _barNotificationService.ShowSuccess("Config", $"Server installation path automatically set to '{sitServerDirectory}'");
                }
                else {
                    // Optional: Notify user that automatic path detection failed and manual setting is needed
                    _barNotificationService.ShowWarning("Notice", "Automatic Server path detection failed. Please set it manually.");
                }

                _configService.UpdateConfig(config);

                _barNotificationService.ShowSuccess("Install", "Installation of Server was succesful.");
            }
            catch (Exception ex) {
                // TODO ShowInfoBarWithLogButton("Install Error", "Encountered an error during installation.", InfoBarSeverity.Error, 10);
                _logger.LogError(ex, "Install Server");
            }
        }

        public async Task InstallSIT(GithubRelease selectedVersion) {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath)) {
                _barNotificationService.ShowError("Error", "Install Path is not set. Configure it in Settings.");
                return;
            }

            if (selectedVersion == null) {
                _logger.LogWarning("InstallSIT: selectVersion is 'null'");
                return;
            }

            // Load all the file paths up front to save my sanity of writing Path.Combine a million times
            string sitReleaseZipPath = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "CoreFiles", "StayInTarkov-Release.zip");
            string coreFilesPath = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "CoreFiles");
            string backupCoreFilesPath = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "Backup", "CoreFiles");
            string pluginsPath = Path.Combine(_configService.Config.InstallPath, "BepInEx", "plugins");
            string eftDataManagedPath = Path.Combine(_configService.Config.InstallPath, "EscapeFromTarkov_Data", "Managed");

            try {
                if (File.Exists(Path.Combine(_configService.Config.InstallPath, "EscapeFromTarkov_BE.exe"))) {
                    CleanUpEFTDirectory();
                }

                if (File.Exists(sitReleaseZipPath)) {
                    File.Delete(sitReleaseZipPath);
                }

                ManagerConfig config;
                if (_configService.Config.TarkovVersion != selectedVersion.body) {
                    await DownloadAndRunPatcher(selectedVersion.body);

                    string tarkovVersion = _versionService.GetEFTVersion(_configService.Config.InstallPath);

                    config = _configService.Config;
                    config.TarkovVersion = tarkovVersion;
                    _configService.UpdateConfig(config);
                }

                if (!Directory.Exists(coreFilesPath))
                    Directory.CreateDirectory(coreFilesPath);

                if (!Directory.Exists(backupCoreFilesPath))
                    Directory.CreateDirectory(backupCoreFilesPath);

                if (!Directory.Exists(pluginsPath)) {
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

                using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT.Manager.Resources.Aki.Common.dll")) {
                    using (FileStream file = new(Path.Combine(eftDataManagedPath, "Aki.Common.dll"), FileMode.Create, FileAccess.Write)) {
                        if (resource != null) {
                            await resource.CopyToAsync(file);
                        }
                    }
                }

                using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT.Manager.Resources.Aki.Reflection.dll")) {
                    using (FileStream file = new FileStream(Path.Combine(eftDataManagedPath, "Aki.Reflection.dll"), FileMode.Create, FileAccess.Write)) {
                        if (resource != null) {
                            await resource.CopyToAsync(file);
                        }
                    }
                }

                config = _configService.Config;
                string sitVersion = _versionService.GetSITVersion(_configService.Config.InstallPath);
                config.SitVersion = sitVersion;
                _configService.UpdateConfig(config);

                _barNotificationService.ShowSuccess("Install", "Installation of SIT was succesful.");
            }
            catch (Exception ex) {
                // TODO ShowInfoBarWithLogButton("Install Error", "Encountered an error during installation.", InfoBarSeverity.Error, 10);
                _logger.LogError(ex, "Install SIT");
            }
        }
    }
}
