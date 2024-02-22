using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels
{
    public partial class ModsPageViewModel : ViewModelBase
    {
        private readonly IBarNotificationService _barNotificationService;
        private readonly IManagerConfigService _managerConfigService;
        private readonly ILogger _logger;
        private readonly IModService _modService;

        [ObservableProperty]
        private bool _showModsDisclaimer = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowModInfo))]
        private ModInfo? _selectedMod = null;

        [ObservableProperty]
        private bool _enableInstall = false;

        public bool ShowModInfo => SelectedMod != null;

        public ObservableCollection<ModInfo> ModList { get; } = [];

        public IAsyncRelayCommand DownloadModPackageCommand { get; }

        public IAsyncRelayCommand InstallModCommand { get; }

        public IAsyncRelayCommand UninstallModCommand { get; }

        public ModsPageViewModel(IManagerConfigService managerConfigService,
                                 IBarNotificationService barNotificationService,
                                 ILogger<ModsPageViewModel> logger,
                                 IModService modService) {
            _barNotificationService = barNotificationService;
            _managerConfigService = managerConfigService;
            _logger = logger;
            _modService = modService;

            if (_managerConfigService.Config.AcceptedModsDisclaimer) {
                ShowModsDisclaimer = false;
            }

            DownloadModPackageCommand = new AsyncRelayCommand(DownloadModPackage);
            InstallModCommand = new AsyncRelayCommand(InstallMod);
            UninstallModCommand = new AsyncRelayCommand(UninstallMod);

            this.WhenActivated(async (CompositeDisposable disposables) => {
                /* Handle activation */
                await LoadMasterList();

                Disposable.Create(() => {
                    /* Handle deactivation */
                }).DisposeWith(disposables);
            });
        }

        private async Task LoadMasterList() {
            if (string.IsNullOrEmpty(_managerConfigService.Config.InstallPath)) {
                _barNotificationService.ShowError("Error", "Install Path is not set. Configure it in Settings.");
                return;
            }

            ModList.Clear();

            string modsDirectory = Path.Combine(_managerConfigService.Config.InstallPath, "SITLauncher", "Mods", "Extracted");
            List<ModInfo> outdatedMods = [];

            string modsListFile = Path.Combine(modsDirectory, "MasterList.json");
            if (!File.Exists(modsListFile)) {
                ModList.Add(new ModInfo() {
                    Name = "No mods found"
                });
                return;
            }

            string masterListFile = await File.ReadAllTextAsync(modsListFile);
            List<ModInfo> masterList = JsonSerializer.Deserialize<List<ModInfo>>(masterListFile) ?? [];
            masterList = [.. masterList.OrderBy(x => x.Name)];

            foreach (ModInfo mod in masterList) {
                ModList.Add(mod);

                var keyValuePair = _managerConfigService.Config.InstalledMods.Where(x => x.Key == mod.Name).FirstOrDefault();

                if (!keyValuePair.Equals(default(KeyValuePair<string, string>))) {
                    Version installedVersion = new(keyValuePair.Value);
                    Version currentVersion = new(mod.PortVersion);

                    int result = installedVersion.CompareTo(currentVersion);
                    if (result < 0) {
                        outdatedMods.Add(mod);
                    }
                }
            }

            if (ModList.Count > 0) {
                SelectedMod = ModList[0];
            }

            if (outdatedMods.Count > 0) {
                await _modService.AutoUpdate(outdatedMods);
            }
        }

        [RelayCommand]
        private void AcceptModsDisclaimer() {
            ShowModsDisclaimer = false;

            ManagerConfig config = _managerConfigService.Config;
            config.AcceptedModsDisclaimer = true;
            _managerConfigService.UpdateConfig(config);
        }

        private async Task DownloadModPackage() {
            if (string.IsNullOrEmpty(_managerConfigService.Config.InstallPath)) {
                _barNotificationService.ShowError("Error", "Install Path is not set. Configure it in Settings.");
                return;
            }
            _logger.LogInformation("DownloadModPack: Starting download of mod package.");

            try {
                await _modService.DownloadModsCollection();
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"DownloadModPack");
            }

            await LoadMasterList();
        }

        partial void OnSelectedModChanged(ModInfo? value) {
            if (value == null) {
                return;
            }

            bool isInstalled = _managerConfigService.Config.InstalledMods.ContainsKey(value.Name);
            EnableInstall = !isInstalled;
        }

        private async Task InstallMod() {
            if (SelectedMod == null) {
                return;
            }

            bool installSuccessful = await _modService.InstallMod(SelectedMod);
            EnableInstall = !installSuccessful;
        }

        private async Task UninstallMod() {
            if (SelectedMod == null) {
                return;
            }

            bool uninstallSuccessful = await _modService.UninstallMod(SelectedMod);
            EnableInstall = uninstallSuccessful;
        }
    }
}
