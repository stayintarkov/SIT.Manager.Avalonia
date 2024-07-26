using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using SIT.Manager.Converters;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Config;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Timers;

namespace SIT.Manager.Services;

internal sealed class ManagerConfigService : IManagerConfigService
{
    private const string ConfigName = "ManagerConfig.json";

    private readonly JsonSerializerOptions _jsonSerializationOptions = new()
    {
        Converters = { new ColorJsonConverter() }, WriteIndented = true
    };

    private readonly ILogger<ManagerConfigService> _logger;
    private readonly FileInfo _managerConfigPath = new(Path.Combine(AppContext.BaseDirectory, ConfigName));
    private readonly Timer _configSaveTimer = new(TimeSpan.FromSeconds(60));
    private static readonly FileOptions FileStreamOptions = OperatingSystem.IsWindows() ? FileOptions.WriteThrough : FileOptions.None;

    public ManagerConfigService(ILogger<ManagerConfigService> logger)
    {
        _logger = logger;
        Config = LoadConfig();

        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.ShutdownRequested += (_, _) => SaveConfig();
        }

        _configSaveTimer.Elapsed += (_, _) => SaveConfig();
    }

    public ManagerConfig Config { get; }

    private ManagerConfig LoadConfig()
    {
        ManagerConfig ret = new();
        try
        {
            if (_managerConfigPath.Exists)
            {
                ret = JsonSerializer.Deserialize<ManagerConfig>(_managerConfigPath.OpenRead(), _jsonSerializationOptions) ?? ret;
            }
            else
            {
                _logger.LogWarning("{configName} could not be found, Unable to load config file.", ConfigName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while trying to load {configName}", ConfigName);
        }

        return ret;
    }

    private void SaveConfig()
    {
        try
        {
            string serializedConfig = JsonSerializer.Serialize(Config, _jsonSerializationOptions);
            /*
             * NTFS does cached writes. If the file system allocates the file to be written to but is interrupted
             * by something like a system reboot before the cached write occurs then file will be the correct length
             * but will contain only 0x00. This seems unlikely to occur, but it has already happened multiple times.
             */
            using FileStream configFileStream = new(_managerConfigPath.FullName, FileMode.Create,
                FileAccess.Write, FileShare.Read, 4096, FileStreamOptions);
            byte[] data = Encoding.UTF8.GetBytes(serializedConfig);
            configFileStream.Write(data, 0, data.Length);
            configFileStream.Flush();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to save {configName} due to an exception", ConfigName);
        }
    }
}
