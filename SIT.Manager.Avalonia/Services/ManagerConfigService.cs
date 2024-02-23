using SIT.Manager.Avalonia.Converters;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SIT.Manager.Avalonia.Services
{
    internal sealed class ManagerConfigService : IManagerConfigService
    {
        private ManagerConfig _config = new();
        public ManagerConfig Config {
            get => _config;
            private set { _config = value; }
        }

        public event EventHandler<ManagerConfig>? ConfigChanged;

        public ManagerConfigService() {
            Load();
        }

        private void Load() {
            var options = new JsonSerializerOptions() {
                Converters = {
                    new ColorJsonConverter()
                }
            };

            try {
                string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
                if (File.Exists(managerConfigPath)) {
                    string json = File.ReadAllText(managerConfigPath);
                    _config = JsonSerializer.Deserialize<ManagerConfig>(json, options) ?? new();
                }
            }
            catch (Exception ex) {
                // TODO Loggy.LogToFile("ManagerConfig.Load: " + ex.Message);
            }
        }


        public void UpdateConfig(ManagerConfig config, bool ShouldSave = true, bool SaveAccount = false) {
            _config = config;

            var options = new JsonSerializerOptions()
            {
                Converters = {
                    new ColorJsonConverter()
                },
                WriteIndented = true
            };

            if (ShouldSave)
            {
                ManagerConfig newLauncherConfig = _config;
                if (!SaveAccount)
                {
                    newLauncherConfig.Username = string.Empty;
                    newLauncherConfig.Password = string.Empty;
                }

                string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
                File.WriteAllText(managerConfigPath, JsonSerializer.Serialize(newLauncherConfig, options));
            }

            ConfigChanged?.Invoke(this, _config);
        }
    }
}
