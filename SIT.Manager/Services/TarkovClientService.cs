using Avalonia.Controls.ApplicationLifetimes;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using System;
using System.Diagnostics;
using System.IO;

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
            StartInfo = new(ExecutableFilePath)
            {
                UseShellExecute = true,
                Arguments = arguments
            },
            EnableRaisingEvents = true,
        };
        if (OperatingSystem.IsLinux())
        {
            LinuxConfig config = (LinuxConfig)_configService.Config;

            // Check if either mangohud or gamemode is enabled.
            if (config.IsGameModeEnabled)
            {
                _process.StartInfo.FileName = "gamemoderun";
                _process.StartInfo.Arguments = string.Empty;
                if (config.IsMangoHudEnabled)
                {
                    _process.StartInfo.Arguments += "\"mangohud\"";
                }
                _process.StartInfo.Arguments += $"\"{config.WineRunner}\"";
            }
            else if (config.IsMangoHudEnabled) // only mangohud is enabled
            {
                _process.StartInfo.FileName = "mangohud";
                _process.StartInfo.Arguments = $"\"{config.WineRunner}\"";
            }
            else
            {
                _process.StartInfo.FileName = config.WineRunner;
                _process.StartInfo.Arguments = string.Empty;
            }
            
            
            //_process.StartInfo.FileName = config.IsGameModeEnabled ? "gamemoderun" : config.IsMangoHudEnabled ? "mangohud" : _configService.Config.WineRunner;
            
            // force-gfx-jobs native is a workaround for the Unity bug that causes the game to crash on startup.
            // Taken from SPT Aki.Launcher.Base/Controllers/GameStarter.cs
            _process.StartInfo.Arguments += $"\"{ExecutableFilePath}\" -force-gfx-jobs native {arguments}"; 
            _process.StartInfo.UseShellExecute = false;

            string winePrefix = Path.GetFullPath(_configService.Config.WinePrefix);
            if (!Path.EndsInDirectorySeparator(winePrefix))
            {
                winePrefix = $"{winePrefix}{Path.DirectorySeparatorChar}";
            }
            
            _process.StartInfo.EnvironmentVariables.Add("WINEPREFIX", winePrefix);
            _process.StartInfo.EnvironmentVariables.Add("WINEESYNC", config.IsEsyncEnabled ? "1" : "0");
            _process.StartInfo.EnvironmentVariables.Add("WINEFSYNC", config.IsFsyncEnabled ? "1" : "0");
            _process.StartInfo.EnvironmentVariables.Add("WINE_FULLSCREEN_FSR", config.IsWineFsrEnabled ? "1" : "0");
            _process.StartInfo.EnvironmentVariables.Add("DXVK_NVAPIHACK", config.IsDXVK_NVAPIEnabled ? "1" : "0");
            _process.StartInfo.EnvironmentVariables.Add("DXVK_ENABLE_NVAPI", config.IsDXVK_NVAPIEnabled ? "1" : "0");
            _process.StartInfo.EnvironmentVariables.Add("WINEARCH", "win64");
            _process.StartInfo.EnvironmentVariables.Add("MANGOHUD", config.IsMangoHudEnabled ? "1" : "0");
            _process.StartInfo.EnvironmentVariables.Add("MANGOHUD_DLSYM", config.IsMangoHudEnabled ? "1" : "0");
            _process.StartInfo.EnvironmentVariables.Add("__GL_SHADER_DISK_CACHE", "1");
            _process.StartInfo.EnvironmentVariables.Add("__GL_SHADER_DISK_CACHE_PATH", winePrefix);
            _process.StartInfo.EnvironmentVariables.Add("DXVK_STATE_CACHE_PATH", winePrefix);
            // TODO: configure these with the DLLManager and add the ability to add custom DLL overrides.
            _process.StartInfo.EnvironmentVariables.Add("WINEDLLOVERRIDES", "\"d3d10core,d3d11,d3d12,d3d12core,d3d9,d3dcompiler_33,d3dcompiler_34,d3dcompiler_35,d3dcompiler_36,d3dcompiler_37,d3dcompiler_38,d3dcompiler_39,d3dcompiler_40,d3dcompiler_41,d3dcompiler_42,d3dcompiler_43,d3dcompiler_46,d3dcompiler_47,d3dx10,d3dx10_33,d3dx10_34,d3dx10_35,d3dx10_36,d3dx10_37,d3dx10_38,d3dx10_39,d3dx10_40,d3dx10_41,d3dx10_42,d3dx10_43,d3dx11_42,d3dx11_43,d3dx9_24,d3dx9_25,d3dx9_26,d3dx9_27,d3dx9_28,d3dx9_29,d3dx9_30,d3dx9_31,d3dx9_32,d3dx9_33,d3dx9_34,d3dx9_35,d3dx9_36,d3dx9_37,d3dx9_38,d3dx9_39,d3dx9_40,d3dx9_41,d3dx9_42,d3dx9_43,dxgi,nvapi,nvapi64=n;winemenubuilder=");
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
