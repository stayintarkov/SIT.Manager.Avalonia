using SIT.Manager.Interfaces;
using SIT.Manager.Models;
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

public class ModService(ICachingService cachingService,
                        IFileService filesService,
                        IManagerConfigService configService,
                        IVersionService versionService,
                        HttpClient httpClient) : IModService
{
    private const string BEPINEX_CONFIGURATION_MANAGER_RELEASE_URL = "https://api.github.com/repos/BepInEx/BepInEx.ConfigurationManager/releases/latest";
    private const string CONFIGURATION_MANAGER_ZIP_CACHE_KEY = "configuration-manager-dll";

    private readonly ICachingService _cachingService = cachingService;
    private readonly IManagerConfigService _configService = configService;
    private readonly IFileService _filesService = filesService;
    private readonly IVersionService _versionService = versionService;
    private readonly HttpClient _httpClient = httpClient;

    private static readonly List<string> _modCompatDlls = [
        "aki-core",
        "aki-custom",
        "aki-singleplayer",
        "aki_PrePatch"
    ];

    private static string GetPatchersDirectoryPath(string baseDirectory)
    {
        return Path.Combine(baseDirectory, "BepInEx", "patchers");
    }

    private static string GetPluginsDirectoryPath(string baseDirectory)
    {
        return Path.Combine(baseDirectory, "BepInEx", "plugins");
    }

    /// <summary>
    /// Downloads the latest ConfigurationManager from the BepInEx GitHub
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UriFormatException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="IOException"></exception>
    private async Task<byte[]> DownloadConfigurationManager()
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
    }

    public bool CheckModCompatibilityLayerInstalled()
    {
        bool modCompatInstalled = true;

        // Check if the plugins and patchers dlls are installed
        List<ModInfo> modList = GetInstalledMods();
        foreach (string mod in _modCompatDlls)
        {
            if (modList.Any(x => x.Name == mod))
            {
                continue;
            }
            else
            {
                modCompatInstalled = false;
                break;
            }
        }

        // Check if Aki.Common.dll and Aki.Reflection.dll are installed
        string basePath = Path.Combine(_configService.Config.SitEftInstallPath, "EscapeFromTarkov_Data", "Managed");
        string akiCommonPath = Path.Combine(basePath, "Aki.Common.dll");
        string akiReflectionPath = Path.Combine(basePath, "Aki.Reflection.dll");
        if (!File.Exists(akiCommonPath) || !File.Exists(akiReflectionPath))
        {
            modCompatInstalled = false;
        }

        return modCompatInstalled;
    }

    public List<ModInfo> GetInstalledMods()
    {
        List<ModInfo> mods = [];

        string eftPath = _configService.Config.SitEftInstallPath;
        mods.Add(new ModInfo()
        {
            IsRequired = true,
            ModVersion = _configService.Config.SitTarkovVersion,
            Name = "Escape From Tarkov"
        });

        List<FileInfo> rawMods = [];

        DirectoryInfo pluginsDir = new(GetPluginsDirectoryPath(eftPath));
        rawMods.AddRange(pluginsDir.GetFiles("*.dll", new EnumerationOptions() { RecurseSubdirectories = true }));

        DirectoryInfo patchersDir = new(GetPatchersDirectoryPath(eftPath));
        rawMods.AddRange(patchersDir.GetFiles("*.dll", new EnumerationOptions() { RecurseSubdirectories = true }));

        foreach (FileInfo fileInfo in rawMods)
        {
            string filename = fileInfo.Name.Replace(".dll", string.Empty);

            ModInfo mod = new()
            {
                Name = filename,
                ModVersion = _versionService.GetFileProductVersionString(fileInfo.FullName),
            };

            if (filename.Contains("StayInTarkov") || _modCompatDlls.Contains(filename))
            {
                mod.IsRequired = true;
            }

            mods.Add(mod);
        }

        return [.. mods.OrderBy(x => !x.IsRequired)];
    }

    public async Task<bool> InstallConfigurationManager(string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            throw new ArgumentException("Parameter can't be null or empty", nameof(targetPath));
        }

        CacheValue<byte[]> configurationManagerDll = await _cachingService.OnDisk.GetOrComputeAsync(CONFIGURATION_MANAGER_ZIP_CACHE_KEY, (key) => DownloadConfigurationManager(), TimeSpan.FromDays(1));

        byte[] configurationManagerBytes = configurationManagerDll.Value ?? [];
        if (configurationManagerBytes.Length == 0)
        {
            throw new FileNotFoundException("Failed find and install Configuration Manager");
        }

        string targetInstallLocation = Path.Combine(GetPluginsDirectoryPath(targetPath), "ConfigurationManager.dll");
        await File.WriteAllBytesAsync(targetInstallLocation, configurationManagerBytes).ConfigureAwait(false);
        return true;
    }
}
