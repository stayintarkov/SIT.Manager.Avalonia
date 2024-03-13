using CG.Web.MegaApiClient;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SIT.Manager.Avalonia.Extentions;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services;

public class FileService(IActionNotificationService actionNotificationService,
                         IManagerConfigService configService,
                         ILocalizationService localizationService,
                         HttpClient httpClient,
                         ILogger<FileService> logger) : IFileService
{
    private readonly IActionNotificationService _actionNotificationService = actionNotificationService;
    private readonly IManagerConfigService _configService = configService;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<FileService> _logger = logger;
    private readonly ILocalizationService _localizationService = localizationService;

    private static long CalculateDirectorySize(DirectoryInfo d)
    {
        long size = 0;

        IEnumerable<DirectoryInfo> directories = d.EnumerateDirectories();
        IEnumerable<FileInfo> files = d.EnumerateFiles();

        // Add subdirectory sizes.
        foreach (DirectoryInfo dir in directories)
        {
            size += CalculateDirectorySize(dir);
        }

        // Add file sizes.
        foreach (FileInfo f in files)
        {
            size += f.Length;
        }

        return size;
    }

    private static async Task<double> CopyDirectoryAsync(DirectoryInfo source, DirectoryInfo destination, double currentProgress, double totalSize, IProgress<double>? progress = null)
    {
        IEnumerable<DirectoryInfo> directories = source.EnumerateDirectories();
        IEnumerable<FileInfo> files = source.EnumerateFiles();

        foreach (DirectoryInfo directory in directories)
        {
            DirectoryInfo newDestination = destination.CreateSubdirectory(directory.Name);
            currentProgress = await CopyDirectoryAsync(directory, newDestination, currentProgress, totalSize, progress);
        }

        foreach (FileInfo file in files)
        {
            using (FileStream sourceStream = file.OpenRead())
            {
                using (FileStream destinationStream = File.Create(Path.Combine(destination.FullName, file.Name)))
                {
                    Progress<long> streamProgress = new(x =>
                    {
                        double progressPercentage = (currentProgress + x) / totalSize * 100;
                        progress?.Report(progressPercentage);
                    });
                    await sourceStream.CopyToAsync(destinationStream, ushort.MaxValue, streamProgress);
                    currentProgress += file.Length;
                }
            }
        }

        return currentProgress;
    }

    private static async Task OpenAtLocation(string path)
    {
        using (Process opener = new())
        {
            if (OperatingSystem.IsWindows())
            {
                opener.StartInfo.FileName = "explorer.exe";
                opener.StartInfo.Arguments = path;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                opener.StartInfo.FileName = "explorer";
                opener.StartInfo.Arguments = $"-R {path}";
            }
            else
            {
                opener.StartInfo.FileName = path;
                opener.StartInfo.UseShellExecute = true;
            }
            opener.Start();
            await opener.WaitForExitAsync();
        }
    }

    private async Task<bool> DownloadMegaFile(string fileName, string fileUrl, IProgress<double> progress)
    {
        _logger.LogInformation("Attempting to use Mega API.");
        try
        {
            MegaApiClient megaApiClient = new();
            await megaApiClient.LoginAnonymousAsync();

            // TODO: Add proper error handling below
            if (!megaApiClient.IsLoggedIn)
            {
                return false;
            }

            _logger.LogInformation($"Starting download of '{fileName}' from '{fileUrl}'");

            Uri fileLink = new(fileUrl);
            INode fileNode = await megaApiClient.GetNodeFromLinkAsync(fileLink);

            string targetPath = Path.Combine(_configService.Config.InstallPath, fileName);
            await megaApiClient.DownloadFileAsync(fileNode, targetPath, progress);

            return true;
        }
        catch
        {
            return false;
        }
    }

    // TODO unify this and the other DownloadMegaFile function nicely
    private async Task<bool> DownloadMegaFile(string fileName, string fileUrl, bool showProgress)
    {
        _logger.LogInformation("Attempting to use Mega API.");
        try
        {
            MegaApiClient megaApiClient = new();
            await megaApiClient.LoginAnonymousAsync();

            // TODO: Add proper error handling below
            if (!megaApiClient.IsLoggedIn)
            {
                return false;
            }

            _logger.LogInformation($"Starting download of '{fileName}' from '{fileUrl}'");

            Progress<double> progress = new((prog) =>
            {
                _actionNotificationService.UpdateActionNotification(new ActionNotification(_localizationService.TranslateSource("FileServiceProgressDownloading", fileName), prog, showProgress));
            });

            Uri fileLink = new(fileUrl);
            INode fileNode = await megaApiClient.GetNodeFromLinkAsync(fileLink);

            string targetPath = Path.Combine(_configService.Config.InstallPath, fileName);
            await megaApiClient.DownloadFileAsync(fileNode, targetPath, progress);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task CopyDirectory(string source, string destination, IProgress<double>? progress = null)
    {
        DirectoryInfo sourceDir = new(source);
        double totalSize = CalculateDirectorySize(sourceDir);

        DirectoryInfo destinationDir = new(destination);
        destinationDir.Create();

        double currentprogress = 0;

        await CopyDirectoryAsync(sourceDir, destinationDir, currentprogress, totalSize, progress);
    }

    // TODO unify this and the other DownloadFile function nicely
    public async Task<bool> DownloadFile(string fileName, string filePath, string fileUrl, IProgress<double> progress)
    {
        bool result = false;
        if (fileUrl.Contains("mega.nz"))
        {
            result = await DownloadMegaFile(fileName, fileUrl, progress);
        }
        else
        {
            _logger.LogInformation($"Starting download of '{fileName}' from '{fileUrl}'");
            filePath = Path.Combine(filePath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            try
            {
                using (FileStream file = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await _httpClient.DownloadAsync(file, fileUrl, progress);
                }
                result = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadFile");
            }
        }
        return result;
    }

    public async Task<bool> DownloadFile(string fileName, string filePath, string fileUrl, bool showProgress = false)
    {
        _actionNotificationService.StartActionNotification();

        bool result = false;
        if (fileUrl.Contains("mega.nz"))
        {
            result = await DownloadMegaFile(fileName, fileUrl, showProgress);
        }
        else
        {
            _logger.LogInformation($"Starting download of '{fileName}' from '{fileUrl}'");
            filePath = Path.Combine(filePath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            Progress<double> progress = new((prog) =>
            {
                _actionNotificationService.UpdateActionNotification(new ActionNotification(_localizationService.TranslateSource("FileServiceProgressDownloading", fileName), Math.Floor(prog), showProgress));
            });

            try
            {
                using (FileStream file = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await _httpClient.DownloadAsync(file, fileUrl, progress);
                }
                result = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadFile");
            }
        }

        _actionNotificationService.StopActionNotification();
        return result;
    }

    // TODO unify this with the other ExtractArchive function
    public async Task ExtractArchive(string filePath, string destination, IProgress<double> progress)
    {
        // Ensures that the last character on the extraction path is the directory separator char.
        // Without this, a malicious zip file could try to traverse outside of the expected extraction path.
        if (!destination.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            destination += Path.DirectorySeparatorChar;
        }

        DirectoryInfo destinationInfo = new(destination);
        destinationInfo.Create();

        try
        {
            using ZipArchive archive = await Task.Run(() => ZipArchive.Open(filePath));
            double totalFiles = archive.Entries.Where(file => !file.IsDirectory).Count();
            double completed = 0;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }
                else
                {
                    await Task.Run(() => entry.WriteToDirectory(destination, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    }));
                }

                double progressPercentage = ++completed / totalFiles * 100;
                progress.Report(progressPercentage);
            }

            progress.Report(100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExtractFile: Error when opening Archive");
            throw;
        }
    }

    public async Task ExtractArchive(string filePath, string destination)
    {
        _actionNotificationService.StartActionNotification();

        // Ensures that the last character on the extraction path is the directory separator char.
        // Without this, a malicious zip file could try to traverse outside of the expected extraction path.
        if (!destination.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            destination += Path.DirectorySeparatorChar;
        }

        DirectoryInfo destinationInfo = new(destination);
        destinationInfo.Create();

        ActionNotification actionNotification = new(string.Empty, 0, true);
        try
        {
            using ZipArchive archive = await Task.Run(() => ZipArchive.Open(filePath));
            int totalFiles = archive.Entries.Where(file => !file.IsDirectory).Count();
            int completed = 0;

            Progress<float> progress = new((prog) =>
            {
                actionNotification.ProgressPercentage = prog;
                _actionNotificationService.UpdateActionNotification(actionNotification);
            });

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }
                else
                {
                    entry.WriteToDirectory(destination, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }

                actionNotification.ActionText = _localizationService.TranslateSource("FileServiceProgressExtracting", $"{Path.GetFileName(entry.Key)}", $"{++completed}/{totalFiles}");
                ((IProgress<float>) progress).Report((float) completed / totalFiles * 100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExtractFile: Error when opening Archive");
        }

        _actionNotificationService.StopActionNotification();
    }

    public async Task OpenDirectoryAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            // Directory doesn't exist so return early.
            return;
        }
        path = Path.GetFullPath(path);
        await OpenAtLocation(path);
    }

    public async Task OpenFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            // File doesn't exist so return early.
            return;
        }
        path = Path.GetFullPath(path);
        await OpenAtLocation(path);
    }
}
