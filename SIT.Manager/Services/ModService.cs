using Avalonia.Controls;
using Avalonia.Layout;
using CsQuery;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using SIT.Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

    public async Task DownloadModsCollection()
    {
        string modsDirectory = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "Mods");
        if (!Directory.Exists(modsDirectory))
        {
            Directory.CreateDirectory(modsDirectory);
        }

        string[] subDirs = Directory.GetDirectories(modsDirectory);
        foreach (string subDir in subDirs)
        {
            Directory.Delete(subDir, true);
        }
        Directory.CreateDirectory(Path.Combine(modsDirectory, "Extracted"));

        await _filesService.DownloadFile("SIT.Mod.Ports.Collection.zip", modsDirectory, MOD_COLLECTION_URL, true);
        await _filesService.ExtractArchive(Path.Combine(modsDirectory, "SIT.Mod.Ports.Collection.zip"), Path.Combine(modsDirectory, "Extracted"));
    }

    public async Task AutoUpdate(List<ModInfo> outdatedMods, List<ModInfo> modList)
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

                await InstallMod(mod, modList, true, true);
            }
        }
        else
        {
            return;
        }

        _barNotificationService.ShowSuccess(_localizationService.TranslateSource("ModServiceUpdatedModsTitle"), _localizationService.TranslateSource("ModServiceUpdatedModsDescription", $"{outdatedMods.Count}"));
    }

    public async Task<bool> InstallMod(ModInfo mod, List<ModInfo> modList, bool suppressNotification = false, bool installDependenciesWithoutConfirm = false, bool includeAdditionalFiles = false)
    {
        if (string.IsNullOrEmpty(_configService.Config.InstallPath))
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceInstallErrorModDescription"));
            return false;
        }
        
        if (mod.Dependencies.Count > 0)
        {
            List<ModInfo> missingDependencies = [];
            foreach (string dependency in mod.Dependencies)
            {
                if (!_configService.Config.InstalledMods.ContainsKey(dependency))
                {
                    missingDependencies.Add(modList.FirstOrDefault(x => x.Name == dependency));
                }
            }
            
            if (missingDependencies.Count > 0 && (!suppressNotification || installDependenciesWithoutConfirm))
            {
                //iterate through missing dependencies and check for dependencies of dependencies until no more dependencies are found (use while)
                //if a dependency is found, add it to the missingDependencies list
                List<ModInfo> newMissingDependencies = [];
                do
                {
                    missingDependencies.AddRange(newMissingDependencies);
                    newMissingDependencies.Clear();
                    
                    foreach (ModInfo missingDependency in missingDependencies)
                    {
                        foreach (string dependency in missingDependency.Dependencies)
                        {
                            if (!_configService.Config.InstalledMods.ContainsKey(dependency) && missingDependencies.All(x => x.Name != dependency))
                            {
                                newMissingDependencies.Add(modList.FirstOrDefault(x => x.Name == dependency));
                            }
                        }
                    }
                } while (newMissingDependencies.Count > 0);
                
                string missingDependenciesString = string.Join(", ", missingDependencies.Select(x => x.Name));
                if (!installDependenciesWithoutConfirm && !suppressNotification)
                {
                    ContentDialog contentDialog = new()
                    {
                        Title = _localizationService.TranslateSource("ModServiceWarningTitle"),
                        Content =
                            _localizationService.TranslateSource("ModServiceInstallQuestionInstallDependencies",
                                mod.Name, missingDependenciesString),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes"),
                        CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo")
                    };
                    ContentDialogResult response = await contentDialog.ShowAsync();
                    // if not install dependencies, ask to abort
                    if (response != ContentDialogResult.Primary)
                    {
                        contentDialog = new()
                        {
                            Title = _localizationService.TranslateSource("ModServiceWarningTitle"),
                            Content =
                                _localizationService.TranslateSource("ToolsSelectLogsCloseButtonText",
                                    mod.Name, missingDependenciesString),
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            IsPrimaryButtonEnabled = true,
                            PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes"),
                            CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo")
                        };
                        response = await contentDialog.ShowAsync();
                        if (response == ContentDialogResult.Primary)
                            return false;
                    }
                }

                if (mod.RequiresFiles || missingDependencies.Any(x => x.RequiresFiles))
                {
                    if (!suppressNotification)
                    {
                        List<string> requiresFilesMissingDependencies =
                            missingDependencies.Where(x => x.RequiresFiles).Select(x => x.Name).ToList();
                        if (mod.RequiresFiles)
                            requiresFilesMissingDependencies.Add(mod.Name);
                        string requiresFilesMissingDependenciesString =
                            string.Join(", ", requiresFilesMissingDependencies);

                        ContentDialog contentDialog = new()
                        {
                            Title = _localizationService.TranslateSource("ModServiceWarningTitle"),
                            Content =
                                _localizationService.TranslateSource("ModServiceInstallQuestionIncludeAdditionalFiles",
                                    requiresFilesMissingDependenciesString),
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            IsPrimaryButtonEnabled = true,
                            PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes"),
                            CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo"),
                        };
                        ContentDialogResult response = await contentDialog.ShowAsync();
                        if (response == ContentDialogResult.Primary)
                        {
                            includeAdditionalFiles = true;
                        }
                    }
                }

                try
                {
                    foreach (ModInfo missingMod in missingDependencies)
                    {
                        // don't set installDependenciesWithoutConfirm to true, as we install all dependencies at once
                        bool installedSuccessfully = await InstallMod(missingMod, modList, true, false, includeAdditionalFiles);
                        if (!installedSuccessfully)
                            throw new Exception("Error installing dependency " + missingMod.Name);
                    }
                }
                catch (Exception ex)
                { 
                    //cleanup if error
                    try
                    {
                        foreach (ModInfo missingMod in missingDependencies)
                        {
                            await UninstallMod(missingMod);
                        }
                    }
                    catch
                    {
                    }
                    
                    _logger.LogError(ex, "InstallMod");
                    _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceErrorInstallModDescription", mod.Name));
                    return false;
                }
            }
        }
        else
        {
            ContentDialog contentDialog = new()
            {
                Title = _localizationService.TranslateSource("ModServiceWarningTitle"),
                Content =
                    _localizationService.TranslateSource("ModServiceInstallQuestionIncludeAdditionalFiles",
                        mod.Name),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes"),
                CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo")
            };
            ContentDialogResult response = await contentDialog.ShowAsync();
            if (response == ContentDialogResult.Primary)
            {
                includeAdditionalFiles = true;
            }
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

            string installPath = _configService.Config.InstallPath;
            string gamePluginsPath = Path.Combine(installPath, "BepInEx", "plugins");
            string gameConfigPath = Path.Combine(installPath, "BepInEx", "config");

            foreach (string pluginFile in mod.PluginFiles)
            {
                string sourcePath = Path.Combine(installPath, "SITLauncher", "Mods", "Extracted", "plugins", pluginFile);
                string targetPath = Path.Combine(gamePluginsPath, pluginFile);
                File.Copy(sourcePath, targetPath, true);
            }

            foreach (string? configFile in mod.ConfigFiles)
            {
                string sourcePath = Path.Combine(installPath, "SITLauncher", "Mods", "Extracted", "config", configFile);
                string targetPath = Path.Combine(gameConfigPath + configFile);
                File.Copy(sourcePath, targetPath, true);
            }
            
            if (includeAdditionalFiles)
                await InstallAdditionalModFiles(mod);

            ManagerConfig config = _configService.Config;
            config.InstalledMods.Add(mod.Name, mod.PortVersion);
            _configService.UpdateConfig(config);

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

    public async Task<bool> UninstallMod(ModInfo mod)
    {
        try
        {
            if (mod == null || string.IsNullOrEmpty(_configService.Config.InstallPath))
            {
                return false;
            }

            string installPath = _configService.Config.InstallPath;
            string gamePluginsPath = Path.Combine(installPath, "BepInEx", "plugins");
            string gameConfigPath = Path.Combine(installPath, "BepInEx", "config");

            foreach (string pluginFile in mod.PluginFiles)
            {
                string targetPath = Path.Combine(gamePluginsPath, pluginFile);
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                else
                {
                    ContentDialog dialog = new()
                    {
                        Title = _localizationService.TranslateSource("ModServiceErrorUninstallModTitle"),
                        Content = _localizationService.TranslateSource("ModServiceErrorUninstallModDescription", mod.Name, pluginFile),
                        CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo"),
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes")
                    };

                    ContentDialogResult result = await dialog.ShowAsync();
                    if (result != ContentDialogResult.Primary)
                    {
                        throw new FileNotFoundException(_localizationService.TranslateSource("ModServiceErrorExceptionUninstallModDescription", mod.Name, pluginFile));
                    }
                }
            }

            foreach (var configFile in mod.ConfigFiles)
            {
                string targetPath = Path.Combine(gamePluginsPath, configFile);
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                else
                {
                    ContentDialog dialog = new()
                    {
                        Title = _localizationService.TranslateSource("ModServiceErrorUninstallModTitle"),
                        Content = _localizationService.TranslateSource("ModServiceErrorExceptionUninstallModDescription", mod.Name, configFile),
                        CloseButtonText = _localizationService.TranslateSource("ModServiceButtonNo"),
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes")
                    };

                    ContentDialogResult result = await dialog.ShowAsync();

                    if (result != ContentDialogResult.Primary)
                    {
                        throw new FileNotFoundException(_localizationService.TranslateSource("ModServiceErrorExceptionFileUninstallModDescription", mod.Name, configFile));
                    }
                }
            }

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

    private string GetDownloadUrlFromHubSpt(ModInfo mod, string url)
    {
        //input url is like this: https://hub.sp-tarkov.com/files/file/1159-morecheckmarks
        //get source code of url
        string sourceCode = new WebClient().DownloadString(url);
        //find download via csquery:
        CQ dom = sourceCode;
        // get versions box by id versions
        CQ versionsBox = dom["#versions"];
        // get a list of versions by a.externalURL
        CQ versions = versionsBox["a.externalURL"];
        // get the top most version that contains the sit version
        IDomObject? version = versions.FirstOrDefault(x => x.InnerText.Contains(mod.ModVersion));
        // get the download link from the version
        string downloadUrl = version?.GetAttribute("href") ?? string.Empty;
        return downloadUrl;
    }

    private string GetDownloadUrlFromGithub(ModInfo mod, string url)
    {
        string downloadUrl = null;
        //input url is like this: https://github.com/TommySoucy/MoreCheckmarks/
        //needs to get all releases and match the version like this: https://api.github.com/repos/rails/rails/releases
        string apiUrl = url.Replace("https://github.com", "https://api.github.com/repos") + "/releases";
        //get json from api
        using (WebClient client = new WebClient())
        {
            // Set user-agent header to avoid being blocked by GitHub API for missing user-agent
            client.Headers.Add("User-Agent", "request");

            string json = client.DownloadString(apiUrl);
            //parse json
            //find the release that contains the version
            // Parse json
            dynamic releases = JsonConvert.DeserializeObject(json);

            // Find the release that contains the version
            foreach (var release in releases)
            {
                string tagName = release.tag_name;
                if (tagName != null && tagName.ToString().Contains(mod.ModVersion))
                {
                    // Get the newest release asset
                    downloadUrl = release.assets[0].browser_download_url;
                }
            }
        }
        if (downloadUrl == null)
        {
            throw new Exception("Download url not found");
        }
        return downloadUrl;
    }

    public async Task<bool> InstallAdditionalModFiles(ModInfo mod)
    {
        bool fixVersion = true;
        
        if (string.IsNullOrEmpty(_configService.Config.InstallPath))
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceInstallErrorModDescription"));
            return false;
        }

        if (string.IsNullOrEmpty(mod.OriginalDownloadUrl))
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceInstallErrorDownloadMissing"));
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

            string installPath = _configService.Config.InstallPath;
            string gamePluginsPath = Path.Combine(installPath, "BepInEx", "plugins");
            string serverPath = _configService.Config.AkiServerPath;
            string serverModPath = Path.Combine(serverPath, "user", "mods");

            string tempPath = Path.Combine(_configService.Config.InstallPath, "SITLauncher", "Mods", "AdditionalFiles");
            bool installedClient, installedServer = false;
            
            
            //if mod is from github and not a release, get the download url from the github api
            if (mod.OriginalDownloadUrl.Contains("github.com") && !mod.OriginalDownloadUrl.Contains("/releases/tags"))
            {
                mod.OriginalDownloadUrl = GetDownloadUrlFromGithub(mod, mod.OriginalDownloadUrl);
            }

            try
            {
                WebRequest request = WebRequest.Create(mod.OriginalDownloadUrl);
                WebResponse response = request.GetResponse();
                string originalFileName = response.Headers["Content-Disposition"].Trim();
                if (originalFileName != null)
                {
                    originalFileName = originalFileName.Replace("attachment; filename=", "");
                }

                Stream streamWithFileBody = response.GetResponseStream();
                string pathToSave = Path.Combine(tempPath, originalFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(pathToSave));
                using (Stream output = File.OpenWrite(pathToSave))
                {
                    streamWithFileBody.CopyTo(output);
                }

                await _filesService.ExtractArchive(pathToSave, Path.Combine(tempPath, "Extracted"));

                //find directories like Path.Combine("BepInEx", "plugins") in extracted files
                try
                {
                    string[] bepInExDirectories = Directory.GetDirectories(Path.Combine(tempPath, "Extracted"),
                        Path.Combine("BepInEx", "plugins"), SearchOption.AllDirectories);
                    string bepInExDirectory = bepInExDirectories.FirstOrDefault();

                    //foreach file in directory copy to gamePluginsPath
                    foreach (string file in Directory.GetFiles(bepInExDirectory, "*", SearchOption.AllDirectories))
                    {
                        if (mod.PluginFiles.Contains(Path.GetFileName(file)))
                        {
                            continue;
                        }

                        string relativePath = file.Replace(bepInExDirectory, "");
                        if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
                            relativePath = relativePath[1..];

                        string targetPath = Path.Combine(gamePluginsPath, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        File.Copy(file, targetPath, true);
                    }

                    installedClient = true;
                }
                catch (DirectoryNotFoundException ex)
                {
                    installedClient = false;
                }

                try
                {
                    string[] userModsDirectories = Directory.GetDirectories(Path.Combine(tempPath, "Extracted"),
                        Path.Combine("user", "mods"), SearchOption.AllDirectories);
                    string userModsDirectory = userModsDirectories.FirstOrDefault();

                    if (fixVersion)
                    {
                        //find package.json anywhere in userModsDirectory and parse it as json, change version to x and write back
                        string[] packageJsonFiles = Directory.GetFiles(userModsDirectory, "package.json",
                            SearchOption.AllDirectories);
                        string packageJsonFile = packageJsonFiles.FirstOrDefault();
                        if (packageJsonFile != null)
                        {
                            string packageJson = File.ReadAllText(packageJsonFile);
                            dynamic json = JsonConvert.DeserializeObject(packageJson);
                            string akiVersion = _configService.Config.SptAkiVersion;
                            string[] akiVersionParts = akiVersion.Split('.');
                            string version = $"~{akiVersionParts[0]}.{akiVersionParts[1]}";
                            json["version"] = version;
                            File.WriteAllText(packageJsonFile, JsonConvert.SerializeObject(json, Formatting.Indented));
                        }
                    }

                    foreach (string file in Directory.GetFiles(userModsDirectory, "*", SearchOption.AllDirectories))
                    {
                        string relativePath = file.Replace(userModsDirectory, "");
                        if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
                            relativePath = relativePath[1..];

                        string targetPath = Path.Combine(serverModPath, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        File.Copy(file, targetPath, true);
                    }
                    installedServer = true;
                } 
                catch (DirectoryNotFoundException ex)
                {
                    installedServer = false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
            
            if (!installedClient && !installedServer)
            {
                throw new Exception("Error installing additional mod files");
            }

            if (!installedClient)
            {
                _barNotificationService.ShowWarning(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceErrorInstallAdditionalModFilesWarningClient", mod.Name), 15);
            }
            if (!installedServer)
            {
                _barNotificationService.ShowWarning(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceErrorInstallAdditionalModFilesWarningServer", mod.Name), 15);
            }

            ManagerConfig config = _configService.Config;
            _configService.UpdateConfig(config);
            
            _barNotificationService.ShowSuccess(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceInstallAdditionalModFilesDescription", mod.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InstallAdditionalModFiles");
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModServiceInstallModTitle"), _localizationService.TranslateSource("ModServiceErrorInstallAdditionalModFilesDescription", mod.Name));
            return false;
        }

        return true;
    }
}
