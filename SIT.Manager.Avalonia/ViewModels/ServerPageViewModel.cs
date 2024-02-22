using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static SIT.Manager.Avalonia.Services.AkiServerService;

namespace SIT.Manager.Avalonia.ViewModels
{
    /// <summary>
    /// ServerPageViewModel for handling SPT-AKI Server execution and console output.
    /// </summary>
    public partial class ServerPageViewModel : ViewModelBase
    {
        private const int CONSOLE_LINE_LIMIT = 10_000;

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

        public ServerPageViewModel(IAkiServerService akiServerService, IManagerConfigService configService, IFileService fileService) {
            _akiServerService = akiServerService;
            _configService = configService;
            _fileService = fileService;

            EditServerConfigCommand = new AsyncRelayCommand(EditServerConfig);

            _akiServerService.OutputDataReceived += AkiServer_OutputDataReceived;
            _akiServerService.RunningStateChanged += AkiServer_RunningStateChanged;
            
            UpdateCachedServerProperties(null, _configService.Config);
            _configService.ConfigChanged += UpdateCachedServerProperties;
            if(_akiServerService.State != RunningState.NotRunning)
                AkiServer_RunningStateChanged(null, _akiServerService.State);
        }

        private void UpdateCachedServerProperties(object? sender, ManagerConfig newConfig)
        {
            
            FontFamily newFont = FontManager.Current.SystemFonts.FirstOrDefault(x => x.Name == newConfig.ConsoleFontFamily, FontFamily.Parse("Bender"));
            if(!newFont.Name.Equals(cachedFontFamily.Name))
            {
                cachedFontFamily = newFont;
                foreach (ConsoleText textEntry in ConsoleOutput)
                {
                    textEntry.TextFont = cachedFontFamily;
                }
            }

            cachedColorBrush.Color = newConfig.ConsoleFontColor;
        }

        private void AddConsole(string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            if (ConsoleOutput.Count > CONSOLE_LINE_LIMIT) {
                ConsoleOutput.RemoveAt(0);
            }

            //[32m, [2J, [0;0f,
            text = ConsoleTextRemoveANSIFilterRegex().Replace(text, "");

            ConsoleText consoleTextEntry = new() {
                TextColor = cachedColorBrush,
                TextFont = cachedFontFamily,
                Message = text
            };

            ConsoleOutput.Add(consoleTextEntry);
        }

        private void AkiServer_OutputDataReceived(object? sender, DataReceivedEventArgs e) {
            Dispatcher.UIThread.Post(() => AddConsole(e.Data ?? "\n"));
        }

        private void AkiServer_RunningStateChanged(object? sender, RunningState runningState) {
            Dispatcher.UIThread.Post(() => {
                switch (runningState) {
                    case RunningState.Running: {
                            AddConsole("Server started!");
                            StartServerButtonSymbolIcon = Symbol.Stop;
                            StartServerButtonTextBlock = "Stop Server";
                            break;
                        }
                    case RunningState.NotRunning: {
                            AddConsole("Server stopped!");
                            StartServerButtonSymbolIcon = Symbol.Play;
                            StartServerButtonTextBlock = "Start Server";
                            break;
                        }
                    case RunningState.StoppedUnexpectedly: {
                            AddConsole("Server stopped unexpectedly! Check console for errors.");
                            StartServerButtonSymbolIcon = Symbol.Play;
                            StartServerButtonTextBlock = "Start Server";
                            break;
                        }
                }
            });
        }

        private async Task EditServerConfig() {
            string serverPath = _configService.Config.AkiServerPath;
            if (string.IsNullOrEmpty(serverPath)) {
                return;
            }

            string serverConfigPath = Path.Combine(serverPath, "Aki_Data", "Server", "configs");
            await _fileService.OpenDirectoryAsync(serverConfigPath);
        }

        [RelayCommand]
        private void StartServer() {
            if (_akiServerService.State != RunningState.Running) {
                if (_akiServerService.IsUnhandledInstanceRunning()) {
                    AddConsole("SPT-AKI is currently running. Please close any running instance of SPT-AKI.");
                    return;
                }

                if (!File.Exists(_akiServerService.ExecutableFilePath)) {
                    AddConsole("SPT-AKI not found. Please configure the SPT-AKI path in Settings tab before starting the server.");
                    return;
                }

                AddConsole("Starting server...");
                try {
                    _akiServerService.Start();
                }
                catch (Exception ex) {
                    AddConsole(ex.Message);
                }
            }
            else {
                AddConsole("Stopping server...");
                try {
                    _akiServerService.Stop();
                }
                catch (Exception ex) {
                    AddConsole(ex.Message);
                }
            }
        }
    }
}
