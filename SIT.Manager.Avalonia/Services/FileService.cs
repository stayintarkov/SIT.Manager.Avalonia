using CG.Web.MegaApiClient;
using Microsoft.Extensions.Logging;
using SharpCompress.Common;
using SharpCompress.Readers;
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

    private static async Task<long> CalculateDirectorySize(DirectoryInfo d)
    {
        long size = 0;

        // Add subdirectory sizes.
        IEnumerable<DirectoryInfo> directories = d.EnumerateDirectories();
        foreach (DirectoryInfo dir in directories)
        {
            size += await CalculateDirectorySize(dir).ConfigureAwait(false);
        }

        // Add file sizes.
        size += d.EnumerateFiles().Sum(x => x.Length);

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
        double totalSize = await CalculateDirectorySize(sourceDir).ConfigureAwait(false);

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

    public async Task ExtractArchive(string filePath, string destination, IProgress<double>? progress = null)
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
            using (Stream stream = await Task.Run(() => File.OpenRead(filePath)))
            {
                double totalBytes = stream.Length;
                double bytesCompleted = 0;
                using (IReader reader = await Task.Run(() => ReaderFactory.Open(stream)))
                {
                    reader.EntryExtractionProgress += (s, e) =>
                    {
                        if (e.ReaderProgress?.PercentageReadExact == 100)
                        {
                            bytesCompleted += e.Item.CompressedSize;
                            progress?.Report(bytesCompleted / totalBytes * 100);
                        }
                    };

                    await Task.Run(() => reader.WriteAllToDirectory(destination, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true,
                    }));
                }
                progress?.Report(100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when extracting archive");
            throw;
        }
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

    public async Task SetFileAsExecutable(string filePath)
    {
        if (OperatingSystem.IsLinux())
        {
            string cmd = $"chmod 755 {filePath}";
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
                await process.WaitForExitAsync();
            }
        }
    }
}
