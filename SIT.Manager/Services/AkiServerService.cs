using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SIT.Manager.Classes;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

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
        if (OutputDataReceived != null)
        {
            if (cachedServerOutput.Any())
            {
                cachedServerOutput.Clear();
            }
            OutputDataReceived?.Invoke(sender, e);
        }
        else
        {
            if (cachedServerOutput.Count > ServerLineLimit)
            {
                cachedServerOutput.RemoveAt(0);
            }

            if (!string.IsNullOrEmpty(e.Data))
            {
                cachedServerOutput.Add(e.Data);
            }
        }
    }

    public override void ClearCache()
    {
        string serverPath = _configService.Config.AkiServerPath;
        if (!string.IsNullOrEmpty(serverPath))
        {
            // Combine the serverPath with the additional subpath.
            string serverCachePath = Path.Combine(serverPath, "user", "cache");
            if (Directory.Exists(serverCachePath))
            {
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

        if (akiServerProcesses.Length > 0)
        {
            if (_process == null || _process.HasExited)
            {
                return true;
            }

            foreach (Process akiServerProcess in akiServerProcesses)
            {
                if (_process.Id != akiServerProcess.Id)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override void Start(string? arguments = null)
    {
        if (State == RunningState.Running || State == RunningState.Starting)
        {
            return;
        }

        bool cal = _configService.Config.CloseAfterLaunch;
        _process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = ExecutableFilePath,
                WorkingDirectory = ExecutableDirectory,
                UseShellExecute = false,
                StandardOutputEncoding = cal ? null : Encoding.UTF8,
                RedirectStandardOutput = !cal,
                CreateNoWindow = !cal
            },
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += AkiServer_OutputDataReceived;
        _process.Exited += new EventHandler((sender, e) =>
        {
            ExitedEvent(sender, e);
            IsStarted = false;
        });

        _process.Start();
        UpdateRunningState(RunningState.Starting);

        if (!cal)
        {
            _process.BeginOutputReadLine();
        }

        Task.Run(async () =>
        {
            Uri serverUri = new("http://127.0.0.1:6969");

            string httpConfigPath = Path.Combine(_configService.Config.AkiServerPath, "Aki_Data", "Server", "configs", "http.json");
            if (File.Exists(httpConfigPath))
            {
                JObject httpConfig = JObject.Parse(File.ReadAllText(httpConfigPath));
                if (httpConfig.TryGetValue("ip", out JToken IPToken) && httpConfig.TryGetValue("port", out JToken PortToken))
                {
                    string ipAddress = IPToken.ToString();
                    if (ipAddress == "0.0.0.0")
                    {
                        serverUri = new Uri($"{serverUri}:{PortToken}");
                    }
                    else
                    {
                        serverUri = new Uri($"http://{IPToken}:{PortToken}");
                    }
                }
            }
        });
    }
}
