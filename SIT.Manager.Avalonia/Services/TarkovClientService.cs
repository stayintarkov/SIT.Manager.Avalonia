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

        private void ClearModCache()
        {
            string cachePath = _configService.Config.InstallPath;
            if (!string.IsNullOrEmpty(cachePath) && Directory.Exists(cachePath))
            {
                // Combine the installPath with the additional subpath.
                cachePath = Path.Combine(cachePath, "BepInEx", "cache");
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                }
                Directory.CreateDirectory(cachePath);
                _barNotificationService.ShowInformational(_localizationService.TranslateSource("TarkovClientServiceCacheClearedTitle"), _localizationService.TranslateSource("TarkovClientServiceCacheClearedDescription"));
            }
            else
            {
                // Handle the case where InstallPath is not found or empty.
                _barNotificationService.ShowError(_localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorTitle"), _localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorDescription"));
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
            if (Directory.Exists(eftCachePath))
            {
                Directory.Delete(eftCachePath, true);
            }
            else
            {
                // Handle the case where the cache directory does not exist.
                _barNotificationService.ShowWarning(_localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorTitle"), _localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorEFTDescription", eftCachePath));
                return;
            }

            Directory.CreateDirectory(eftCachePath);
            _barNotificationService.ShowInformational(_localizationService.TranslateSource("TarkovClientServiceCacheClearedTitle"), _localizationService.TranslateSource("TarkovClientServiceCacheClearedEFTDescription"));
        }

        public override void Start(string? arguments)
        {
            _process = new Process()
            {
                StartInfo = new(ExecutableFilePath)
                {
                    UseShellExecute = true,
                    Arguments = arguments
                },
                EnableRaisingEvents = true,
            };
            if (OperatingSystem.IsLinux())
            {
                _process.StartInfo.FileName = _configService.Config.WineRunner;
                _process.StartInfo.Arguments = $"\"{ExecutableFilePath}\" {arguments}";
                _process.StartInfo.UseShellExecute = false;

                string winePrefix = Path.GetFullPath(_configService.Config.WinePrefix);
                if (!Path.EndsInDirectorySeparator(winePrefix))
                {
                    winePrefix = $"{winePrefix}{Path.DirectorySeparatorChar}";
                }
                _process.StartInfo.EnvironmentVariables.Add("WINEPREFIX", winePrefix);
            }
            else
            {
                _process.StartInfo.WorkingDirectory = ExecutableDirectory;
            }

            _process.Exited += new EventHandler(ExitedEvent);
            _process.Start();

            if (_configService.Config.CloseAfterLaunch)
            {
                IApplicationLifetime? lifetime = App.Current?.ApplicationLifetime;
                if (lifetime != null && lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    desktopLifetime.Shutdown();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                UpdateRunningState(RunningState.Running);
            }
        }
    }
}