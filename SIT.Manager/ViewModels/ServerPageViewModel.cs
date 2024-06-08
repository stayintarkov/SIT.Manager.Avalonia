using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models;
using SIT.Manager.Models.Config;
using SIT.Manager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

/// <summary>
/// ServerPageViewModel for handling SPT-AKI Server execution and console output.
/// </summary>
public partial class ServerPageViewModel : ObservableRecipient
{
    [GeneratedRegex("\\x1B(?:[@-Z\\\\-_]|\\[[0-?]*[ -/]*[@-~])")]
    private static partial Regex ConsoleTextRemoveANSIFilterRegex();

    private readonly IAkiServerService _akiServerService;
    private readonly IManagerConfigService _configService;
    private readonly IFileService _fileService;

    private readonly SolidColorBrush _cachedColorBrush = new(Color.FromRgb(255, 255, 255));
    private FontFamily _cachedFontFamily = FontFamily.Default;
    private AkiConfig _akiConfig => _configService.Config.AkiSettings;

    [ObservableProperty]
    private Symbol _startServerButtonSymbolIcon = Symbol.Play;

    [ObservableProperty]
    private string _startServerButtonTextBlock;

    public ObservableCollection<ConsoleText> ConsoleOutput { get; }

    public IAsyncRelayCommand EditServerConfigCommand { get; }
    public IAsyncRelayCommand ClearServerOutputCommand { get; }

    private readonly ILocalizationService _localizationService;

    public ServerPageViewModel(IAkiServerService akiServerService, ILocalizationService localizationService, IManagerConfigService configService, IFileService fileService)
    {
        _akiServerService = akiServerService;
        _configService = configService;
        _fileService = fileService;
        _localizationService = localizationService;

        StartServerButtonTextBlock = _localizationService.TranslateSource("ServerPageViewModelStartServer");
        EditServerConfigCommand = new AsyncRelayCommand(EditServerConfig);
        ClearServerOutputCommand = new AsyncRelayCommand(ClearServerOutput);
        //This is just to preallocate the space to avoid incremental allocations of the buckets
        ConsoleOutput = new ObservableCollection<ConsoleText>(new List<ConsoleText>(_akiServerService.ServerLineLimit));
    }

    private async Task ClearServerOutput()
    {
        ContentDialogResult clearServerOutputResponse = await new ContentDialog()
        {
            Title = _localizationService.TranslateSource("ServerPageViewModelClearServerOutputTitle"),
            Content = _localizationService.TranslateSource("ServerPageViewModelClearServerOutputDescription"),
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = _localizationService.TranslateSource("ServerPageViewModelClearServerOutputPrimary"),
            CloseButtonText = _localizationService.TranslateSource("ServerPageViewModelClearServerOutputSecondary")
        }.ShowAsync();
        if (clearServerOutputResponse == ContentDialogResult.Primary)
        {
            //TODO: Clear the cached output too
            ConsoleOutput.Clear();
        }
    }

    private void UpdateCachedFont()
    {
        string newFontFamilyName = _configService.Config.AkiSettings.ConsoleFontFamily;
        if (newFontFamilyName.Equals(_cachedFontFamily.Name)) return;
        
        FontFamily newFont = FontManager.Current.SystemFonts.FirstOrDefault(x => x.Name == newFontFamilyName, FontFamily.Parse("Bender"));
        _cachedFontFamily = newFont;

        //TODO: Test me!
        Interlocked.Exchange(ref _cachedFontFamily, newFont);

        /*lock (ConsoleOutput)
        {
            foreach (ConsoleText textEntry in ConsoleOutput)
            {
                textEntry.TextFont = _cachedFontFamily;
            }
        }*/
    }

    private void UpdateCachedColour()
    {
        Color newColor = _configService.Config.AkiSettings.ConsoleFontColor;
        if (newColor == _cachedColorBrush.Color) return;
        Dispatcher.UIThread.Post(() => _cachedColorBrush.Color = newColor);
    }

