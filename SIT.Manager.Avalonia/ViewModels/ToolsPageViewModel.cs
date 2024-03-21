using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.Services;
using SIT.Manager.Avalonia.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class ToolsPageViewModel : ObservableObject
{
    private readonly IAkiServerService _akiServerService;
    private readonly IBarNotificationService _barNotificationService;
    private readonly IManagerConfigService _configService;
    private readonly IFileService _fileService;
    private readonly ITarkovClientService _tarkovClientService;
    private readonly ILocalizationService _localizationService;

    public IAsyncRelayCommand OpenEFTFolderCommand { get; }

    public IAsyncRelayCommand OpenBepInExFolderCommand { get; }

    public IAsyncRelayCommand OpenSITConfigCommand { get; }

    public IAsyncRelayCommand OpenEFTLogCommand { get; }

    public IAsyncRelayCommand ClearCacheCommand { get; }

    public ToolsPageViewModel(IAkiServerService akiServerService,
                              IBarNotificationService barNotificationService,
                              IManagerConfigService configService,
                              IFileService fileService,
                              ILocalizationService localizationService,
                              ITarkovClientService tarkovClientService)
    {
        _akiServerService = akiServerService;
        _barNotificationService = barNotificationService;
        _configService = configService;
        _fileService = fileService;
        _tarkovClientService = tarkovClientService;
        _localizationService = localizationService;

        OpenEFTFolderCommand = new AsyncRelayCommand(OpenEFTFolder);
        OpenBepInExFolderCommand = new AsyncRelayCommand(OpenBepInExFolder);
        OpenSITConfigCommand = new AsyncRelayCommand(OpenSITConfig);
        OpenEFTLogCommand = new AsyncRelayCommand(OpenEFTLog);
        ClearCacheCommand = new AsyncRelayCommand(ClearCache);
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

    // Different versions of SIT has different names
    private readonly HashSet<string> _configPaths =
    [
        "SIT.Core",
        "com.sit.core",
        "com.stayintarkov"
    ];

    private async Task OpenSITConfig()
    {
        string path = Path.Combine(_configService.Config.InstallPath, "BepInEx", "config");
        if (!Directory.Exists(path))
        {
            ContentDialog contentDialog = new()
            {
                Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigSITDescription", path),
                CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
            };
            await contentDialog.ShowAsync();
            return;
        }
        string[] files = Directory.GetFiles(path, "*.cfg");

        string? cfgFile = files.Where(x => _configPaths.Contains(Path.GetFileNameWithoutExtension(x))).FirstOrDefault();

        if (cfgFile != null && File.Exists(cfgFile))
        {
            await _fileService.OpenFileAsync(cfgFile);
        }
        else
        {
            ContentDialog contentDialog = new()
            {
                Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigSITDescription", Path.GetFileName(cfgFile) ?? "\"\""),
                CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
            };
            await contentDialog.ShowAsync();
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
