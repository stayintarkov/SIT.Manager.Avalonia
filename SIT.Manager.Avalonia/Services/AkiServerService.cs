using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Classes;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
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

        private readonly IServiceProvider _serviceProvider = serviceProvider;

        protected override string EXECUTABLE_NAME => SERVER_EXE;
        public override string ExecutableDirectory => !string.IsNullOrEmpty(_configService.Config.AkiServerPath) ? _configService.Config.AkiServerPath : string.Empty;
        public event EventHandler<DataReceivedEventArgs>? OutputDataReceived;
        public event EventHandler? ServerStarted;
        public bool IsStarted { get; private set; } = false;

        public override void ClearCache() {
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

        public bool IsUnhandledInstanceRunning() {
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

        public override void Start(string? arguments = null) {
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

            _process.OutputDataReceived += new DataReceivedEventHandler((sender, e) => OutputDataReceived?.Invoke(sender, e));
            DataReceivedEventHandler? startedEventHandler = null;
            startedEventHandler = new DataReceivedEventHandler((sender, e) => {
                if (ServerPageViewModel.ConsoleTextRemoveANSIFilterRegex()
                .Replace(e.Data ?? string.Empty, "")
                .Equals("Server is running, do not close while playing SPT, Happy playing!!", StringComparison.InvariantCultureIgnoreCase)) {
                    IsStarted = true;
                    ServerStarted?.Invoke(sender, e);
                    _process.OutputDataReceived -= startedEventHandler;
                }
            });
            _process.OutputDataReceived += startedEventHandler;
            _process.Exited += new EventHandler((sender, e) => {
                ExitedEvent(sender, e);
                IsStarted = false;
            });

            _process.Start();
            if (cal) {
                _ = Task.Run(async () => {
                    TarkovRequesting requesting = ActivatorUtilities.CreateInstance<TarkovRequesting>(_serviceProvider, new Uri("http://127.0.0.1:6969/"));
                    int retryCounter = 0;
                    while (retryCounter < 60) {
                        using (CancellationTokenSource cts = new()) {
                            DateTime abortTime = DateTime.Now + TimeSpan.FromSeconds(2);
                            cts.CancelAfter(abortTime - DateTime.Now);

                            bool pingReponse = await requesting.PingServer(cts.Token);
                            if (pingReponse && _process?.HasExited == false) {
                                IsStarted = true;
                                ServerStarted?.Invoke(this, new EventArgs());
                                var queryResponse = requesting.QueryServer();
                                return;
                            }
                        }
                        await Task.Delay(1000);
                        retryCounter++;
                    }

                    // TODO if we make it here log an error or something and notify user somehow.
                });
            }
            else
                _process.BeginOutputReadLine();

            UpdateRunningState(RunningState.Running);
        }
    }
}
