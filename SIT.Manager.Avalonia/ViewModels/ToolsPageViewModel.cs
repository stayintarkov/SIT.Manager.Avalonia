using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.Services;
using SIT.Manager.Avalonia.Views;
using SIT.Manager.Avalonia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly IVersionService _versionService;
        private readonly ILocalizationService _localizationService;

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
                                  ILocalizationService localizationService,
                                  IInstallerService installerService,
                                  ITarkovClientService tarkovClientService,
                                  IVersionService versionService)
        {
            _akiServerService = akiServerService;
            _barNotificationService = barNotificationService;
            _configService = configService;
            _fileService = fileService;
            _installerService = installerService;
            _tarkovClientService = tarkovClientService;
            _versionService = versionService;
            _localizationService = localizationService;

            InstallSITCommand = new AsyncRelayCommand(InstallSIT);
            OpenEFTFolderCommand = new AsyncRelayCommand(OpenEFTFolder);
            OpenBepInExFolderCommand = new AsyncRelayCommand(OpenBepInExFolder);
            OpenSITConfigCommand = new AsyncRelayCommand(OpenSITConfig);
            InstallServerCommand = new AsyncRelayCommand(InstallServer);
            OpenEFTLogCommand = new AsyncRelayCommand(OpenEFTLog);
            ClearCacheCommand = new AsyncRelayCommand(ClearCache);
        }

        /// <summary>
        /// Check the current version of EFT and update the version in the config if it's different
        /// </summary>
        private void CheckTarkovVersion()
        {
            ManagerConfig config = _configService.Config;
            string tarkovVersion = _versionService.GetEFTVersion(config.InstallPath);
            if (tarkovVersion != config.TarkovVersion)
            {
                config.TarkovVersion = tarkovVersion;
                _configService.UpdateConfig(config);
            }
        }

        private async Task<GithubRelease?> EnsureEftVersion(List<GithubRelease> releases)
        {
            if (!releases.Any())
            {
                _barNotificationService.ShowWarning(_localizationService.TranslateSource("ToolsPageViewModelErrorMessageTitle"), _localizationService.TranslateSource("ToolsPageViewModelErrorMessageDescription"));
                return null;
            }

            SelectVersionDialog selectWindow = new(releases);
            GithubRelease? selectedVersion = await selectWindow.ShowAsync();
            if (selectedVersion == null)
            {
                return null;
            }

            // Ensure the tarkov version is up to date before we check it
            CheckTarkovVersion();
            if (_configService.Config.TarkovVersion != selectedVersion.body)
            {
                Dictionary<string, string>? availableMirrors = await _installerService.GetAvaiableMirrorsForVerison(selectedVersion.body);
                if (availableMirrors == null)
                {
                    return null;
                }

                SelectDowngradePatcherMirrorDialog selectDowngradePatcherWindow = new(availableMirrors);
                string? selectedMirrorUrl = await selectDowngradePatcherWindow.ShowAsync();
                if (string.IsNullOrEmpty(selectedMirrorUrl))
                {
                    return null;
                }

                await _installerService.DownloadAndRunPatcher(selectedMirrorUrl);
                CheckTarkovVersion();
            }

            return selectedVersion;
        }

        private async Task InstallSIT()
        {
            List<GithubRelease> sitReleases = await _installerService.GetSITReleases();
            GithubRelease? versionToInstall = await EnsureEftVersion(sitReleases);
            if (versionToInstall != null)
            {
                await _installerService.InstallSIT(versionToInstall);
            }
        }

        private async Task OpenEFTFolder()
        {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath))
            {
                ContentDialog contentDialog = new()
                {
                    Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                    Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigDescription"),
                    CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
                };
                await contentDialog.ShowAsync();
            }
            else
            {
                await _fileService.OpenDirectoryAsync(_configService.Config.InstallPath);
            }
        }

        private async Task OpenBepInExFolder()
        {
            if (string.IsNullOrEmpty(_configService.Config.InstallPath))
            {
                ContentDialog contentDialog = new()
                {
                    Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                    Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigDescription"),
                    CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
                };
                await contentDialog.ShowAsync();
            }
            else
            {
                string dirPath = Path.Combine(_configService.Config.InstallPath, "BepInEx", "plugins");
                if (Directory.Exists(dirPath))
                {
                    await _fileService.OpenDirectoryAsync(dirPath);
                }
                else
                {
                    ContentDialog contentDialog = new()
                    {
                        Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                        Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigBepInExDescription", dirPath),
                        CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
                    };
                    await contentDialog.ShowAsync();
                }
            }
        }

        private async Task OpenSITConfig()
        {
            string path = Path.Combine(_configService.Config.InstallPath, "BepInEx", "config");
            string sitCfg = @"SIT.Core.cfg";

            // Different versions of SIT has different names
            if (!File.Exists(Path.Combine(path, sitCfg)))
            {
                sitCfg = "com.sit.core.cfg";
            }

            if (!File.Exists(Path.Combine(path, sitCfg)))
            {
                ContentDialog contentDialog = new()
                {
                    Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                    Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigSITDescription", sitCfg),
                    CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
                };
                await contentDialog.ShowAsync();
            }
            else
            {
                await _fileService.OpenFileAsync(Path.Combine(path, sitCfg));
            }
        }

        private async Task InstallServer()
        {
            List<GithubRelease> serverReleases = await _installerService.GetServerReleases();
            GithubRelease? versionToInstall = await EnsureEftVersion(serverReleases);
            if (versionToInstall != null)
            {
                await _installerService.InstallServer(versionToInstall);
            }
        }

        private async Task OpenEFTLog()
        {
            // TODO fix this for linux :)
            string logPath = @"%userprofile%\AppData\LocalLow\Battlestate Games\EscapeFromTarkov\Player.log";
            logPath = Environment.ExpandEnvironmentVariables(logPath);
            if (File.Exists(logPath))
            {
                await _fileService.OpenFileAsync(logPath);
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                    Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigLogDescription"),
                    CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
                };
                await contentDialog.ShowAsync();
            }
        }

        [RelayCommand]
        private void OpenLocationEditor()
        {
            PageNavigation pageNavigation = new(typeof(LocationEditorView), false);
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
        }

        private async Task ClearCache()
        {
            // Prompt the user for their choice using a dialog.
            ContentDialog choiceDialog = new()
            {
                Title = _localizationService.TranslateSource("ToolsPageViewModelConfigClearEFTTitle"),
                Content = _localizationService.TranslateSource("ToolsPageViewModelConfigClearEFTDescription"),
                PrimaryButtonText = _localizationService.TranslateSource("ToolsPageViewModelConfigClearEFTCache"),
                SecondaryButtonText = _localizationService.TranslateSource("ToolsPageViewModelConfigClearAllCache"),
                CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonCancel")
            };
            ContentDialogResult result = await choiceDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // User chose to clear EFT local cache.
                try
                {
                    _tarkovClientService.ClearLocalCache();
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the process.
                    _barNotificationService.ShowError(_localizationService.TranslateSource("ToolsPageViewModelErrorMessageTitle"), _localizationService.TranslateSource("ToolsPageViewModelUnhandledExceptionError", ex.Message));
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // User chose to clear everything.
                try
                {
                    _akiServerService.ClearCache();
                    _tarkovClientService.ClearCache();
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the process.
                    _barNotificationService.ShowError(_localizationService.TranslateSource("ToolsPageViewModelErrorMessageTitle"), _localizationService.TranslateSource("ToolsPageViewModelUnhandledExceptionError", ex.Message));
                }
            }
        }
    }
}