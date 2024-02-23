using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels
{
    public partial class LocationEditorViewModel : ViewModelBase
    {
        private readonly IBarNotificationService _barNotificationService;
        private readonly IPickerDialogService _pickerDialogService;

        private static Dictionary<string, string> _mapLocationMapping = new Dictionary<string, string>() {
            { "maps/factory_day_preset.bundle","Factory (Day)" },
            { "maps/factory_night_preset.bundle",  "Factory (Night)" },
            { "maps/woods_preset.bundle",  "Woods" },
            { "maps/customs_preset.bundle", "Customs" },
            { "maps/shopping_mall.bundle", "Interchange" },
            { "maps/rezerv_base_preset.bundle", "Reserve" },
            { "maps/shoreline_preset.bundle", "Shoreline" },
            { "maps/laboratory_preset.bundle", "Labs" },
            { "maps/lighthouse_preset.bundle", "Lighthouse" },
            { "maps/city_preset.bundle","Streets" }
        };

        [ObservableProperty]
        private BaseLocation? _location;

        [ObservableProperty]
        private Wave? _selectedWave;

        [ObservableProperty]
        private BossLocationSpawn? _selectedBossLocationSpawn;

        [ObservableProperty]
        private int _selectedWaveIndex = 0;

        [ObservableProperty]
        private string _loadedLocation = string.Empty;

        public List<string> BotDifficulty => [
            "easy",
            "normal",
            "hard"
        ];

        public List<string> BotSide => [
            "Savage",
            "Bear",
            "Usec"
        ];

        public List<string> WildSpawnType => [
            "assault",
            "marksman"
        ];

        public List<string> BossEscortType => [
            "followerBully",
            "followerTagilla",
            "followerZryachiy",
            "followerGluharAssault",
            "followerSanitar",
            "followerKojaniy",
            "followerBoar",
            "bossBoarSniper",
            "crazyAssaultEvent",
            "pmcBot",
            "exUsec",
            "sectantWarrior",
            "arenaFighterEvent"
        ];

        public List<string> BossName => [
            "bossKnight",
            "bossBully",
            "bossTagilla",
            "bossKilla",
            "bossZryachiy",
            "bossGluhar",
            "bossSanitar",
            "bossKojaniy",
            "bossBoar",
            "bossBoarSniper",
            "sectantPriest",
            "arenaFighterEvent",
            "pmcBot",
            "exUsec",
            "crazyAssaultEvent"
        ];

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }

        public LocationEditorViewModel(IBarNotificationService barNotificationService, IPickerDialogService pickerDialogService) {
            _barNotificationService = barNotificationService;
            _pickerDialogService = pickerDialogService;

            LoadCommand = new AsyncRelayCommand(Load);
            SaveCommand = new AsyncRelayCommand(Save);
        }

        private async Task Load() {
            IStorageFile? file = await _pickerDialogService.GetFileFromPickerAsync();
            if (file == null) {
                return;
            }

            if (File.Exists(file.Path.LocalPath)) {
                string jsonString = await File.ReadAllTextAsync(file.Path.LocalPath);
                BaseLocation? location = JsonSerializer.Deserialize<BaseLocation>(jsonString);
                if (location == null) {
                    _barNotificationService.ShowError("Load Error", "There was an error saving the file.");
                    return;
                }

                for (int i = 0; i < location.waves.Count; i++) {
                    location.waves[i].Name = i + 1;
                }

                for (int i = 0; i < location.BossLocationSpawn.Count; i++) {
                    location.BossLocationSpawn[i].Name = i + 1;
                }

                _mapLocationMapping.TryGetValue(location.Scene.path, out string? map);
                LoadedLocation = map ?? "Unknown Location";

                Location = location;

                if (location.waves.Count > 0) {
                    SelectedWave = location.waves[0];
                }

                if (location.BossLocationSpawn.Count > 0) {
                    SelectedBossLocationSpawn = location.BossLocationSpawn[0];
                }

                _barNotificationService.ShowSuccess("Load Location", $"Loaded location {LoadedLocation} successfully.");
            }
        }

        private async Task Save() {
            IStorageFile? file = await _pickerDialogService.GetFileSaveFromPickerAsync(".json", "base.json");
            if (file == null) {
                return;
            }

            if (File.Exists(file.Path.LocalPath)) {
                string backupFilePath = Path.Combine(file.Path.LocalPath, ".BAK");
                File.Copy(file.Path.LocalPath, backupFilePath, true);
            }

            if (Location == null) {
                _barNotificationService.ShowError("Save Error", "There was an error saving the file.");
                return;
            }
            string json = JsonSerializer.Serialize(Location, new JsonSerializerOptions() { WriteIndented = true });
            await File.WriteAllTextAsync(file.Path.LocalPath, json);

            _barNotificationService.ShowSuccess("Save", $"Successfully saved the file to: {file.Path}");
        }

        [RelayCommand]
        private void AddWave() {
            /* TODO not really any point implementing this the funciton isn't enabled anyway            
            BaseLocation location = (BaseLocation)DataContext;

            if (location != null)
            {
                location.waves.Add(new Wave());

                for (int i = 0; i < location.waves.Count; i++)
                {
                    location.waves[i].Name = i + 1;
                }

                if (location.waves.Count > 0)
                {
                    WaveList.SelectedIndex = 0;
                }
            }
            */
        }

        [RelayCommand]
        private void RemoveWave() {
            /* TODO not really any point implementing this the funciton isn't enabled anyway
            if (WaveList.SelectedIndex == -1)
                return;

            BaseLocation location = (BaseLocation)DataContext;

            if (location != null)
            {
                location.waves.RemoveAt(WaveList.SelectedIndex);

                for (int i = 0; i < location.waves.Count; i++)
                {
                    location.waves[i].Name = i + 1;
                }

                if (location.waves.Count > 0)
                {
                    WaveList.SelectedIndex = 0;
                }
            }
            */
        }

        [RelayCommand]
        private void AddBoss() {
            /* TODO not really any point implementing this the funciton isn't enabled anyway
            BaseLocation location = (BaseLocation) DataContext;

            if (location != null) {
                location.BossLocationSpawn.Add(new BossLocationSpawn());

                for (int i = 0; i < location.BossLocationSpawn.Count; i++) {
                    location.BossLocationSpawn[i].Name = i + 1;
                }

                if (location.BossLocationSpawn.Count > 0) {
                    BossList.SelectedIndex = 0;
                }
            }
            */
        }

        [RelayCommand]
        private void RemoveBossCommand() {
            /* TODO not really any point implementing this the funciton isn't enabled anyway
            if (BossList.SelectedIndex == -1)
                return;

            BaseLocation location = (BaseLocation) DataContext;

            if (location != null) {
                location.BossLocationSpawn.RemoveAt(BossList.SelectedIndex);

                for (int i = 0; i < location.BossLocationSpawn.Count; i++) {
                    location.BossLocationSpawn[i].Name = i + 1;
                }

                if (location.BossLocationSpawn.Count > 0) {
                    BossList.SelectedIndex = 0;
                }
            }
            */
        }
    }
}
