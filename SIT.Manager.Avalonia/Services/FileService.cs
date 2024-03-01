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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services
{
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

        /// <summary>
        /// Handy function to compactly translate source code.
        /// </summary>
        /// <param name="key">key in the resources</param>
        /// <param name="parameters">the paramaters that was inside the source string. will be replaced by hierarchy where %1 .. %n is the first paramater.</param>
        private string Translate(string key, params string[] parameters) => _localizationService.TranslateSource(key, parameters);

        private static async Task OpenAtLocation(string path) {
            using (Process opener = new()) {
                if (OperatingSystem.IsWindows()) {
                    opener.StartInfo.FileName = "explorer.exe";
                    opener.StartInfo.Arguments = path;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    opener.StartInfo.FileName = "explorer";
                    opener.StartInfo.Arguments = $"-R {path}";
                }
                else {
                    opener.StartInfo.FileName = path;
                    opener.StartInfo.UseShellExecute = true;
                }
                opener.Start();
                await opener.WaitForExitAsync();
            }
        }

        private async Task<bool> DownloadMegaFile(string fileName, string fileUrl, bool showProgress) {
            _logger.LogInformation("Attempting to use Mega API.");
            try {
                MegaApiClient megaApiClient = new();
                await megaApiClient.LoginAnonymousAsync();

                // Todo: Add proper error handling below
                if (!megaApiClient.IsLoggedIn) {
                    return false;
                }

                _logger.LogInformation($"Starting download of '{fileName}' from '{fileUrl}'");

                Progress<double> progress = new((prog) => {
                    _actionNotificationService.UpdateActionNotification(new ActionNotification(Translate("FileServiceProgressDownloading", fileName), prog, showProgress));
                });

                Uri fileLink = new(fileUrl);
                INode fileNode = await megaApiClient.GetNodeFromLinkAsync(fileLink);

                string targetPath = Path.Combine(_configService.Config.InstallPath, fileName);
                await megaApiClient.DownloadFileAsync(fileNode, targetPath, progress);

                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Downloads a file and shows a progress bar if enabled
        /// </summary>
        /// <param name="fileName">The name of the file to be downloaded.</param>
        /// <param name="filePath">The path (not including the filename) to download to.</param>
        /// <param name="fileUrl">The URL to download from.</param>
        /// <param name="showProgress">If a progress bar should show the status.</param>
        /// <returns></returns>
        public async Task<bool> DownloadFile(string fileName, string filePath, string fileUrl, bool showProgress = false) {
            _actionNotificationService.StartActionNotification();

            bool result = false;
            if (fileUrl.Contains("mega.nz")) {
                result = await DownloadMegaFile(fileName, fileUrl, showProgress);
            }
            else {
                _logger.LogInformation($"Starting download of '{fileName}' from '{fileUrl}'");
                filePath = Path.Combine(filePath, fileName);
                if (File.Exists(filePath)) {
                    File.Delete(filePath);
                }

                Progress<double> progress = new((prog) => {
                    _actionNotificationService.UpdateActionNotification(new ActionNotification(Translate("FileServiceProgressDownloading", fileName), Math.Floor(prog), showProgress));
                });

                try {
                    using (FileStream file = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        await _httpClient.DownloadAsync(file, fileUrl, progress);
                    }
                    result = true;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "DownloadFile");
                }
            }

            _actionNotificationService.StopActionNotification();
            return result;
        }

        public async Task ExtractArchive(string filePath, string destination)
        {
            _actionNotificationService.StartActionNotification();

            // Ensures that the last character on the extraction path is the directory separator char.
            // Without this, a malicious zip file could try to traverse outside of the expected extraction path.
            if (!destination.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) {
                destination += Path.DirectorySeparatorChar;
            }

            DirectoryInfo destinationInfo = new(destination);
            destinationInfo.Create();

            ActionNotification actionNotification = new(string.Empty, 0, true);
            try {
                using ZipArchive archive = await Task.Run(() => ZipArchive.Open(filePath));
                int totalFiles = archive.Entries.Where(file => !file.IsDirectory).Count();
                int completed = 0;

                Progress<float> progress = new((prog) => {
                    actionNotification.ProgressPercentage = prog;
                    _actionNotificationService.UpdateActionNotification(actionNotification);
                });

                foreach (ZipArchiveEntry entry in archive.Entries) {
                    if (entry.IsDirectory) {
                        continue;
                    }
                    else {
                        entry.WriteToDirectory(destination, new ExtractionOptions() {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }

                    actionNotification.ActionText = Translate("FileServiceProgressExtracting", $"{Path.GetFileName(entry.Key)}", $"{++completed}/{totalFiles}");
                    ((IProgress<float>) progress).Report((float) completed / totalFiles * 100);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "ExtractFile: Error when opening Archive");
            }

            _actionNotificationService.StopActionNotification();
        }

        public async Task OpenDirectoryAsync(string path) {
            if (!Directory.Exists(path)) {
                // Directory doesn't exist so return early.
                return;
            }
            path = Path.GetFullPath(path);
            await OpenAtLocation(path);
        }

        public async Task OpenFileAsync(string path) {
            if (!File.Exists(path)) {
                // File doesn't exist so return early.
                return;
            }
            path = Path.GetFullPath(path);
            await OpenAtLocation(path);
        }
    }
}