using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Config;
using SIT.Manager.Models.Github;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public class AppUpdaterService(IFileService fileService, ILogger<AppUpdaterService> logger, HttpClient httpClient, IManagerConfigService configService) : IAppUpdaterService
{
    private const string MANAGER_RELEASE_URL = @"https://api.github.com/repos/stayintarkov/SIT.Manager.Avalonia/releases/";
    private const string MANAGER_EXECUTABLE_NAME = @"SIT.Manager";
    private const string MANAGER_WINDOWS_RELEASE_FILE = @"win-x64.zip";
    private const string MANAGER_LINUX_RELEASE_FILE = @"linux-x64.tar.gz";
    private SITConfig _sitConfig => configService.Config.SITSettings;

    private static string ProcessName
    {
        get
        {
            if (OperatingSystem.IsWindows()) return $"{MANAGER_EXECUTABLE_NAME}.exe";
            if (OperatingSystem.IsLinux()) return MANAGER_EXECUTABLE_NAME;
            throw new NotImplementedException("No process name found for this platform");
        }
    }

    private static string ReleaseUrl
    {
        get
        {
            string? releaseFile = null;
            if (OperatingSystem.IsWindows()) releaseFile = MANAGER_WINDOWS_RELEASE_FILE;
            if (OperatingSystem.IsLinux()) releaseFile = MANAGER_LINUX_RELEASE_FILE;
            if(string.IsNullOrEmpty(releaseFile))
                throw new NotImplementedException("No Release URL found for this platform");
            return Path.Combine(MANAGER_RELEASE_URL, releaseFile);
        }
    }

    private async Task MoveManager(DirectoryInfo source, DirectoryInfo destination, CancellationToken ct = default)
    {
        IEnumerable<DirectoryInfo> directories = source.EnumerateDirectories();
        IEnumerable<FileInfo> files = source.EnumerateFiles();
        destination.Create();

        await Parallel.ForEachAsync(directories, ct, async (directory, token) =>
        {
            if (ct.IsCancellationRequested) return;
            if (directory.Name.Equals("backup", StringComparison.InvariantCultureIgnoreCase)) return;
            DirectoryInfo newDestination = destination.CreateSubdirectory(directory.Name);
            await MoveManager(directory, newDestination, token);
        });

        await Parallel.ForEachAsync(files, ct, (file, token) =>
        {
            try
            {
                if (token.IsCancellationRequested) return ValueTask.FromCanceled(token);
                file.MoveTo(Path.Combine(destination.FullName, file.Name));
                return ValueTask.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception moving file {fileName}", file.FullName);
                return ValueTask.FromException(ex);
            }
        });
    }

    private async Task ExtractUpdatedManager(string zipPath, string destination)
    {
        DirectoryInfo releasePath = new(destination);
        releasePath.Create();
        await fileService.ExtractArchive(zipPath, releasePath.FullName);
    }

    public async Task<bool> CheckForUpdate()
    {
        //TODO: Handle this being null instead of silently failing
        Version currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("0");
        Version latestVersion = new();

        TimeSpan timeSinceLastCheck = DateTime.Now - configService.Config.LauncherSettings.LastManagerUpdateCheckTime;

        if (!configService.Config.LauncherSettings.LookForUpdates || !(timeSinceLastCheck.TotalHours >= 1))
            return latestVersion > currentVersion;

        try
        {
            //TODO: use polly pipelines
            string latestReleaseUrl = Path.Combine(MANAGER_RELEASE_URL, "latest");
            string versionJsonString = await httpClient.GetStringAsync(latestReleaseUrl);
            GithubRelease? latestRelease = JsonSerializer.Deserialize<GithubRelease>(versionJsonString);
            if (latestRelease != null) latestVersion = new Version(latestRelease.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CheckForUpdate");
        }

        _sitConfig.LastSitUpdateCheckTime = DateTime.Now;

        return latestVersion > currentVersion;
    }

    public async Task<bool> Update(IProgress<double> progress)
    {
        string workingDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!File.Exists(Path.Combine(workingDir, ProcessName)))
        {
            logger.LogError("Unable to find '{ProcessName}' in root directory. Make sure the app is installed correctly.", ProcessName);
            return false;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);
        string zipName = Path.GetFileName(ReleaseUrl);
        string zipPath = Path.Combine(tempPath, zipName);

        try
        {
            logger.LogInformation("Downloading '{ZipName}' to '{ZipPath}'", zipName, zipPath);
            await using (FileStream fs = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                //TODO: Use polly here
                await httpClient.DownloadAsync(fs, ReleaseUrl, progress);
            }
            progress.Report(1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during download");
            return false;
        }

        logger.LogInformation("Download complete; Extracting new version..");
        string releasePath = Path.Combine(tempPath, "Release");
        await ExtractUpdatedManager(zipPath, releasePath);

        // Set the permissions for the executable now that we have extracted it
        // this has the added bonus of making sure that Process.dll tm is loaded 
        // before we move it elsewhere allowing us to restart the app
        string executablePath = Path.Combine(releasePath, ProcessName);
        await fileService.SetFileAsExecutable(executablePath);
        if (OperatingSystem.IsWindows())
        {
            //TODO: Explain this hack better as it's extremely obscure but VERY NECESSARY
            // HACK this literally is just so that the Process class and related dlls
            // get loaded on Windows :)
            _ = Process.GetCurrentProcess();
        }

        logger.LogInformation("Extraction complete; Creating backup of SIT.Manager..");
        string backupPath = Path.Combine(workingDir, "Backup");
        if (Directory.Exists(backupPath))
            Directory.Delete(backupPath, true);

        DirectoryInfo workingFolderInfo = new(workingDir);
        await MoveManager(workingFolderInfo, new DirectoryInfo(backupPath));
        FileInfo configFile = new(Path.Combine(backupPath, "ManagerConfig.json"));
        if (configFile.Exists)
            configFile.CopyTo(Path.Combine(workingFolderInfo.FullName, configFile.Name));

        logger.LogInformation("Backup complete; Moving extracted release to SIT Manager working dir");
        await MoveManager(new DirectoryInfo(releasePath), new DirectoryInfo(workingDir));
        Directory.Delete(tempPath, true);

        string backupPathFileName = Path.GetFileName(backupPath);
        logger.LogInformation("Update done. Backup can be found in the {BackupPathFileName} folder. User settings have been saved.", backupPathFileName);
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
        if (lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            desktopLifetime.Shutdown();
        else
            Environment.Exit(0);
    }
}