    private void OnAkiPropertyChanged(object? _, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_akiConfig.ConsoleFontFamily):
                UpdateCachedFont();
                break;
            case nameof(_akiConfig.ConsoleFontColor):
                UpdateCachedColour();
                break;
            default:
                return;
        }
    }

    private void UpdateConsoleWithCachedEntries()
    {
        lock(ConsoleOutput)
        {
            foreach (string entry in _akiServerService.GetCachedServerOutput())
            {
                AddConsole(entry);
            }   
        }
    }

    private void AddConsole(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (ConsoleOutput.Count >= _akiServerService.ServerLineLimit)
        {
            ConsoleOutput.RemoveAt(0);
        }
        
        text = ConsoleTextRemoveANSIFilterRegex().Replace(text, string.Empty);

        ConsoleText consoleTextEntry = new()
        {
            TextColor = _cachedColorBrush,
            TextFont = _cachedFontFamily,
            Message = text
        };

        ConsoleOutput.Add(consoleTextEntry);
    }

    private void AkiServer_OutputDataReceived(object? sender, DataReceivedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => AddConsole(e.Data ?? "\n"));
    }

    private void AkiServer_RunningStateChanged(object? _, RunningState runningState)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            string buttonTextKey = "ServerPageViewModel";
            string? consoleText = null;
            
            switch (runningState)
            {
                case RunningState.Starting:
                    {
                        consoleText = _localizationService.TranslateSource("ServerPageViewModelServerStarted");
                        buttonTextKey += "StartingServer";
                        break;
                    }
                case RunningState.Running:
                    {
                        buttonTextKey += "StopServer";
                        break;
                    }
                case RunningState.NotRunning:
                    {
                        consoleText = _localizationService.TranslateSource("ServerPageViewModelServerStopped");
                        buttonTextKey += "StartServer";
                        break;
                    }
                case RunningState.StoppedUnexpectedly:
                    {
                        consoleText = _localizationService.TranslateSource("ServerPageViewModelServerError");
                        buttonTextKey += "StartServer";
                        break;
                    }
                default:
                    {
                        buttonTextKey = "ToolsPageViewModelErrorMessageTitle";
                        break;
                    }
            }
            
            StartServerButtonTextBlock = _localizationService.TranslateSource(buttonTextKey);
            StartServerButtonSymbolIcon = runningState >= RunningState.Starting ? Symbol.Stop : Symbol.Play;
            if(consoleText != null)
                AddConsole(consoleText);
        });
    }

    private async Task EditServerConfig()
    {
        string serverPath = _akiConfig.AkiServerPath;
        if (string.IsNullOrEmpty(serverPath)) return;

        string serverConfigPath = Path.Combine(serverPath, "Aki_Data", "Server", "configs");
        await _fileService.OpenDirectoryAsync(serverConfigPath);
    }

    [RelayCommand]
    private void StartServer()
    {
        if (_akiServerService.State>= RunningState.Starting)
        {
            AddConsole(_localizationService.TranslateSource("ServerPageViewModelStoppingServerLog"));
            try
            {
                _akiServerService.Stop();
            }
            catch (Exception ex)
            {
                //TODO: Add logging here!
                AddConsole(ex.Message);
            }
        }
        else
        {
            if (_akiServerService.IsUnhandledInstanceRunning())
            {
                AddConsole(_localizationService.TranslateSource("ServerPageViewModelSPTAkiRunning"));
                return;
            }

            if (!File.Exists(_akiServerService.ExecutableFilePath))
            {
                AddConsole(_localizationService.TranslateSource("ServerPageViewModelSPTAkiNotFound"));
                return;
            }

            AddConsole(_localizationService.TranslateSource("ServerPageViewModelStartingServerLog"));
            try
            {
                _akiServerService.Start();
            }
            catch (Exception ex)
            {
                //TODO: Add logging here!
                AddConsole(ex.Message);
            }
        }
    }

    protected override void OnActivated()
    {
        if (_akiServerService.State != RunningState.NotRunning)
        {
            AkiServer_RunningStateChanged(null, _akiServerService.State);
        }

        _akiServerService.OutputDataReceived += AkiServer_OutputDataReceived;
        _akiServerService.RunningStateChanged += AkiServer_RunningStateChanged;
        _akiConfig.PropertyChanged += OnAkiPropertyChanged;
        UpdateConsoleWithCachedEntries();
    }

    protected override void OnDeactivated()
    {
        // We don't want these event firing when the page isn't currently active.
        _akiServerService.OutputDataReceived -= AkiServer_OutputDataReceived;
        _akiServerService.RunningStateChanged -= AkiServer_RunningStateChanged;
        _akiConfig.PropertyChanged -= OnAkiPropertyChanged;
    }
}
