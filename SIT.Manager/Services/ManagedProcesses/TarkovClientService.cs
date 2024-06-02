using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Exceptions;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Config;
using SIT.Manager.Models.Play;
using SIT.Manager.Native.Linux;
using SIT.Manager.Views.Play;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Services.ManagedProcesses;

public class TarkovClientService(
    IAkiServerRequestingService serverRequestingService,
    IBarNotificationService barNotificationService,
    ILocalizationService localizationService,
    ILogger<TarkovClientService> logger,
    IManagerConfigService configService) : ManagedProcess(barNotificationService, configService), ITarkovClientService
{
    private const string TARKOV_EXE = "EscapeFromTarkov.exe";
    private SITConfig _sitConfig => configService.Config.SITSettings;
    private LauncherConfig _launcherConfig => configService.Config.LauncherSettings;

    protected override string EXECUTABLE_NAME => TARKOV_EXE;

    public override string ExecutableDirectory => !string.IsNullOrEmpty(_sitConfig.SitEFTInstallPath)
        ? _sitConfig.SitEFTInstallPath
        : string.Empty;

    public override void ClearCache()
    {
        ClearLocalCache();
        ClearModCache();
    }

    public void ClearLocalCache()
    {
        string eftCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp", "Battlestate Games", "EscapeFromTarkov");

        // Check if the directory exists.
        if (Directory.Exists(eftCachePath))
        {
            Directory.Delete(eftCachePath, true);
        }
        else
        {
            // Handle the case where the cache directory does not exist.
            BarNotificationService.ShowWarning(
                localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorTitle"),
                localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorEFTDescription",
                    eftCachePath));
            return;
        }

        Directory.CreateDirectory(eftCachePath);
        BarNotificationService.ShowInformational(
            localizationService.TranslateSource("TarkovClientServiceCacheClearedTitle"),
            localizationService.TranslateSource("TarkovClientServiceCacheClearedEFTDescription"));
    }

    public async Task<bool> ConnectToServer(AkiServer server, AkiCharacter character)
    {
        string? ProfileID;
        List<AkiMiniProfile> miniProfiles = await serverRequestingService.GetMiniProfilesAsync(server);
        if (miniProfiles.Select(x => x.Username == character.Username).Any())
        {
            logger.LogDebug("Username {Username} was already found on server. Attempting to login...",
                character.Username);
            (string loginRespStr, AkiLoginStatus status) = await serverRequestingService.LoginAsync(server, character);
            switch (status)
            {
                case AkiLoginStatus.Success:
                    logger.LogDebug("Login successful");
                    ProfileID = loginRespStr;
                    break;
                case AkiLoginStatus.AccountNotFound:
                    throw new AccountNotFoundException();
                default:
                    logger.LogDebug("Failed to login with error {status}", status);
                    await new ContentDialog
                    {
                        Title = localizationService.TranslateSource("DirectConnectViewModelLoginErrorTitle"),
                        Content =
                            localizationService.TranslateSource("DirectConnectViewModelLoginIncorrectPassword"),
                        CloseButtonText = localizationService.TranslateSource("DirectConnectViewModelButtonOk")
                    }.ShowAsync();
                    return false;
            }
        }
        else
        {
            throw new AccountNotFoundException();
        }

        character.ProfileID = ProfileID;
        logger.LogDebug("{Username}'s ProfileID is {ProfileID}", character.Username, character.ProfileID);

        // Launch game
        string launchArguments =
            CreateLaunchArguments(new TarkovLaunchConfig { BackendUrl = server.Address.AbsoluteUri.TrimEnd('/') },
                character.ProfileID);
        try
        {
            Start(launchArguments);
            while (State == RunningState.Starting)
            {
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occured while launching Tarkov");
            await new ContentDialog
            {
                Title = localizationService.TranslateSource("ModsPageViewModelErrorTitle"), Content = ex.Message
            }.ShowAsync();
            return false;
        }

        if (_launcherConfig.Config.MinimizeAfterLaunch)
        {
            MinimizeManager();
        }

        if (_launcherConfig.CloseAfterLaunch)
        {
            CloseManager();
        }

        return true;
    }

    public override void Start(string? arguments)
    {
        UpdateRunningState(RunningState.Starting);

        ProcessToManage = new Process
        {
            StartInfo = new ProcessStartInfo(ExecutableFilePath) { UseShellExecute = true, Arguments = arguments },
            EnableRaisingEvents = true
        };
        if (OperatingSystem.IsLinux())
        {
            LinuxConfig linuxConfig = configService.Config.LinuxSettings;

            // Check if either mangohud or gamemode is enabled.
            StringBuilder argumentsBuilder = new();
            if (linuxConfig.IsGameModeEnabled)
            {
                ProcessToManage.StartInfo.FileName = "gamemoderun";
                if (linuxConfig.IsMangoHudEnabled)
                {
                    argumentsBuilder.Append("mangohud ");
                }

                argumentsBuilder.Append($"\"{linuxConfig.WineRunner}\" ");
            }
            else if (linuxConfig.IsMangoHudEnabled) // only mangohud is enabled
            {
                ProcessToManage.StartInfo.FileName = "mangohud";
                argumentsBuilder.Append($"\"{linuxConfig.WineRunner}\" ");
            }
            else
            {
                ProcessToManage.StartInfo.FileName = linuxConfig.WineRunner;
            }

            // force-gfx-jobs native is a workaround for the Unity bug that causes the game to crash on startup.
            // Taken from SPT Aki.Launcher.Base/Controllers/GameStarter.cs
            argumentsBuilder.Append($"\"{ExecutableFilePath}\" -force-gfx-jobs native ");
            if (!string.IsNullOrEmpty(arguments))
            {
                argumentsBuilder.Append(arguments);
            }

            ProcessToManage.StartInfo.Arguments = argumentsBuilder.ToString();
            ProcessToManage.StartInfo.UseShellExecute = false;

            ProcessToManage.StartInfo.EnvironmentVariables["WINEPREFIX"] = linuxConfig.WinePrefix;
            ProcessToManage.StartInfo.EnvironmentVariables["WINEESYNC"] = linuxConfig.IsEsyncEnabled ? "1" : "0";
            ProcessToManage.StartInfo.EnvironmentVariables["WINEFSYNC"] = linuxConfig.IsFsyncEnabled ? "1" : "0";
            ProcessToManage.StartInfo.EnvironmentVariables["WINE_FULLSCREEN_FSR"] = linuxConfig.IsWineFsrEnabled ? "1" : "0";
            ProcessToManage.StartInfo.EnvironmentVariables["DXVK_NVAPIHACK"] = "0";
            ProcessToManage.StartInfo.EnvironmentVariables["DXVK_ENABLE_NVAPI"] = linuxConfig.IsDXVK_NVAPIEnabled ? "1" : "0";
            ProcessToManage.StartInfo.EnvironmentVariables["WINEARCH"] = "win64";
            ProcessToManage.StartInfo.EnvironmentVariables["MANGOHUD"] = linuxConfig.IsMangoHudEnabled ? "1" : "0";
            ProcessToManage.StartInfo.EnvironmentVariables["MANGOHUD_DLSYM"] = linuxConfig.IsMangoHudEnabled ? "1" : "0";
            ProcessToManage.StartInfo.EnvironmentVariables["__GL_SHADER_DISK_CACHE"] = "1";
            ProcessToManage.StartInfo.EnvironmentVariables["__GL_SHADER_DISK_CACHE_PATH"] = linuxConfig.WinePrefix;
            ProcessToManage.StartInfo.EnvironmentVariables["DXVK_STATE_CACHE_PATH"] = linuxConfig.WinePrefix;
            // TODO: add the ability to add custom DLL overrides.
            string str = DllManager.GetDllOverride(linuxConfig);
            ProcessToManage.StartInfo.EnvironmentVariables.Add("WINEDLLOVERRIDES", str);
        }
        else
        {
            ProcessToManage.StartInfo.WorkingDirectory = ExecutableDirectory;
        }

        ProcessToManage.Exited += ExitedEvent;
        ProcessToManage.Start();

        if (_launcherConfig.CloseAfterLaunch)
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

            return;
        }

        UpdateRunningState(RunningState.Running);
    }

    public async Task<AkiCharacter?> CreateCharacter(AkiServer server, string username, string password,
        bool rememberLogin)
    {
        List<TarkovEdition> editions = [];
        AkiServerInfo? serverInfo = await serverRequestingService.GetAkiServerInfoAsync(server);
        if (serverInfo != null)
        {
            editions.AddRange(serverInfo.Editions.Select(edition =>
                new TarkovEdition(edition, serverInfo.Descriptions[edition])));
        }

        CreateCharacterDialogResult result =
            await new CreateCharacterDialogView(username, password, rememberLogin, [.. editions]).ShowAsync();
        if (result.DialogResult != ContentDialogResult.Primary)
        {
            return null;
        }

        AkiCharacter character = new(result.Username, result.Password) { Edition = result.TarkovEdition.Edition };

        logger.LogInformation("Registering new character...");
        (string _, AkiLoginStatus status) = await serverRequestingService.RegisterCharacterAsync(server, character);
        if (status != AkiLoginStatus.Success)
        {
            await new ContentDialog
            {
                Title = localizationService.TranslateSource("DirectConnectViewModelLoginErrorTitle"),
                Content = localizationService.TranslateSource("DirectConnectViewModelLoginErrorDescription"),
                CloseButtonText = localizationService.TranslateSource("DirectConnectViewModelButtonOk")
            }.ShowAsync();
            logger.LogDebug("Register character failed with {status}", status);
            return null;
        }

        if (result.SaveLogin)
        {
            server.Characters.Add(character);
            int index = _sitConfig.BookmarkedServers.FindIndex(x => x.Address == server.Address);
            if (index != -1 && !_sitConfig.BookmarkedServers[index].Characters
                    .Any(x => x.Username == character.Username))
            {
                _sitConfig.BookmarkedServers[index].Characters.Add(character);
            }
        }

        return character;
    }

    public async Task<AkiCharacter?> CreateCharacter(AkiServer server)
    {
        return await CreateCharacter(server, string.Empty, string.Empty, false).ConfigureAwait(false);
    }

    private void ClearModCache()
    {
        string cachePath = _sitConfig.SitEFTInstallPath;
        if (!string.IsNullOrEmpty(cachePath) && Directory.Exists(cachePath))
        {
            // Combine the installPath with the additional subpath.
            DirectoryInfo cacheDirectory = new(Path.Combine(cachePath, "BepInEx", "cache"));
            if (cacheDirectory.Exists)
            {
                cacheDirectory.Delete(true);
            }

            cacheDirectory.Create();
            BarNotificationService.ShowInformational(
                localizationService.TranslateSource("TarkovClientServiceCacheClearedTitle"),
                localizationService.TranslateSource("TarkovClientServiceCacheClearedDescription"));
        }
        else
        {
            // Handle the case where InstallPath is not found or empty.
            BarNotificationService.ShowError(
                localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorTitle"),
                localizationService.TranslateSource("TarkovClientServiceCacheClearedErrorDescription"));
        }
    }

    private static void CloseManager()
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

    private static string CreateLaunchArguments(TarkovLaunchConfig launchConfig, string token)
    {
        string jsonConfig = JsonSerializer.Serialize(launchConfig);

        // The json needs single quotes on Linux for some reason even though not valid json
        if (OperatingSystem.IsLinux())
        {
            jsonConfig = jsonConfig.Replace('\"', '\'');
        }

        Dictionary<string, string> argumentList = new() { { "-token", token }, { "-config", jsonConfig } };

        string launchArguments = string.Join(' ', argumentList.Select(argument => $"{argument.Key}={argument.Value}"));
        if (OperatingSystem.IsLinux())
        {
            // We need to make sure that the json is contained in quotes on Linux otherwise you won't be able to connect to the server.
            launchArguments = string.Join(' ', argumentList.Select(argument => $"{argument.Key}=\"{argument.Value}\""));
        }

        return launchArguments;
    }
}
