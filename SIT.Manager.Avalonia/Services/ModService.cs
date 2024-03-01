using Avalonia.Controls;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services
{
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

        /// <summary>
        /// Handy function to compactly translate source code.
        /// </summary>
        /// <param name="key">key in the resources</param>
        /// <param name="parameters">the paramaters that was inside the source string. will be replaced by hierarchy where %1 .. %n is the first paramater.</param>
        private string Translate(string key, params string[] parameters) => _localizationService.TranslateSource(key, parameters);

        public async Task DownloadModsCollection() {
            string modsDirectory = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "Mods");
            if (!Directory.Exists(modsDirectory)) {
                Directory.CreateDirectory(modsDirectory);
            }

            string[] subDirs = Directory.GetDirectories(modsDirectory);
            foreach (string subDir in subDirs) {
                Directory.Delete(subDir, true);
            }
            Directory.CreateDirectory(Path.Combine(modsDirectory, "Extracted"));

            await _filesService.DownloadFile("SIT.Mod.Ports.Collection.zip", modsDirectory, MOD_COLLECTION_URL, true);
            await _filesService.ExtractArchive(Path.Combine(modsDirectory, "SIT.Mod.Ports.Collection.zip"), Path.Combine(modsDirectory, "Extracted"));
        }

        public async Task AutoUpdate(List<ModInfo> outdatedMods) {
            List<string> outdatedNames = [.. outdatedMods.Select(x => x.Name)];
            string outdatedString = string.Join("\n", outdatedNames);

            ScrollViewer scrollView = new() {
                Content = new TextBlock() {
                    Text = Translate("ModServiceOutdatedDescription", $"{outdatedMods.Count}", outdatedString)
                }
            };

            ContentDialog contentDialog = new() {
                Title = Translate("ModServiceOutdatedModsFoundTitle"),
                Content = scrollView,
                CloseButtonText = Translate("ModServiceButtonNo"),
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = Translate("ModServiceButtonYes")
            };

            ContentDialogResult result = await contentDialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                foreach (ModInfo mod in outdatedMods) {
                    ManagerConfig config = _configService.Config;
                    config.InstalledMods.Remove(mod.Name);
                    _configService.UpdateConfig(config);

                    await InstallMod(mod, true);
                }
            }
            else {
                return;
            }

            _barNotificationService.ShowSuccess(Translate("ModServiceUpdatedModsTitle"), Translate("ModServiceUpdatedModsDescription", $"{outdatedMods.Count}"));
        }

        public async Task<bool> InstallMod(ModInfo mod, bool suppressNotification = false) {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath)) {
                _barNotificationService.ShowError(Translate("ModServiceInstallModTitle"), Translate("ModServiceInstallErrorModDescription"));
                return false;
            }

            try {
                if (mod.SupportedVersion != _configService.Config.SitVersion) {
                    ContentDialog contentDialog = new() {
                        Title = Translate("ModServiceWarningTitle"),
                        Content = Translate("ModServiceWarningDescription", mod.SupportedVersion, $"{(string.IsNullOrEmpty(_configService.Config.SitVersion) ? Translate("ModServiceUnknownTitle") : _configService.Config.SitVersion)}"),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = Translate("ModServiceButtonYes"),
                        CloseButtonText = Translate("ModServiceButtonNo")
                    };
                    ContentDialogResult response = await contentDialog.ShowAsync();
                    if (response != ContentDialogResult.Primary) {
                        return false;
                    }
                }

                if (mod == null) {
                    return false;
                }

                string installPath = _configService.Config.InstallPath;
                string gamePluginsPath = Path.Combine(installPath, "BepInEx", "plugins");
                string gameConfigPath = Path.Combine(installPath, "BepInEx", "config");

                foreach (string pluginFile in mod.PluginFiles) {
                    string sourcePath = Path.Combine(installPath, "SITLauncher", "Mods", "Extracted", "plugins", pluginFile);
                    string targetPath = Path.Combine(gamePluginsPath, pluginFile);
                    File.Copy(sourcePath, targetPath, true);
                }

                foreach (string? configFile in mod.ConfigFiles) {
                    string sourcePath = Path.Combine(installPath, "SITLauncher", "Mods", "Extracted", "config", configFile);
                    string targetPath = Path.Combine(gameConfigPath + configFile);
                    File.Copy(sourcePath, targetPath, true);
                }

                ManagerConfig config = _configService.Config;
                config.InstalledMods.Add(mod.Name, mod.PortVersion);
                _configService.UpdateConfig(config);

                if (!suppressNotification) {
                    _barNotificationService.ShowSuccess(Translate("ModServiceInstallModTitle"), Translate("ModServiceInstallModDescription", mod.Name));
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "InstallMod");
                _barNotificationService.ShowError(Translate("ModServiceInstallModTitle"), Translate("ModServiceErrorInstallModDescription", mod.Name));
                return false;
            }

            return true;
        }

        public async Task<bool> UninstallMod(ModInfo mod) {
            try {
                if (mod == null || string.IsNullOrEmpty(_configService.Config.InstallPath)) {
                    return false;
                }

                string installPath = _configService.Config.InstallPath;
                string gamePluginsPath = Path.Combine(installPath, "BepInEx", "plugins");
                string gameConfigPath = Path.Combine(installPath, "BepInEx", "config");

                foreach (string pluginFile in mod.PluginFiles) {
                    string targetPath = Path.Combine(gamePluginsPath, pluginFile);
                    if (File.Exists(targetPath)) {
                        File.Delete(targetPath);
                    }
                    else {
                        ContentDialog dialog = new() {
                            Title = Translate("ModServiceErrorUninstallModTitle"),
                            Content = Translate("ModServiceErrorUninstallModDescription", mod.Name, pluginFile),
                            CloseButtonText = Translate("ModServiceButtonNo"),
                            IsPrimaryButtonEnabled = true,
                            PrimaryButtonText = Translate("ModServiceButtonYes")
                        };

                        ContentDialogResult result = await dialog.ShowAsync();
                        if (result != ContentDialogResult.Primary) {
                            throw new FileNotFoundException(Translate("ModServiceErrorExceptionUninstallModDescription", mod.Name, pluginFile));
                        }
                    }
                }

                foreach (var configFile in mod.ConfigFiles) {
                    string targetPath = Path.Combine(gamePluginsPath, configFile);
                    if (File.Exists(targetPath)) {
                        File.Delete(targetPath);
                    }
                    else {
                        ContentDialog dialog = new() {
                            Title = Translate("ModServiceErrorUninstallModTitle"),
                            Content = Translate("ModServiceErrorExceptionUninstallModDescription", mod.Name, configFile),
                            CloseButtonText = Translate("ModServiceButtonNo"),
                            IsPrimaryButtonEnabled = true,
                            PrimaryButtonText = Translate("ModServiceButtonYes")
                        };

                        ContentDialogResult result = await dialog.ShowAsync();

                        if (result != ContentDialogResult.Primary) {
                            throw new FileNotFoundException(Translate("ModServiceErrorExceptionFileUninstallModDescription", mod.Name, configFile));
                        }
                    }
                }

                ManagerConfig config = _configService.Config;
                config.InstalledMods.Remove(mod.Name);
                _configService.UpdateConfig(config);

                _barNotificationService.ShowSuccess(Translate("ModServiceFileUninstallModTitle"), Translate("ModServiceFileUninstallModDescription", mod.Name));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "UninstallMod");
                _barNotificationService.ShowError(Translate("ModServiceFileUninstallModTitle"), Translate("ModServiceErrorInstallModDescription", mod.Name));
                return false;
            }

            return true;
        }
    }
}