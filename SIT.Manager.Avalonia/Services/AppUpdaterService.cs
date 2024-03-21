using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SIT.Manager.Avalonia.Extentions;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services;

public class AppUpdaterService(ILogger<AppUpdaterService> logger, HttpClient httpClient, IManagerConfigService managerConfigService) : IAppUpdaterService
{
    private const string MANAGER_VERSION_URL = @"https://api.github.com/repos/stayintarkov/SIT.Manager.Avalonia/releases/latest";
    private const string SITMANAGER_PROC_NAME = "SIT.Manager.Avalonia.Desktop.exe";
    private const string SITMANAGER_RELEASE_URL = @"https://github.com/stayintarkov/SIT.Manager.Avalonia/releases/latest/download/win-x64.zip";

    private readonly ILogger<AppUpdaterService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IManagerConfigService _managerConfigService = managerConfigService;

    private async Task MoveManager(DirectoryInfo source, string destination)
    {
        await MoveManager(source, new DirectoryInfo(destination));
    }

    private async Task MoveManager(DirectoryInfo source, DirectoryInfo destination)
    {
        IEnumerable<DirectoryInfo> directories = source.EnumerateDirectories();
        IEnumerable<FileInfo> files = source.EnumerateFiles();
        destination.Create();

        foreach (DirectoryInfo directory in directories)
        {
            if (directory.Name.Equals("backup", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            DirectoryInfo newDestination = destination.CreateSubdirectory(directory.Name);
            await MoveManager(directory, newDestination);
        }

        foreach (FileInfo file in files)
        {
            try
            {
                file.MoveTo(Path.Combine(destination.FullName, file.Name));
            }
            catch (Exception) { }
        }
    }

    public async Task<bool> CheckForUpdate()
    {
        if (_managerConfigService.Config.LookForUpdates)
        {
            try
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0");

                string versionJsonString = await _httpClient.GetStringAsync(MANAGER_VERSION_URL);
                GithubRelease? latestRelease = JsonSerializer.Deserialize<GithubRelease>(versionJsonString);
                if (latestRelease != null)
                {
                    Version latestVersion = new(latestRelease.name);
                    return latestVersion.CompareTo(currentVersion) > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckForUpdate");
            }
        }
        return false;
    }

    public async Task<bool> Update(IProgress<double> progress)
    {
        string workingDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!File.Exists(Path.Combine(workingDir, SITMANAGER_PROC_NAME)))
        {
            _logger.LogError("Unable to find '{0}' in root directory. Make sure the app is installed correctly.", SITMANAGER_PROC_NAME);
            return false;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);
        string zipName = Path.GetFileName(SITMANAGER_RELEASE_URL);
        string zipPath = Path.Combine(tempPath, zipName);

        try
        {
            _logger.LogInformation("Downloading '{0}' to '{1}'", zipName, zipPath);
            using (FileStream fs = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _httpClient.DownloadAsync(fs, SITMANAGER_RELEASE_URL, progress);
            }
            progress.Report(1);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during download: {0}", ex.Message);
            return false;
        }

        _logger.LogInformation("Download complete; Extracting new version..");
        DirectoryInfo releasePath = new(Path.Combine(tempPath, "Release"));
        releasePath.Create();
        using (ZipArchive archive = ZipArchive.Open(zipPath))
        {
            archive.ExtractToDirectory(releasePath.FullName);
        }

        _logger.LogInformation("Extraction complete; Creating backup of SIT.Manager..");
        string backupPath = Path.Combine(workingDir, "Backup");
        if (Directory.Exists(backupPath))
        {
            Directory.Delete(backupPath, true);
        }

        DirectoryInfo workingFolderInfo = new(workingDir);
        await MoveManager(workingFolderInfo, backupPath);
        FileInfo configFile = new(Path.Combine(backupPath, "ManagerConfig.json"));
        if (configFile.Exists)
        {
            configFile.CopyTo(Path.Combine(workingFolderInfo.FullName, configFile.Name));
        }

        _logger.LogInformation("Backup complete; Moving extracted release to SIT Manager working dir");
        await MoveManager(releasePath, workingDir);
        Directory.Delete(tempPath, true);

        _logger.LogInformation($"\nUpdate done. Backup can be found in the {Path.GetFileName(backupPath)} folder. User settings have been saved.");
        return true;
    }

    public void RestartApp()
    {
        // Start new instance of application
        string executablePath = Path.Combine(AppContext.BaseDirectory, SITMANAGER_PROC_NAME);
        Process.Start(executablePath);

        // Shutdown the current application
        IApplicationLifetime? lifetime = Application.Current?.ApplicationLifetime;
        if (lifetime != null && lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }
}
