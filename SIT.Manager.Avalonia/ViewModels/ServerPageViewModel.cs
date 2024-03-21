using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels;

/// <summary>
/// ServerPageViewModel for handling SPT-AKI Server execution and console output.
/// </summary>
public partial class ServerPageViewModel : ObservableRecipient
{
    [GeneratedRegex("\\x1B(?:[@-Z\\\\-_]|\\[[0-?]*[ -/]*[@-~])")]
    internal static partial Regex ConsoleTextRemoveANSIFilterRegex();

    private readonly IAkiServerService _akiServerService;
    private readonly IManagerConfigService _configService;
    private FontFamily cachedFontFamily = FontFamily.Parse("Bender");
    private SolidColorBrush cachedColorBrush = new(Color.FromRgb(255, 255, 255));
    private readonly IFileService _fileService;

    [ObservableProperty]
    private Symbol _startServerButtonSymbolIcon = Symbol.Play;

    [ObservableProperty]
    private string _startServerButtonTextBlock = "Start Server";

    public ObservableCollection<ConsoleText> ConsoleOutput { get; } = [];

    public IAsyncRelayCommand EditServerConfigCommand { get; }
    private readonly ILocalizationService _localizationService;

    public ServerPageViewModel(IAkiServerService akiServerService, ILocalizationService localizationService, IManagerConfigService configService, IFileService fileService)
    {
        _akiServerService = akiServerService;
        _configService = configService;
        _fileService = fileService;
        _localizationService = localizationService;

        StartServerButtonTextBlock = _localizationService.TranslateSource("ServerPageViewModelStartServer");
        EditServerConfigCommand = new AsyncRelayCommand(EditServerConfig);

        configService.ConfigChanged += (o, e) =>
        {
            StartServerButtonTextBlock = _localizationService.TranslateSource("ServerPageViewModelStartServer");
        };
    }

    private void UpdateCachedServerProperties(object? sender, ManagerConfig newConfig)
    {
        FontFamily newFont = FontManager.Current.SystemFonts.FirstOrDefault(x => x.Name == newConfig.ConsoleFontFamily, FontFamily.Parse("Bender"));
        if (!newFont.Name.Equals(cachedFontFamily.Name))
        {
            cachedFontFamily = newFont;
            foreach (ConsoleText textEntry in ConsoleOutput)
            {
                textEntry.TextFont = cachedFontFamily;
            }
        }

        cachedColorBrush.Color = newConfig.ConsoleFontColor;
    }

    private void UpdateConsoleWithCachedEntries()
    {
        foreach (string entry in _akiServerService.GetCachedServerOutput())
        {
            AddConsole(entry);
        }
    }

    private void AddConsole(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (ConsoleOutput.Count > _akiServerService.ServerLineLimit)
        {
            ConsoleOutput.RemoveAt(0);
        }

        //[32m, [2J, [0;0f,
        text = ConsoleTextRemoveANSIFilterRegex().Replace(text, "");

        ConsoleText consoleTextEntry = new()
        {
            TextColor = cachedColorBrush,
            TextFont = cachedFontFamily,
            Message = text
        };

        ConsoleOutput.Add(consoleTextEntry);
    }

    private void AkiServer_OutputDataReceived(object? sender, DataReceivedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => AddConsole(e.Data ?? "\n"));
    }

    private void AkiServer_RunningStateChanged(object? sender, RunningState runningState)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            switch (runningState)
            {
                case RunningState.Starting:
                    {
                        AddConsole(_localizationService.TranslateSource("ServerPageViewModelServerStarted"));
                        StartServerButtonSymbolIcon = Symbol.Stop;
                        StartServerButtonTextBlock = _localizationService.TranslateSource("ServerPageViewModelStartingServer");
                        break;
                    }
                case RunningState.Running:
                    {
                        AddConsole(_localizationService.TranslateSource("ServerPageViewModelServerStarted"));
                        StartServerButtonSymbolIcon = Symbol.Stop;
                        StartServerButtonTextBlock = _localizationService.TranslateSource("ServerPageViewModelStopServer");
                        break;
                    }
                case RunningState.NotRunning:
                    {
                        AddConsole(_localizationService.TranslateSource("ServerPageViewModelServerStopped"));
                        StartServerButtonSymbolIcon = Symbol.Play;
                        StartServerButtonTextBlock = _localizationService.TranslateSource("ServerPageViewModelStartServer");
                        break;
                    }
                case RunningState.StoppedUnexpectedly:
                    {
                        AddConsole(_localizationService.TranslateSource("ServerPageViewModelServerError"));
                        StartServerButtonSymbolIcon = Symbol.Play;
                        StartServerButtonTextBlock = _localizationService.TranslateSource("ServerPageViewModelStartServer");
                        break;
                    }
            }
        });
    }

    private async Task EditServerConfig()
    {
        string serverPath = _configService.Config.AkiServerPath;
        if (string.IsNullOrEmpty(serverPath))
        {
            return;
        }

        string serverConfigPath = Path.Combine(serverPath, "Aki_Data", "Server", "configs");
        await _fileService.OpenDirectoryAsync(serverConfigPath);
    }

    [RelayCommand]
    private void StartServer()
    {
        if (_akiServerService.State == RunningState.Starting)
        {
            return;
        }
        else if (_akiServerService.State != RunningState.Running)
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
                AddConsole(ex.Message);
            }
        }
        else
        {
            AddConsole(_localizationService.TranslateSource("ServerPageViewModelStoppingServerLog"));
            try
            {
                _akiServerService.Stop();
            }
            catch (Exception ex)
            {
                AddConsole(ex.Message);
            }
        }
    }

    protected override void OnActivated()
    {
        UpdateCachedServerProperties(null, _configService.Config);
        _configService.ConfigChanged += UpdateCachedServerProperties;
        if (_akiServerService.State != RunningState.NotRunning)
        {
            AkiServer_RunningStateChanged(null, _akiServerService.State);
        }

        _akiServerService.OutputDataReceived += AkiServer_OutputDataReceived;
        _akiServerService.RunningStateChanged += AkiServer_RunningStateChanged;

        UpdateConsoleWithCachedEntries();
    }

    protected override void OnDeactivated()
    {
        _akiServerService.OutputDataReceived -= AkiServer_OutputDataReceived;
        _akiServerService.RunningStateChanged -= AkiServer_RunningStateChanged;
    }
}
