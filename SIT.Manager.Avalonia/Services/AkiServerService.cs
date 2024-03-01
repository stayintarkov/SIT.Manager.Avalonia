using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Classes;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services
{
    public class AkiServerService(IBarNotificationService barNotificationService,
                                  IManagerConfigService configService,
                                  IServiceProvider serviceProvider) : ManagedProcess.ManagedProcess(barNotificationService, configService), IAkiServerService
    {
        private const string SERVER_EXE = "Aki.Server.exe";
        private const int SERVER_LINE_LIMIT = 10_000;

        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private readonly List<string> cachedServerOutput = [];

        protected override string EXECUTABLE_NAME => SERVER_EXE;
        public override string ExecutableDirectory => !string.IsNullOrEmpty(_configService.Config.AkiServerPath) ? _configService.Config.AkiServerPath : string.Empty;
        public bool IsStarted { get; private set; } = false;
        public int ServerLineLimit => SERVER_LINE_LIMIT;

        public event EventHandler<DataReceivedEventArgs>? OutputDataReceived;
        public event EventHandler? ServerStarted;

        private void AkiServer_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (OutputDataReceived != null) {
                if (cachedServerOutput.Any()) {
                    cachedServerOutput.Clear();
                }
                OutputDataReceived?.Invoke(sender, e);
            }
            else {
                if (cachedServerOutput.Count > ServerLineLimit) {
                    cachedServerOutput.RemoveAt(0);
                }

                if (!string.IsNullOrEmpty(e.Data)) {
                    cachedServerOutput.Add(e.Data);
                }
            }
        }

        public override void ClearCache()
        {
            string serverPath = _configService.Config.AkiServerPath;
            if (!string.IsNullOrEmpty(serverPath)) {
                // Combine the serverPath with the additional subpath.
                string serverCachePath = Path.Combine(serverPath, "user", "cache");
                if (Directory.Exists(serverCachePath)) {
                    Directory.Delete(serverCachePath, true);
                }
                Directory.CreateDirectory(serverCachePath);
            }
        }

        public string[] GetCachedServerOutput()
        {
            return [.. cachedServerOutput];
        }

        public bool IsUnhandledInstanceRunning()
        {
            Process[] akiServerProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(SERVER_EXE));

            if (akiServerProcesses.Length > 0) {
                if (_process == null || _process.HasExited) {
                    return true;
                }

                foreach (Process akiServerProcess in akiServerProcesses) {
                    if (_process.Id != akiServerProcess.Id) {
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Start(string? arguments = null)
        {
            if (State == RunningState.Running) {
                return;
            }

            bool cal = _configService.Config.CloseAfterLaunch;
            _process = new Process() {
                StartInfo = new ProcessStartInfo() {
                    FileName = ExecutableFilePath,
                    WorkingDirectory = ExecutableDirectory,
                    UseShellExecute = false,
                    StandardOutputEncoding = cal ? null : Encoding.UTF8,
                    RedirectStandardOutput = !cal,
                    CreateNoWindow = !cal
                },
                EnableRaisingEvents = true
            };
            if (OperatingSystem.IsLinux()) {
                _process.StartInfo.FileName = _configService.Config.WineRunner;
                _process.StartInfo.Arguments = $"\"{ExecutableFilePath}\"";

                string winePrefix = Path.GetFullPath(_configService.Config.WinePrefix);
                if (!Path.EndsInDirectorySeparator(winePrefix)) {
                    winePrefix = $"{winePrefix}{Path.DirectorySeparatorChar}";
                }
                _process.StartInfo.EnvironmentVariables.Add("WINEPREFIX", winePrefix);
            }
            else {
                _process.StartInfo.WorkingDirectory = ExecutableDirectory;
            }

            _process.OutputDataReceived += AkiServer_OutputDataReceived;
            _process.Exited += new EventHandler((sender, e) => {
                ExitedEvent(sender, e);
                IsStarted = false;
            });

            _process.Start();
            UpdateRunningState(RunningState.Starting);

            if (!cal) {
                _process.BeginOutputReadLine();
            }

            Task.Run(async () => {
                TarkovRequesting requesting = ActivatorUtilities.CreateInstance<TarkovRequesting>(_serviceProvider, new Uri("http://127.0.0.1:6969/"));
                int retryCounter = 0;
                while (retryCounter < 6) {
                    using (CancellationTokenSource cts = new()) {
                        DateTime abortTime = DateTime.Now + TimeSpan.FromSeconds(10);
                        cts.CancelAfter(abortTime - DateTime.Now);

                        bool pingReponse;
                        try {
                            pingReponse = await requesting.PingServer(cts.Token);
                        }
                        catch (HttpRequestException) {
                            pingReponse = false;
                        }

                        if (pingReponse && _process?.HasExited == false) {
                            IsStarted = true;
                            ServerStarted?.Invoke(this, new EventArgs());
                            UpdateRunningState(RunningState.Running);
                            return;
                        }
                        else if (_process?.HasExited == true) {
                            UpdateRunningState(RunningState.NotRunning);
                            return;
                        }
                    }

                    await Task.Delay(5 * 1000);
                    retryCounter++;
                }
            });
        }
    }
}
