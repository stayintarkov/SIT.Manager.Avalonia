using Avalonia.Controls;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public class ModService(IBarNotificationService barNotificationService,
                        IFileService filesService,
                        ILocalizationService localizationService,
                        IManagerConfigService configService,
                        ILogger<ModService> logger) : IModService
{
    private const string MOD_COLLECTION_URL = "https://github.com/stayintarkov/SIT-Mod-Ports/releases/latest/download/SIT.Mod.Ports.Collection.zip";

    private readonly IBarNotificationService _barNotificationService = barNotificationService;
    private readonly IFileService _filesService = filesService;
    private readonly IManagerConfigService _configService = configService;
    private readonly ILogger<ModService> _logger = logger;
    private readonly ILocalizationService _localizationService = localizationService;

    // Stores the downloaded mods and caches it in the manager's directory
    private readonly string _localModCache = Path.Combine(AppContext.BaseDirectory, "Mods");

    public string[] RecommendedModInstalls => ["ConfigurationManager"];

    public List<ModInfo> ModList { get; private set; } = [];

    private async Task InstallFiles(string baseSourceDirectory, string baseTargetDirectory, List<string> files)
    {
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

    public async Task DownloadModsCollection()
    {
        Directory.CreateDirectory(_localModCache);
        await Task.Run(ClearCache).ConfigureAwait(false);

        string extractedModsDir = Path.Combine(_localModCache, "Extracted");
        Directory.CreateDirectory(extractedModsDir);

        await _filesService.DownloadFile("SIT.Mod.Ports.Collection.zip", _localModCache, MOD_COLLECTION_URL, true).ConfigureAwait(false);
        await _filesService.ExtractArchive(Path.Combine(_localModCache, "SIT.Mod.Ports.Collection.zip"), extractedModsDir).ConfigureAwait(false);
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
                await InstallMod(_configService.Config.InstallPath, mod, true).ConfigureAwait(false);
            }
        }
        else
        {
            return;
        }

        _barNotificationService.ShowSuccess(_localizationService.TranslateSource("ModServiceUpdatedModsTitle"), _localizationService.TranslateSource("ModServiceUpdatedModsDescription", $"{outdatedMods.Count}"));
    }

    public async Task<bool> InstallMod(string targetPath, ModInfo mod, bool suppressNotification = false)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceInstallErrorModDescription"));
            return false;
        }

        try
        {
            if (mod.SupportedVersion != _configService.Config.SitVersion)
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
                await InstallFiles(Path.Combine(baseModSourcePath, "patchers"), Path.Combine(targetPath, "BepInEx", "patchers"), mod.ConfigFiles).ConfigureAwait(false);

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
