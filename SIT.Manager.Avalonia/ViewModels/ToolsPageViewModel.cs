using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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
using SIT.Manager.Avalonia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly HttpClient _httpClient;
    private static string EFTLogPath
    {
        get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow", "Battlestate Games", "EscapeFromTarkov", "Player.log");
    }

    public IAsyncRelayCommand OpenEFTFolderCommand { get; }

    public IAsyncRelayCommand OpenBepInExFolderCommand { get; }

    public IAsyncRelayCommand OpenSITConfigCommand { get; }

    public IAsyncRelayCommand OpenEFTLogCommand { get; }

    public IAsyncRelayCommand ClearCacheCommand { get; }

    public IAsyncRelayCommand GenerateDiagnosticReportCommand { get; }

    public ToolsPageViewModel(IAkiServerService akiServerService,
                              IBarNotificationService barNotificationService,
                              IManagerConfigService configService,
                              IFileService fileService,
                              ILocalizationService localizationService,
                              ITarkovClientService tarkovClientService,
                              HttpClient httpClient)
    {
        _akiServerService = akiServerService;
        _barNotificationService = barNotificationService;
        _configService = configService;
        _fileService = fileService;
        _tarkovClientService = tarkovClientService;
        _localizationService = localizationService;
        _httpClient = httpClient;

        OpenEFTFolderCommand = new AsyncRelayCommand(OpenEFTFolder);
        OpenBepInExFolderCommand = new AsyncRelayCommand(OpenBepInExFolder);
        OpenSITConfigCommand = new AsyncRelayCommand(OpenSITConfig);
        OpenEFTLogCommand = new AsyncRelayCommand(OpenEFTLog);
        ClearCacheCommand = new AsyncRelayCommand(ClearCache);
        GenerateDiagnosticReportCommand = new AsyncRelayCommand(GenerateDiagnosticReport);
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
        var results = await new SelectLogsDialog().ShowAsync();
        List<Tuple<string, string>> DiagnosticData = new(4);

        if (results.IncludeClientLog)
        {
            string eftLogPath = EFTLogPath;
            DiagnosticData.Add(new(Path.GetFileName(eftLogPath), await File.ReadAllTextAsync(eftLogPath)));
        }

        if (results.IncludeServerLog && !string.IsNullOrEmpty(_configService.Config.AkiServerPath))
        {
            DirectoryInfo serverLogDirectory = new(Path.Combine(_configService.Config.AkiServerPath, "user", "logs"));
            if (serverLogDirectory.Exists)
            {
                IEnumerable<FileInfo> files = serverLogDirectory.GetFiles("*.log");
                files = files.OrderBy(x => x.LastWriteTime);
                if (files.Any())
                {
                    string serverLogFile = files.First().FullName;
                    DiagnosticData.Add(new(Path.GetFileName(serverLogFile), await File.ReadAllTextAsync(serverLogFile)));
                }
            }
        }

        if (results.IncludeDiagnosticLog)
        {
            //TODO: Add more diagnostics if needed
            StringBuilder sb = new("#--- DIAGNOSTICS LOG ---#\n\n");

            //Get all networks adaptors local address if they're online
            sb.AppendLine("#-- Network Information: --#\n");
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            sb.AppendLine($"Network Interface: {networkInterface.Name}");
                            sb.AppendLine($"Interface Type: {networkInterface.NetworkInterfaceType.ToString()}");
                            sb.AppendLine($"Address: {ip.Address}\n");
                        }
                    }
                }
            }

            //TODO: Add system hardware information

            DiagnosticData.Add(new("Diagnostics.log", sb.ToString()));
        }

        if (results.IncludeHttpJson)
        {
            FileInfo httpJsonFile = new(Path.Combine(_configService.Config.AkiServerPath, "Aki_Data", "Server", "configs", "http.json"));
            if (httpJsonFile.Exists)
                DiagnosticData.Add(new(httpJsonFile.Name, await File.ReadAllTextAsync(httpJsonFile.FullName)));
        }

        //I hate doing it this way but at least icanhazip is owned by cloudlfare
        HttpResponseMessage resp = await _httpClient.GetAsync("https://ipv4.icanhazip.com/");
        string externalAddress = await resp.Content.ReadAsStringAsync();

        List<Tuple<string, string>> CleanLogFiles = new(DiagnosticData.Count);
        foreach (Tuple<string, string> diagnosticFile in DiagnosticData)
        {
            CleanLogFiles.Add(new(diagnosticFile.Item1, diagnosticFile.Item2.Replace(externalAddress.Trim(), "REACTED")));
        }

        Stream? fileStream;
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
                fileStream = await pickedPath.OpenWriteAsync();
            }
            else
            {
                return;
            }
        }
        else
        {
            fileStream = File.OpenWrite(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        }

        using (ZipArchive zipArchive = new(fileStream, ZipArchiveMode.Create, true))
        {
            foreach (Tuple<string, string> entryData in CleanLogFiles)
            {
                var entry = zipArchive.CreateEntry(entryData.Item1);
                using (Stream entryStream = entry.Open())
                using (StreamWriter sw = new(entryStream))
                {
                    await sw.WriteAsync(entryData.Item2);
                    await sw.FlushAsync();
                }
            }
        }
        await fileStream.DisposeAsync();
    }
}
