using Avalonia.Controls.ApplicationLifetimes;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using SIT.Manager.Native.Linux;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SIT.Manager.Services;

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
            StartInfo = new ProcessStartInfo(ExecutableFilePath)
            {
                UseShellExecute = true,
                Arguments = arguments
            },
            EnableRaisingEvents = true,
        };
        if (OperatingSystem.IsLinux())
        {
            LinuxConfig linuxConfig = _configService.Config.LinuxConfig;

            // Check if either mangohud or gamemode is enabled.
            StringBuilder argumentsBuilder = new();
            if (linuxConfig.IsGameModeEnabled)
            {
                _process.StartInfo.FileName = "gamemoderun";
                if (linuxConfig.IsMangoHudEnabled)
                {
                    argumentsBuilder.Append("mangohud ");
                }
                argumentsBuilder.Append($"\"{linuxConfig.WineRunner}\" ");
            }
            else if (linuxConfig.IsMangoHudEnabled) // only mangohud is enabled
            {
                _process.StartInfo.FileName = "mangohud";
                argumentsBuilder.Append($"\"{linuxConfig.WineRunner}\" ");
            }
            else
            {
                _process.StartInfo.FileName = linuxConfig.WineRunner;
            }
            
            // force-gfx-jobs native is a workaround for the Unity bug that causes the game to crash on startup.
            // Taken from SPT Aki.Launcher.Base/Controllers/GameStarter.cs
            argumentsBuilder.Append($"\"{ExecutableFilePath}\" -force-gfx-jobs native ");
            if (!string.IsNullOrEmpty(arguments))
            {
                argumentsBuilder.Append(arguments);
            }
            _process.StartInfo.Arguments = argumentsBuilder.ToString();
            _process.StartInfo.UseShellExecute = false;
            
            _process.StartInfo.EnvironmentVariables["WINEPREFIX"] = linuxConfig.WinePrefix;
            _process.StartInfo.EnvironmentVariables["WINEESYNC"] = linuxConfig.IsEsyncEnabled ? "1" : "0";
            _process.StartInfo.EnvironmentVariables["WINEFSYNC"] = linuxConfig.IsFsyncEnabled ? "1" : "0";
            _process.StartInfo.EnvironmentVariables["WINE_FULLSCREEN_FSR"] = linuxConfig.IsWineFsrEnabled ? "1" : "0";
            _process.StartInfo.EnvironmentVariables["DXVK_NVAPIHACK"] = "0";
            _process.StartInfo.EnvironmentVariables["DXVK_ENABLE_NVAPI"] = linuxConfig.IsDXVK_NVAPIEnabled ? "1" : "0";
            _process.StartInfo.EnvironmentVariables["WINEARCH"] = "win64";
            _process.StartInfo.EnvironmentVariables["MANGOHUD"] = linuxConfig.IsMangoHudEnabled ? "1" : "0";
            _process.StartInfo.EnvironmentVariables["MANGOHUD_DLSYM"] = linuxConfig.IsMangoHudEnabled ? "1" : "0";
            _process.StartInfo.EnvironmentVariables["__GL_SHADER_DISK_CACHE"] = "1";
            _process.StartInfo.EnvironmentVariables["__GL_SHADER_DISK_CACHE_PATH"] = linuxConfig.WinePrefix;
            _process.StartInfo.EnvironmentVariables["DXVK_STATE_CACHE_PATH"] = linuxConfig.WinePrefix;
            // TODO: add the ability to add custom DLL overrides.
            string str = DllManager.GetDllOverride(linuxConfig);
            _process.StartInfo.EnvironmentVariables.Add("WINEDLLOVERRIDES", str);
        }
        else
        {
            _process.StartInfo.WorkingDirectory = ExecutableDirectory;
        }

        _process.Exited += ExitedEvent;
        _process.Start();

        if (_configService.Config.CloseAfterLaunch)
        {
            IApplicationLifetime? lifetime = App.Current.ApplicationLifetime;
            if (lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
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
