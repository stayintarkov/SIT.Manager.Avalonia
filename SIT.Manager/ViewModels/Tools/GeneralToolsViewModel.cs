using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models;
using SIT.Manager.Models.Messages;
using SIT.Manager.Services;
using SIT.Manager.Views;
using SIT.Manager.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Tools;
public partial class GeneralToolsViewModel : ObservableObject
{
    private readonly IAkiServerService _akiServerService;
    private readonly IBarNotificationService _barNotificationService;
    private readonly IManagerConfigService _configService;
    private readonly IFileService _fileService;
    private readonly IModService _modService;
    private readonly ITarkovClientService _tarkovClientService;
    private readonly ILocalizationService _localizationService;
    private readonly IDiagnosticService _diagnosticService;
    private static string EFTLogPath
    {
        get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow", "Battlestate Games", "EscapeFromTarkov", "Player.log");
    }

    public IAsyncRelayCommand OpenEFTFolderCommand { get; }

    public IAsyncRelayCommand OpenBepInExFolderCommand { get; }

    public IAsyncRelayCommand OpenServerFolderCommand { get; }

    public IAsyncRelayCommand OpenSITConfigCommand { get; }

    public IAsyncRelayCommand OpenEFTLogCommand { get; }

    public IAsyncRelayCommand ClearCacheCommand { get; }

    public IAsyncRelayCommand GenerateDiagnosticReportCommand { get; }

    public GeneralToolsViewModel(IAkiServerService akiServerService,
                              IBarNotificationService barNotificationService,
                              IManagerConfigService configService,
                              IFileService fileService,
                              ILocalizationService localizationService,
                              IModService modService,
                              ITarkovClientService tarkovClientService,
                              IDiagnosticService diagnosticService)
    {
        _akiServerService = akiServerService;
        _barNotificationService = barNotificationService;
        _configService = configService;
        _fileService = fileService;
        _tarkovClientService = tarkovClientService;
        _localizationService = localizationService;
        _modService = modService;
        _diagnosticService = diagnosticService;

        OpenEFTFolderCommand = new AsyncRelayCommand(OpenEFTFolder);
        OpenBepInExFolderCommand = new AsyncRelayCommand(OpenBepInExFolder);
        OpenServerFolderCommand = new AsyncRelayCommand(OpenServerFolder);
        OpenSITConfigCommand = new AsyncRelayCommand(OpenSITConfig);
        OpenEFTLogCommand = new AsyncRelayCommand(OpenEFTLog);
        ClearCacheCommand = new AsyncRelayCommand(ClearCache);
        GenerateDiagnosticReportCommand = new AsyncRelayCommand(GenerateDiagnosticReport);
    }

    private async Task OpenEFTFolder()
    {
        if (string.IsNullOrEmpty(_configService.Config.SitEftInstallPath))
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
            await _fileService.OpenDirectoryAsync(_configService.Config.SitEftInstallPath);
        }
    }

    private async Task OpenServerFolder()
    {
        if (string.IsNullOrEmpty(_configService.Config.AkiServerPath))
        {
            ContentDialog contentDialog = new()
            {
                Title = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigTitle"),
                Content = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageServerConfigDescription"),
                CloseButtonText = _localizationService.TranslateSource("ToolsPageViewModelErrorMessageConfigButtonOk")
            };
            await contentDialog.ShowAsync();
        }
        else
        {
            await _fileService.OpenDirectoryAsync(_configService.Config.AkiServerPath);
        }
    }

    private async Task OpenBepInExFolder()
    {
        if (string.IsNullOrEmpty(_configService.Config.SitEftInstallPath))
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
            string dirPath = Path.Combine(_configService.Config.SitEftInstallPath, "BepInEx", "plugins");
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
        string path = Path.Combine(_configService.Config.SitEftInstallPath, "BepInEx", "config");
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
        string logPath = EFTLogPath;
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
                _modService.ClearCache();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the process.
                _barNotificationService.ShowError(_localizationService.TranslateSource("ToolsPageViewModelErrorMessageTitle"), _localizationService.TranslateSource("ToolsPageViewModelUnhandledExceptionError", ex.Message));
            }
        }
    }

    private async Task GenerateDiagnosticReport()
    {
        (ContentDialogResult result, DiagnosticsOptions options) = await new SelectLogsDialog().ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        using (Stream diagnosticArchive = await _diagnosticService.GenerateDiagnosticReport(options))
        {
            string savePath;
            Window? mainWindow = (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow != null)
            {
                var pickedPath = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = _localizationService.TranslateSource("ToolsDiagnosticReportSaveFileTitle"),
                    SuggestedFileName = "diagnostics",
                    DefaultExtension = "zip"
                });
                if (pickedPath != null)
                {
                    savePath = pickedPath.Path.LocalPath;
                }
                else
                {
                    return;
                }
            }
            else
            {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }

            using (FileStream fs = File.OpenWrite(savePath))
            {
                await diagnosticArchive.CopyToAsync(fs);
            }
        }
    }
}
