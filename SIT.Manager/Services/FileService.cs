using CG.Web.MegaApiClient;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.IO;
using SharpCompress.Readers;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public class FileService(IActionNotificationService actionNotificationService,
                         IManagerConfigService configService,
                         ILocalizationService localizationService,
                         HttpClient httpClient,
                         ILogger<FileService> logger) : IFileService
{
    private static async Task<long> CalculateDirectorySize(DirectoryInfo d, CancellationToken ct = default)
    {
        long size = 0;

        await Parallel.ForEachAsync(d.EnumerateFiles("*", SearchOption.AllDirectories), ct, (info, token) =>
        {
            if (ct.IsCancellationRequested) return ValueTask.FromCanceled(token);
            Interlocked.Add(ref size, info.Length);
            return ValueTask.CompletedTask;
        });
        
        if (ct.IsCancellationRequested) return -1;
        return size;
    }

    private static async Task<long> CopyDirectoryAsync(DirectoryInfo source, DirectoryInfo destination, IProgress<double>? progress = null)
    {
        IEnumerable<FileInfo> files = source.EnumerateFiles("*", SearchOption.AllDirectories);

        long currentSizeMoved = 0;
        long sizeToMove = await CalculateDirectorySize(source);
        
        //Process all files
        await Parallel.ForEachAsync(files, async (file, token) =>
        {
            string relativePath = Path.GetRelativePath(source.FullName, file.DirectoryName ?? source.FullName);
            DirectoryInfo fileParent = relativePath.Trim().Equals(".", StringComparison.InvariantCultureIgnoreCase) ?
            destination :
            destination.CreateSubdirectory(relativePath);
            
            string absoluteDestinationPath = Path.Combine(fileParent.FullName, file.Name);
            await using FileStream sourceStream = file.OpenRead();
            await using FileStream destinationStream = new(absoluteDestinationPath, FileMode.OpenOrCreate);
            if (destinationStream.Length > 0)
            {
                NonCryptographicHashAlgorithm hasher = new XxHash64();
                byte[] buffer = ArrayPool<byte>.Shared.Rent(hasher.HashLengthInBytes);
                try
                {
                    await hasher.AppendAsync(sourceStream, token);
                    hasher.GetHashAndReset(buffer);
                    Int64 sourceHash = BitConverter.ToInt64(buffer);

                    await hasher.AppendAsync(destinationStream, token);
                    hasher.GetHashAndReset(buffer);
                    Int64 destinationHash = BitConverter.ToInt64(buffer);

                    if (sourceHash == destinationHash)
                    {
                        long newCurrentSize = Interlocked.Add(ref currentSizeMoved, sourceStream.Length);
                        progress?.Report((double)newCurrentSize / sizeToMove * 100);
                        return;
                    }
                    
                    sourceStream.Seek(0, SeekOrigin.Begin);
                    //The destination file doesn't match the source so nuke whatever we have to start copying
                    destinationStream.SetLength(0);
                    await destinationStream.FlushAsync(token);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            
            long prevReport = 0;
            Progress<long> streamProgress = new(x =>
            {
                long newCurrentSize = Interlocked.Add(ref currentSizeMoved, x - Interlocked.Exchange(ref prevReport, x));
                progress?.Report((double)newCurrentSize / sizeToMove * 100);
            });
            await sourceStream.CopyToAsync(destinationStream, ushort.MaxValue, streamProgress, cancellationToken: token).ConfigureAwait(false);
        });
        
        progress?.Report((double)currentSizeMoved / sizeToMove * 100);
        return currentSizeMoved;
    }

    private static async Task OpenAtLocation(string path)
    {
        using Process opener = new();
        string fileName;
        string arguments = string.Empty;

        if (OperatingSystem.IsWindows())
        {
            fileName = "explorer.exe";
            arguments = path;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            fileName = "explorer";
            arguments = $"-R {path}";
        }
        else
        {
            fileName = path;
            opener.StartInfo.UseShellExecute = true;
        }
        opener.StartInfo.FileName = fileName;
        opener.StartInfo.Arguments = arguments;
        opener.Start();
        await opener.WaitForExitAsync();
    }

    public async Task<Stream?> DownloadMegaFileAsync(Uri source, IProgress<double>? progress = null)
    {
        Stream? ret = null;
        logger.LogInformation("Attempting to use Mega API.");
        try
        {
            MegaApiClient megaApiClient = new();
            await megaApiClient.LoginAnonymousAsync().ConfigureAwait(false);

            if (megaApiClient.IsLoggedIn)
            {
                logger.LogInformation("Starting download from '{fileUrl}'", source.AbsoluteUri);
            
                INode fileNode = await megaApiClient.GetNodeFromLinkAsync(source);
                
                ret = await megaApiClient.DownloadAsync(fileNode, progress);
            }
            else
            {
                logger.LogWarning("Failed to login user as anonymous to Mega");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file '{fileUrl}' from Mega", source.AbsoluteUri);
        }

        return ret;
    }

    public async Task CopyDirectory(string source, string destination, IProgress<double>? progress = null)
    {
        DirectoryInfo sourceDir = new(source);
        DirectoryInfo destinationDir = new(destination);
        destinationDir.Create();
        await CopyDirectoryAsync(sourceDir, destinationDir, progress).ConfigureAwait(false);
    }

    public async Task CopyFileAsync(string sourceFile, string destinationFile, int bufferSize = 4096, CancellationToken cancellationToken = default)
    {
        const FileOptions fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        await using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions);
        await using FileStream destinationStream = new(destinationFile, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, fileOptions);
        await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken).ConfigureAwait(false);
    }

    //TODO: Implement polly here
    public async Task<bool> DownloadFile(Uri source, string fileDestination, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        bool success = false;
        
        await using FileStream destFileStream = new(fileDestination, FileMode.Create, FileAccess.Write, FileShare.Read);
        
        if (source.Host.Equals("mega.nz", StringComparison.InvariantCultureIgnoreCase))
        {
            Stream? megaStream = await DownloadMegaFileAsync(source);
            if (megaStream == null)
            {
                logger.LogWarning("Mega API returned null for {sourceUri}", source.AbsoluteUri);
            }
            else
            {
                long streamLength = megaStream.Length;
                IProgress<long>? copyProgressReporter = progress == null ? null : new Progress<long>(val =>
                {
                    progress.Report((double)streamLength / val);
                });
                await megaStream.CopyToAsync(destFileStream, ushort.MaxValue, copyProgressReporter, ct);
            }
        }
        else
        {
            logger.LogInformation("Starting download of '{fileName}' from '{source}'", Path.GetFileName(fileDestination),
                source.AbsoluteUri);
            try
            {
                await httpClient.DownloadAsync(destFileStream, source.AbsoluteUri, progress, cancellationToken: ct).ConfigureAwait(false);
                success = true;
            }
            catch (Exception ex)
            {
                //TODO: Write a better log message
                logger.LogError(ex, "DownloadFile");
            }
        }

        return success;
    }

    //TODO: Implement cancellation token checks
    public async Task ExtractArchive(string filePath, string destination, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        // Ensures that the last character on the extraction path is the directory separator char.
        // Without this, a malicious zip file could try to traverse outside the expected extraction path.
        if (!destination.EndsWith(Path.DirectorySeparatorChar))
        {
            destination += Path.DirectorySeparatorChar;
        }

        DirectoryInfo destinationInfo = new(destination);
        destinationInfo.Create();

        try
        {
            await using Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous);
            using IArchive archive = await Task.Run(() => ArchiveFactory.Open(stream), ct);

            double totalSize = archive.TotalUncompressSize;
            long bytesCompleted = 0;

            //Seems SharpCompress wasn't designed to be multithread capable. Doing so throws all kinds of exceptions
            //As such this is the best we can really do
            ExtractionOptions options = new() { ExtractFullPath = true, Overwrite = true, };
            using IReader reader = archive.ExtractAllEntries();
            reader.EntryExtractionProgress += (s, e) =>
            {
                //TODO: Improve this reporting
                if (e.ReaderProgress?.PercentageReadExact < 100) return;
                long newSize = Interlocked.Add(ref bytesCompleted, e.Item.Size);
                progress?.Report(newSize / totalSize);
            };
            await Task.Run(() => reader.WriteAllToDirectory(destination, options), ct);
            
            progress?.Report(1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while extracting archive");
            throw;
        }
    }

    public async Task OpenDirectoryAsync(string path)
    {
        if (!Directory.Exists(path)) return;
        path = Path.GetFullPath(path);
        await OpenAtLocation(path);
    }

    public async Task OpenFileAsync(string path)
    {
        if (!File.Exists(path)) return;
        path = Path.GetFullPath(path);
        await OpenAtLocation(path);
    }

    public async Task SetFileAsExecutable(string filePath)
    {
        if (OperatingSystem.IsLinux())
        {
            string cmd = $"chmod 755 {filePath}";
            string escapedArgs = cmd.Replace("\"", "\\\"");
            using Process process = new();
            process.StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\""
            };
            process.Start();
            await process.WaitForExitAsync();
        }
    }
}
