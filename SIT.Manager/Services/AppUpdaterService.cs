using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models.Github;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public class AppUpdaterService(IFileService fileService, ILogger<AppUpdaterService> logger, HttpClient httpClient, IManagerConfigService managerConfigService) : IAppUpdaterService
{
    private const string MANAGER_VERSION_URL = @"https://api.github.com/repos/stayintarkov/SIT.Manager.Avalonia/releases/latest";

    private readonly IFileService _fileService = fileService;
    private readonly ILogger<AppUpdaterService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IManagerConfigService _managerConfigService = managerConfigService;

    private static string ProcessName
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return "SIT.Manager.exe";
            }
            else if (OperatingSystem.IsLinux())
            {
                return "SIT.Manager";
            }
            throw new NotImplementedException("No process name found for this platform");
        }
    }

    private static string ReleaseUrl
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return @"https://github.com/stayintarkov/SIT.Manager.Avalonia/releases/latest/download/win-x64.zip";
            }
            else if (OperatingSystem.IsLinux())
            {
                return @"https://github.com/stayintarkov/SIT.Manager.Avalonia/releases/latest/download/linux-x64.tar.gz";
            }
            throw new NotImplementedException("No Release URL found for this platform");
        }
    }

    private static async Task MoveManager(DirectoryInfo source, string destination)
    {
        await MoveManager(source, new DirectoryInfo(destination));
    }

    private static async Task MoveManager(DirectoryInfo source, DirectoryInfo destination)
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

    private async Task ExtractUpdatedManager(string zipPath, string destination)
    {
        DirectoryInfo releasePath = new(destination);
        releasePath.Create();
        await _fileService.ExtractArchive(zipPath, releasePath.FullName);
    }

    public async Task<bool> CheckForUpdate()
    {
        if (_managerConfigService.Config.LookForUpdates)
        {
            try
            {
                Version currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("0");

                string versionJsonString = await _httpClient.GetStringAsync(MANAGER_VERSION_URL);
                GithubRelease? latestRelease = JsonSerializer.Deserialize<GithubRelease>(versionJsonString);
                if (latestRelease != null)
                {
                    Version latestVersion = new(latestRelease.Name);
                    return latestVersion > currentVersion;
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
        if (!File.Exists(Path.Combine(workingDir, ProcessName)))
        {
            _logger.LogError("Unable to find '{ProcessName}' in root directory. Make sure the app is installed correctly.", ProcessName);
            return false;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);
        string zipName = Path.GetFileName(ReleaseUrl);
        string zipPath = Path.Combine(tempPath, zipName);

        try
        {
            _logger.LogInformation("Downloading '{ZipName}' to '{ZipPath}'", zipName, zipPath);
            using (FileStream fs = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _httpClient.DownloadAsync(fs, ReleaseUrl, progress);
            }
            progress.Report(1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during download");
            return false;
        }

        _logger.LogInformation("Download complete; Extracting new version..");
        string releasePath = Path.Combine(tempPath, "Release");
        await ExtractUpdatedManager(zipPath, releasePath);

        // Set the permissions for the executable now that we have extracted it
        // this has the added bonus of making sure that Process.dll tm is loaded 
        // before we move it elsewhere allowing us to restart the app
        string executablePath = Path.Combine(releasePath, ProcessName);
        await _fileService.SetFileAsExecutable(executablePath);
        if (OperatingSystem.IsWindows())
        {
            // HACK this literally is just so that the Process class and related dlls
            // get loaded on Windows :)
            _ = Process.GetCurrentProcess();
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
        await MoveManager(new(releasePath), workingDir);
        Directory.Delete(tempPath, true);

        string backupPathFileName = Path.GetFileName(backupPath);
        _logger.LogInformation("Update done. Backup can be found in the {BackupPathFileName} folder. User settings have been saved.", backupPathFileName);
        return true;
    }

    public void RestartApp()
    {
        // Start new instance of application
        string executablePath = Path.Combine(AppContext.BaseDirectory, ProcessName);
        if (Path.Exists(executablePath))
        {
            Process.Start(executablePath);
        }

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
