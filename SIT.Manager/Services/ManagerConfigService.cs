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
            if (File.Exists(managerConfigPath))
            {
                string json = File.ReadAllText(managerConfigPath);
                _config = JsonSerializer.Deserialize<ManagerConfig>(json, _jsonSerializationOptions) ?? new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ManagerConfig");
        }
    }


    public void UpdateConfig(ManagerConfig config, bool ShouldSave = true, bool? SaveAccount = null)
    {
        _config = config;
        SaveAccount ??= config.RememberLogin;

        if (ShouldSave)
        {
            ManagerConfig newLauncherConfig = _config;
            if (!SaveAccount.Value)
            {
                newLauncherConfig.Username = string.Empty;
                newLauncherConfig.Password = string.Empty;
            }

            string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
            File.WriteAllText(managerConfigPath, JsonSerializer.Serialize(newLauncherConfig, _jsonSerializationOptions));
        }

        ConfigChanged?.Invoke(this, _config);
    }
}
