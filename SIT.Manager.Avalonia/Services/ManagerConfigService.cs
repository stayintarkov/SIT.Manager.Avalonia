using Microsoft.Extensions.Logging;
using SIT.Manager.Avalonia.Converters;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SIT.Manager.Avalonia.Services
{
    internal sealed class ManagerConfigService : IManagerConfigService
    {
        private readonly ILogger<ManagerConfigService> _logger;

        private ManagerConfig _config = new();
        public ManagerConfig Config {
            get => _config;
            private set { _config = value; }
        }

        public event EventHandler<ManagerConfig>? ConfigChanged;

        public ManagerConfigService(ILogger<ManagerConfigService> logger) 
        {
            _logger = logger;
            _config.CurrentLanguageSelected = CheckLocaleAvailability(_config.CurrentLanguageSelected);
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
                _logger.LogError(ex, "Failed to load ManagerConfig");
            }
        }


        public void UpdateConfig(ManagerConfig config, bool ShouldSave = true, bool SaveAccount = false) {
            _config = config;

            var options = new JsonSerializerOptions() {
                Converters = {
                    new ColorJsonConverter()
                },
                WriteIndented = true
            };

            if (ShouldSave) {
                ManagerConfig newLauncherConfig = _config;
                if (!SaveAccount) {
                    newLauncherConfig.Username = string.Empty;
                    newLauncherConfig.Password = string.Empty;
                }

                string managerConfigPath = Path.Combine(AppContext.BaseDirectory, "ManagerConfig.json");
                File.WriteAllText(managerConfigPath, JsonSerializer.Serialize(newLauncherConfig, options));
            }

            ConfigChanged?.Invoke(this, _config);
        }

        public string CheckLocaleAvailability(string currentLanguageSelected)
        {
            var assembly = typeof(LocalizationService).Assembly;
            string folderName = string.Format("{0}.Localization", assembly.GetName().Name);
            var availableLocales = assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(folderName) && r.EndsWith(".axaml")).ToList();
            foreach (var item in availableLocales)
            {
                string languageCode = item.Split('.')[^2];
                if (currentLanguageSelected == languageCode)
                {
                    return currentLanguageSelected;
                }
            }
            return "en-US";
        }
    }
}