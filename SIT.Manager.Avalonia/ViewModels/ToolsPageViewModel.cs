using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Controls;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.Services;
using SIT.Manager.Avalonia.Views;
using SIT.Manager.Avalonia.Views.Dialogs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels
{
    public partial class ToolsPageViewModel : ViewModelBase
    {
        private readonly IAkiServerService _akiServerService;
        private readonly IBarNotificationService _barNotificationService;
        private readonly IManagerConfigService _configService;
        private readonly IFileService _fileService;
        private readonly IInstallerService _installerService;
        private readonly ITarkovClientService _tarkovClientService;

        public IAsyncRelayCommand InstallSITCommand { get; }

        public IAsyncRelayCommand OpenEFTFolderCommand { get; }

        public IAsyncRelayCommand OpenBepInExFolderCommand { get; }

        public IAsyncRelayCommand OpenSITConfigCommand { get; }

        public IAsyncRelayCommand InstallServerCommand { get; }

        public IAsyncRelayCommand OpenEFTLogCommand { get; }

        public IAsyncRelayCommand ClearCacheCommand { get; }

        public ToolsPageViewModel(IAkiServerService akiServerService,
                                  IBarNotificationService barNotificationService,
                                  IManagerConfigService configService,
                                  IFileService fileService,
                                  IInstallerService installerService,
                                  ITarkovClientService tarkovClientService) {
            _akiServerService = akiServerService;
            _barNotificationService = barNotificationService;
            _configService = configService;
            _fileService = fileService;
            _installerService = installerService;
            _tarkovClientService = tarkovClientService;

            InstallSITCommand = new AsyncRelayCommand(InstallSIT);
            OpenEFTFolderCommand = new AsyncRelayCommand(OpenETFFolder);
            OpenBepInExFolderCommand = new AsyncRelayCommand(OpenBepInExFolder);
            OpenSITConfigCommand = new AsyncRelayCommand(OpenSITConfig);
            InstallServerCommand = new AsyncRelayCommand(InstallServer);
            OpenEFTLogCommand = new AsyncRelayCommand(OpenEFTLog);
            ClearCacheCommand = new AsyncRelayCommand(ClearCache);
        }

        private async Task InstallSIT() {
            SelectSitVersionDialog selectWindow = new();
            ContentDialogResult result = await selectWindow.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                GithubRelease? selectedVersion = selectWindow.GetSelectedGithubRelease();
                if (selectedVersion != null) {
                    await _installerService.InstallSIT(selectedVersion);
                }
            }
        }

        private async Task OpenETFFolder() {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath)) {
                ContentDialog contentDialog = new() {
                    Title = "Config Error",
                    Content = "'Install Path' is not configured. Go to settings to configure the installation path.",
                    CloseButtonText = "Ok"
                };
                await contentDialog.ShowAsync();
            }
            else {
                await _fileService.OpenDirectoryAsync(_configService.Config.InstallPath);
            }
        }

        private async Task OpenBepInExFolder() {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath)) {
                ContentDialog contentDialog = new() {
                    Title = "Config Error",
                    Content = "'Install Path' is not configured. Go to settings to configure the installation path.",
                    CloseButtonText = "Ok"
                };
                await contentDialog.ShowAsync();
            }
            else {
                string dirPath = Path.Combine(_configService.Config.InstallPath, "BepInEx", "plugins");
                if (Directory.Exists(dirPath)) {
                    await _fileService.OpenDirectoryAsync(dirPath);
                }
                else {
                    ContentDialog contentDialog = new() {
                        Title = "Config Error",
                        Content = $"Could not find the given path. Is BepInEx installed?\nPath: {dirPath}",
                        CloseButtonText = "Ok"
                    };
                    await contentDialog.ShowAsync();
                }
            }
        }

        private async Task OpenSITConfig() {
            string path = Path.Combine(_configService.Config.InstallPath, "BepInEx", "config");
            string sitCfg = @"SIT.Core.cfg";

            // Different versions of SIT has different names
            if (!File.Exists(Path.Combine(path, sitCfg))) {
                sitCfg = "com.sit.core.cfg";
            }

            if (!File.Exists(Path.Combine(path, sitCfg))) {
                ContentDialog contentDialog = new() {
                    Title = "Config Error",
                    Content = $"Could not find '{sitCfg}'. Make sure SIT is installed and that you have started the game once.",
                    CloseButtonText = "Ok"
                };
                await contentDialog.ShowAsync();
            }
            else {
                await _fileService.OpenFileAsync(Path.Combine(path, sitCfg));
            }
        }

        private async Task InstallServer() {
            SelectServerVersionDialog selectWindow = new();
            ContentDialogResult result = await selectWindow.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                GithubRelease? selectedVersion = selectWindow.GetSelectedGithubRelease();
                if (selectedVersion != null) {
                    await _installerService.InstallServer(selectedVersion);
                }
            }
        }

        private async Task OpenEFTLog() {
            // TODO fix this for linux :)
            string logPath = @"%userprofile%\AppData\LocalLow\Battlestate Games\EscapeFromTarkov\Player.log";
            logPath = Environment.ExpandEnvironmentVariables(logPath);
            if (File.Exists(logPath)) {
                await _fileService.OpenFileAsync(logPath);
            }
            else {
                ContentDialog contentDialog = new() {
                    Title = "Config Error",
                    Content = "Log file could not be found.",
                    CloseButtonText = "Ok"
                };
                await contentDialog.ShowAsync();
            }
        }

        [RelayCommand]
        private void OpenLocationEditor() {
            PageNavigation pageNavigation = new(typeof(LocationEditorView), false);
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
        }

        private async Task ClearCache() {
            // Prompt the user for their choice using a dialog.
            ContentDialog choiceDialog = new() {
                Title = "Clear Cache",
                Content = "Do you want to clear the EFT local cache or clear all cache?",
                PrimaryButtonText = "Clear EFT Local Cache",
                SecondaryButtonText = "Clear All Cache",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await choiceDialog.ShowAsync();

            if (result == ContentDialogResult.Primary) {
                // User chose to clear EFT local cache.
                try {
                    _tarkovClientService.ClearLocalCache();
                }
                catch (Exception ex) {
                    // Handle any exceptions that may occur during the process.
                    _barNotificationService.ShowError("Error", $"An error occurred: {ex.Message}");
                }
            }
            else if (result == ContentDialogResult.Secondary) {
                // User chose to clear everything.
                try {
                    _akiServerService.ClearCache();
                    _tarkovClientService.ClearCache();
                }
                catch (Exception ex) {
                    // Handle any exceptions that may occur during the process.
                    _barNotificationService.ShowError("Error", $"An error occurred: {ex.Message}");
                }
            }
        }
    }
}
