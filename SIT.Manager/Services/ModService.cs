using Avalonia.Controls;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Config;
using SIT.Manager.Models.Github;
using SIT.Manager.Services.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public class ModService(IBarNotificationService barNotificationService,
                        ICachingService cachingService,
                        IFileService filesService,
                        ILocalizationService localizationService,
                        IManagerConfigService configService,
                        HttpClient httpClient,
                        ILogger<ModService> logger) : IModService
{
    private const string BEPINEX_CONFIGURATION_MANAGER_RELEASE_URL = "https://api.github.com/repos/BepInEx/BepInEx.ConfigurationManager/releases/latest";
    private const string CONFIGURATION_MANAGER_ZIP_CACHE_KEY = "configuration-manager-dll";
    private const string MOD_COLLECTION_URL = "https://github.com/stayintarkov/SIT-Mod-Ports/releases/latest/download/SIT.Mod.Ports.Collection.zip";

    private readonly IBarNotificationService _barNotificationService = barNotificationService;
    private readonly ICachingService _cachingService = cachingService;
    private readonly IFileService _filesService = filesService;
    private readonly IManagerConfigService _configService = configService;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ModService> _logger = logger;
    private readonly ILocalizationService _localizationService = localizationService;

    // Stores the downloaded mods and caches it in the manager's directory
    private readonly string _localModCache = Path.Combine(AppContext.BaseDirectory, "Mods");

    public string[] RecommendedModInstalls => ["ConfigurationManager"];

    public List<ModInfo> ModList { get; private set; } = [];

    private async Task InstallFiles(string baseSourceDirectory, string baseTargetDirectory, List<string> files)
    {
        // Ensure that the directory that we are trying to copy to exists
        Directory.CreateDirectory(baseTargetDirectory);

        foreach (string file in files)
        {
            string sourcePath = Path.Combine(baseSourceDirectory, file);
            string targetPath = Path.Combine(baseTargetDirectory, file);
            await _filesService.CopyFileAsync(sourcePath, targetPath).ConfigureAwait(false);
        }
    }

    private async Task UninstallFiles(string baseInstallDirectory, List<string> files, string modName)
    {
        foreach (string file in files)
        {
            string targetPath = Path.Combine(baseInstallDirectory, file);
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            else
            {
                ContentDialog dialog = new()
                {
                    Title = _localizationService.TranslateSource("ModServiceErrorUninstallModTitle"),
                    Content = _localizationService.TranslateSource("ModServiceErrorUninstallModDescription", modName, file),
                    CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo"),
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes")
                };

                ContentDialogResult result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    throw new FileNotFoundException(_localizationService.TranslateSource("ModServiceErrorExceptionUninstallModDescription", modName, modName));
                }
            }
        }
    }

    public void ClearCache()
    {
        string[] subDirs = Directory.GetDirectories(_localModCache);
        foreach (string subDir in subDirs)
        {
            Directory.Delete(subDir, true);
        }
    }

    public async Task AutoUpdate(List<ModInfo> outdatedMods)
    {
        List<string> outdatedNames = [.. outdatedMods.Select(x => x.Name)];
        string outdatedString = string.Join("\n", outdatedNames);

        ScrollViewer scrollView = new()
        {
            Content = new TextBlock()
            {
                Text = _localizationService.TranslateSource("ModServiceOutdatedDescription", $"{outdatedMods.Count}", outdatedString)
            }
        };

        ContentDialog contentDialog = new()
        {
            Title = _localizationService.TranslateSource("ModServiceOutdatedModsFoundTitle"),
            Content = scrollView,
            CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo"),
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes")
        };

        ContentDialogResult result = await contentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            foreach (ModInfo mod in outdatedMods)
            {
                ManagerConfig config = _configService.Config;
                config.InstalledMods.Remove(mod.Name);
                _configService.UpdateConfig(config);
                await InstallMod(_configService.Config.SitEftInstallPath, mod, true).ConfigureAwait(false);
            }
        }
        else
        {
            return;
        }

        _barNotificationService.ShowSuccess(_localizationService.TranslateSource("ModServiceUpdatedModsTitle"), _localizationService.TranslateSource("ModServiceUpdatedModsDescription", $"{outdatedMods.Count}"));
    }

    public async Task<bool> InstallConfigurationManager(string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            throw new ArgumentException("Parameter can't be null or empty", nameof(targetPath));
        }

        CacheValue<byte[]> configurationManagerDll = await _cachingService.OnDisk.GetOrComputeAsync(CONFIGURATION_MANAGER_ZIP_CACHE_KEY,
            async (key) =>
            {
                string latestReleaseJson = await _httpClient.GetStringAsync(BEPINEX_CONFIGURATION_MANAGER_RELEASE_URL).ConfigureAwait(false);
                GithubRelease? latestRelease = JsonSerializer.Deserialize<GithubRelease>(latestReleaseJson);

                if (latestRelease != null)
                {
                    string assetDownloadUrl = string.Empty;
                    foreach (GithubAsset asset in latestRelease.Assets)
                    {
                        if (asset.Name.Contains("BepInEx5"))
                        {
                            assetDownloadUrl = asset.BrowserDownloadUrl;
                        }
                    }

                    if (string.IsNullOrEmpty(assetDownloadUrl))
                    {
                        throw new UriFormatException("No download url available for Configuration manager");
                    }

                    string tmpZipFilePath = Path.GetTempFileName();
                    bool downloadSuccess = await _filesService.DownloadFile(Path.GetFileName(tmpZipFilePath), Path.GetDirectoryName(tmpZipFilePath) ?? string.Empty, assetDownloadUrl, new Progress<double>()).ConfigureAwait(false);
                    if (downloadSuccess)
                    {
                        string extractedZipPath = Path.GetTempFileName();
                        if (File.Exists(extractedZipPath))
                        {
                            File.Delete(extractedZipPath);
                        }
                        await _filesService.ExtractArchive(tmpZipFilePath, extractedZipPath).ConfigureAwait(false);

                        string[] files = Directory.GetFiles(extractedZipPath, "ConfigurationManager.dll", new EnumerationOptions() { RecurseSubdirectories = true });
                        if (files.Length == 1)
                        {
                            return await File.ReadAllBytesAsync(files[0]).ConfigureAwait(false);
                        }
                        else
                        {
                            throw new FileNotFoundException("Found too many or too few Configuration manager dlls in extraction target");
                        }
                    }
                    else
                    {
                        throw new IOException("Failed to download the latest configuration manager from GitHub");
                    }
                }
                throw new FileNotFoundException("Failed to get the latest release for Configuration Manager");
            }, TimeSpan.FromDays(1));

        byte[] configurationManagerBytes = configurationManagerDll.Value ?? [];
        if (configurationManagerBytes.Length == 0)
        {
            throw new FileNotFoundException("Failed find and install Configuration Manager");
        }

        string targetInstallLocation = Path.Combine(targetPath, "BepInEx", "plugins", "ConfigurationManager.dll");
        await File.WriteAllBytesAsync(targetInstallLocation, configurationManagerBytes).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> InstallMod(string targetPath, ModInfo mod, bool suppressNotification = false, bool suppressCompatibilityWarning = false)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceInstallErrorModDescription"));
            return false;
        }

        try
        {
            // Ignore the mod compatibility alert if the version is set to '*' and/or we have supressed the compatibility notice.
            if (mod.SupportedVersion != "*" && mod.SupportedVersion != _configService.Config.SitVersion && !suppressCompatibilityWarning)
            {
                ContentDialog contentDialog = new()
                {
                    Title = _localizationService.TranslateSource("ModServiceWarningTitle"),
                    Content = _localizationService.TranslateSource("ModServiceWarningDescription", mod.SupportedVersion, $"{(string.IsNullOrEmpty(_configService.Config.SitVersion) ? _localizationService.TranslateSource("ModServiceUnknownTitle") : _configService.Config.SitVersion)}"),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes"),
                    CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo")
                };
                ContentDialogResult response = await contentDialog.ShowAsync();
                if (response != ContentDialogResult.Primary)
                {
                    return false;
                }
            }

            if (mod == null)
            {
                return false;
            }

            ManagerConfig config = _configService.Config;
            if (!config.InstalledMods.ContainsKey(mod.Name))
            {
                string baseModSourcePath = Path.Combine(_localModCache, "Extracted");

                // Install any plugin files
                await InstallFiles(Path.Combine(baseModSourcePath, "plugins"), Path.Combine(targetPath, "BepInEx", "plugins"), mod.PluginFiles).ConfigureAwait(false);
                // Install any config files
                await InstallFiles(Path.Combine(baseModSourcePath, "config"), Path.Combine(targetPath, "BepInEx", "config"), mod.ConfigFiles).ConfigureAwait(false);
                // Install any patcher files
                await InstallFiles(Path.Combine(baseModSourcePath, "patchers"), Path.Combine(targetPath, "BepInEx", "patchers"), mod.PatcherFiles).ConfigureAwait(false);

                config.InstalledMods.Add(mod.Name, mod.PortVersion);
                _configService.UpdateConfig(config);
            }

            if (!suppressNotification)
            {
                _barNotificationService.ShowSuccess(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceInstallModDescription", mod.Name));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InstallMod");
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceErrorInstallModDescription", mod.Name));
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task LoadMasterModList()
    {
        ModList.Clear();

        string modsDirectory = Path.Combine(_localModCache, "Extracted");
        List<ModInfo> outdatedMods = [];

        string modsListFile = Path.Combine(modsDirectory, "MasterList.json");
        if (!File.Exists(modsListFile))
        {
            ModList.Add(new ModInfo()
            {
                Name = _localizationService.TranslateSource("ModsPageViewModelErrorNoModsFound")
            });
            return;
        }

        string masterListFile = await File.ReadAllTextAsync(modsListFile);
        List<ModInfo> masterList = JsonSerializer.Deserialize<List<ModInfo>>(masterListFile) ?? [];
        masterList = [.. masterList.OrderBy(x => x.Name)];

        ModList.AddRange(masterList);
    }

    public async Task<bool> UninstallMod(string targetPath, ModInfo mod)
    {
        try
        {
            if (mod == null || string.IsNullOrEmpty(targetPath))
            {
                return false;
            }

            // Uninstall any plugin files
            await UninstallFiles(Path.Combine(targetPath, "BepInEx", "plugins"), mod.PluginFiles, mod.Name).ConfigureAwait(false);
            // Uninstall any config files
            await UninstallFiles(Path.Combine(targetPath, "BepInEx", "config"), mod.ConfigFiles, mod.Name).ConfigureAwait(false);
            // Uninstall any patcher files
            await UninstallFiles(Path.Combine(targetPath, "BepInEx", "patchers"), mod.ConfigFiles, mod.Name).ConfigureAwait(false);

            ManagerConfig config = _configService.Config;
            config.InstalledMods.Remove(mod.Name);
            _configService.UpdateConfig(config);

            _barNotificationService.ShowSuccess(_localizationService.TranslateSource("ModServiceFileUninstallModTitle"), _localizationService.TranslateSource("ModServiceFileUninstallModDescription", mod.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UninstallMod");
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceFileUninstallModTitle"), _localizationService.TranslateSource("ModServiceErrorInstallModDescription", mod.Name));
            return false;
        }

        return true;
    }
}
