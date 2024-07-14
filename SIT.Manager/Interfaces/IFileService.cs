using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public interface IFileService
{
    Task<Stream?> DownloadMegaFileAsync(Uri source, IProgress<double>? progress = null);
    Task CopyDirectory(string source, string destination, IProgress<double>? progress = null);
    Task CopyFileAsync(string sourceFile, string destinationFile, int bufferSize = 4096, CancellationToken cancellationToken = default);
    Task<bool> DownloadFile(Uri source, string fileDestination, IProgress<double>? progress = null, CancellationToken ct = default);
    Task ExtractArchive(string filePath, string destination, IProgress<double>? progress = null, CancellationToken ct = default);
    Task OpenDirectoryAsync(string path);
    Task OpenFileAsync(string path);
    Task SetFileAsExecutable(string filePath);
}
