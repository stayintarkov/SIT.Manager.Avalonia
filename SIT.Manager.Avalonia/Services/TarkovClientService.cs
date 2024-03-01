using Avalonia.Controls.ApplicationLifetimes;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using System;
using System.Diagnostics;
using System.IO;

namespace SIT.Manager.Avalonia.Services
{
    public class TarkovClientService(IBarNotificationService barNotificationService,
                                     ILocalizationService localizationService,
                                     IManagerConfigService configService) : ManagedProcess.ManagedProcess(barNotificationService, configService), ITarkovClientService
    {
        private const string TARKOV_EXE = "EscapeFromTarkov.exe";
        public override string ExecutableDirectory => !string.IsNullOrEmpty(_configService.Config.InstallPath) ? _configService.Config.InstallPath : string.Empty;

        protected override string EXECUTABLE_NAME => TARKOV_EXE;
        private readonly ILocalizationService _localizationService = localizationService;

        /// <summary>
        /// Handy function to compactly translate source code.
        /// </summary>
        /// <param name="key">key in the resources</param>
        /// <param name="parameters">the paramaters that was inside the source string. will be replaced by hierarchy where %1 .. %n is the first paramater.</param>
        private string Translate(string key, params string[] parameters) => _localizationService.TranslateSource(key, parameters);

        private void ClearModCache()
        {
            string cachePath = _configService.Config.InstallPath;
            if (!string.IsNullOrEmpty(cachePath) && Directory.Exists(cachePath)) {
                // Combine the installPath with the additional subpath.
                cachePath = Path.Combine(cachePath, "BepInEx", "cache");
                if (Directory.Exists(cachePath)) {
                    Directory.Delete(cachePath, true);
                }
                Directory.CreateDirectory(cachePath);
                _barNotificationService.ShowInformational(Translate("TarkovClientServiceCacheClearedTitle"), Translate("TarkovClientServiceCacheClearedDescription"));
            }
            else {
                // Handle the case where InstallPath is not found or empty.
                _barNotificationService.ShowError(Translate("TarkovClientServiceCacheClearedErrorTitle"), Translate("TarkovClientServiceCacheClearedErrorDescription"));
            }
        }

        public override void ClearCache()
        {
            ClearLocalCache();
            ClearModCache();
        }

        public void ClearLocalCache()
        {
            string eftCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "Battlestate Games", "EscapeFromTarkov");

            // Check if the directory exists.
            if (Directory.Exists(eftCachePath)) {
                Directory.Delete(eftCachePath, true);
            }
            else {
                // Handle the case where the cache directory does not exist.
                _barNotificationService.ShowWarning(Translate("TarkovClientServiceCacheClearedErrorTitle"), Translate("TarkovClientServiceCacheClearedErrorEFTDescription", eftCachePath));
                return;
            }

            Directory.CreateDirectory(eftCachePath);
            _barNotificationService.ShowInformational(Translate("TarkovClientServiceCacheClearedTitle"), Translate("TarkovClientServiceCacheClearedEFTDescription"));
        }

        public override void Start(string? arguments)
        {
            _process = new Process() {
                StartInfo = new(ExecutableFilePath) {
                    UseShellExecute = true,
                    Arguments = arguments
                },
                EnableRaisingEvents = true,
            };
            if (OperatingSystem.IsLinux()) {
                _process.StartInfo.FileName = _configService.Config.WineRunner;
                _process.StartInfo.Arguments = $"\"{ExecutableFilePath}\" {arguments}";
                _process.StartInfo.UseShellExecute = false;

                string winePrefix = Path.GetFullPath(_configService.Config.WinePrefix);
                if (!Path.EndsInDirectorySeparator(winePrefix)) {
                    winePrefix = $"{winePrefix}{Path.DirectorySeparatorChar}";
                }
                _process.StartInfo.EnvironmentVariables.Add("WINEPREFIX", winePrefix);
            }
            else {
                _process.StartInfo.WorkingDirectory = ExecutableDirectory;
            }

            _process.Exited += new EventHandler(ExitedEvent);
            _process.Start();

            if (_configService.Config.CloseAfterLaunch) {
                IApplicationLifetime? lifetime = App.Current?.ApplicationLifetime;
                if (lifetime != null && lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime) {
                    desktopLifetime.Shutdown();
                }
                else {
                    Environment.Exit(0);
                }
            }
            else {
                UpdateRunningState(RunningState.Running);
            }
        }
    }
}