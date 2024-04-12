using Microsoft.Extensions.Logging;
using SIT.Manager.Converters;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using System;
using System.IO;
using System.Text.Json;

namespace SIT.Manager.Services;

internal sealed class ManagerConfigService : IManagerConfigService
{
    private readonly ILogger<ManagerConfigService> _logger;

    public ManagerConfig Config { get; private set; } = OperatingSystem.IsLinux()
        ? new LinuxConfig()
        : new ManagerConfig();

    public event EventHandler<ManagerConfig>? ConfigChanged;

    private static readonly JsonSerializerOptions Options = new ()
    {
        Converters = {
            new ColorJsonConverter()
        },
        WriteIndented = true
    };

    public ManagerConfigService(ILogger<ManagerConfigService> logger)
    {
        _logger = logger;
        Load();
    }

    private void Load()
    {
        try
        {
            string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
            if (!File.Exists(managerConfigPath))
            {
                return;
            }

            string json = File.ReadAllText(managerConfigPath);
            Config = OperatingSystem.IsLinux()
                ? JsonSerializer.Deserialize<LinuxConfig>(json, Options) ?? new LinuxConfig()
                : JsonSerializer.Deserialize<ManagerConfig>(json, Options) ?? new ManagerConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ManagerConfig");
        }
    }


    public void UpdateConfig(ManagerConfig config, bool shouldSave = true, bool? saveAccount = null)
    {
        Config = config;
        saveAccount ??= config.RememberLogin;
        
        if (shouldSave)
        {
            if (OperatingSystem.IsLinux())
            {
                LinuxConfig newLauncherConfig = (LinuxConfig) Config;
                if (!saveAccount.Value)
                {
                    newLauncherConfig.Username = string.Empty;
                    newLauncherConfig.Password = string.Empty;
                }
                
                string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
                File.WriteAllText(managerConfigPath, JsonSerializer.Serialize(newLauncherConfig, Options));
            }
            else
            {
                ManagerConfig newLauncherConfig = Config;
                if (!saveAccount.Value)
                {
                    newLauncherConfig.Username = string.Empty;
                    newLauncherConfig.Password = string.Empty;
                }

                string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
                File.WriteAllText(managerConfigPath, JsonSerializer.Serialize(newLauncherConfig, Options));
            }
        }

        ConfigChanged?.Invoke(this, Config);
    }
}
