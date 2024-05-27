using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services.ManagedProcesses;

public class AkiServerService(IBarNotificationService barNotificationService,
                              IManagerConfigService configService,
                              ILogger<AkiServerService> logger,
                              IAkiServerRequestingService requestingService) : ManagedProcess(barNotificationService, configService), IAkiServerService
{
    private const string SERVER_EXE = "Aki.Server.exe";
    private const int SERVER_LINE_LIMIT = 10_000;

    private readonly ILogger<AkiServerService> _logger = logger;
    private readonly IAkiServerRequestingService _requestingService = requestingService;
    private AkiConfig _akiConfig => _configService.Config.AkiSettings;

    private readonly List<string> cachedServerOutput = [];
    private AkiServer? _selfServer;

    protected override string EXECUTABLE_NAME => SERVER_EXE;
    public override string ExecutableDirectory =>
        !string.IsNullOrEmpty(_akiConfig.AkiServerPath) ? _akiConfig.AkiServerPath : string.Empty;
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
        string serverPath = _akiConfig.AkiServerPath;
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

        bool cal = _configService.Config.LauncherSettings.CloseAfterLaunch;
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

        Uri serverUri = new("http://127.0.0.1:6969");

        string httpConfigPath = Path.Combine(_akiConfig.AkiServerPath, "Aki_Data", "Server", "configs", "http.json");
        if (File.Exists(httpConfigPath))
        {
            JObject httpConfig = JObject.Parse(File.ReadAllText(httpConfigPath));
            if (httpConfig.TryGetValue("ip", out JToken IPToken) && httpConfig.TryGetValue("port", out JToken PortToken))
            {
                string ipAddress = IPToken.ToString();
                string addressToUse = $"http://{(ipAddress == "0.0.0.0" ? serverUri.Host : IPToken)}:{PortToken}";
                serverUri = new(addressToUse);
            }
        }

        _selfServer = new(serverUri);

        Task.Run(ListenForHeartbeat);
    }

    private async Task ListenForHeartbeat()
    {
        try
        {
            if (_selfServer == null)
                return;

            int ping = await _requestingService.GetPingAsync(_selfServer);
            if(ping != -1)
            {
                if(_process?.HasExited == true)
                {
                    UpdateRunningState(RunningState.NotRunning);
                }
                else
                {
                    //TODO: Refactor this
                    IsStarted = true;
                    ServerStarted?.Invoke(this, new EventArgs());
                    UpdateRunningState(RunningState.Running);
                }
                return;
            }
        }
        catch(HttpRequestException ex)
        {
            _logger.LogError(ex, "Exception throw while attempting to ping local server.");
        }

        _process?.Kill();
    }
}
