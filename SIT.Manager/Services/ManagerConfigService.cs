using Microsoft.Extensions.Logging;
using SIT.Manager.Converters;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Config;
using System;
using System.IO;
using System.Text.Json;

namespace SIT.Manager.Services;

internal sealed class ManagerConfigService : IManagerConfigService
{
    private readonly ILogger<ManagerConfigService> _logger;

    private readonly JsonSerializerOptions _jsonSerializationOptions = new()
    {
        Converters = {
            new ColorJsonConverter()
        },
        WriteIndented = true
    };

    private ManagerConfig _config = new();
    public ManagerConfig Config
    {
        get => _config;
        private set { _config = value; }
    }

    public event EventHandler<ManagerConfig>? ConfigChanged;

    private static readonly JsonSerializerOptions Options = new()
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
            Config = JsonSerializer.Deserialize<ManagerConfig>(json, _jsonSerializationOptions) ?? new ManagerConfig();
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
            ManagerConfig newLauncherConfig = Config;
            if (!saveAccount.Value)
            {
                newLauncherConfig.Username = string.Empty;
                newLauncherConfig.Password = string.Empty;
            }

            string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
            File.WriteAllText(managerConfigPath, JsonSerializer.Serialize(newLauncherConfig, _jsonSerializationOptions));
        }

        ConfigChanged?.Invoke(this, Config);
    }
}
