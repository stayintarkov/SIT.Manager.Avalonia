using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
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

    private readonly ILogger<AppUpdaterService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IManagerConfigService _managerConfigService = managerConfigService;

    private static string ProcessName
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return "SIT.Manager.Avalonia.Desktop.exe";
            }
            else if (OperatingSystem.IsLinux())
            {
                return "SIT.Manager.Avalonia.Desktop";
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
                return @"https://github.com/stayintarkov/SIT.Manager.Avalonia/releases/latest/download/linux-x64.tar";
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

    private static void ExtractUpdatedManager(string zipPath, string destination)
    {
        DirectoryInfo releasePath = new(destination);
        releasePath.Create();

        if (OperatingSystem.IsWindows())
        {
            using (ZipArchive archive = ZipArchive.Open(zipPath))
            {
                archive.ExtractToDirectory(releasePath.FullName);
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            using (TarArchive archive = TarArchive.Open(zipPath))
            {
                archive.ExtractToDirectory(releasePath.FullName);
            }
        }
        else
        {
            throw new NotImplementedException("No manager extraction has been configured for this OS.");
        }
    }

    private static void SetLinuxExecutablePermissions(string cmd)
    {
        string escapedArgs = cmd.Replace("\"", "\\\"");
        using (Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\""
            }
        })
        {
            process.Start();
            process.WaitForExit();
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
        ExtractUpdatedManager(zipPath, releasePath);

        // Set the permissions for the executable now that we have extracted it
        // this has the added bonus of making sure that Process.dll tm is loaded 
        // before we move it elsewhere
        if (OperatingSystem.IsLinux())
        {
            string executablePath = Path.Combine(releasePath, ProcessName);
            SetLinuxExecutablePermissions($"chmod 755 {executablePath}");
        }
        else
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
