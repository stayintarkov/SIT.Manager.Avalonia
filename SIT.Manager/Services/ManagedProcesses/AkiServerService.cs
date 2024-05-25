using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Services.ManagedProcesses;

public class AkiServerService(
    IBarNotificationService barNotificationService,
    IManagerConfigService configService,
    ILogger<AkiServerService> logger,
    IAkiServerRequestingService requestingService)
    : ManagedProcess(barNotificationService, configService), IAkiServerService
{
    private const string SERVER_EXE = "Aki.Server.exe";
    private const int SERVER_LINE_LIMIT = 10_000;

    private readonly List<string> _cachedServerOutput = [];
    private AkiServer? _selfServer;

    protected override string EXECUTABLE_NAME => SERVER_EXE;

    public override string ExecutableDirectory => !string.IsNullOrEmpty(ConfigService.Config.AkiServerPath)
        ? ConfigService.Config.AkiServerPath
        : string.Empty;

    public bool IsStarted { get; private set; }
    public int ServerLineLimit => SERVER_LINE_LIMIT;

    public event EventHandler<DataReceivedEventArgs>? OutputDataReceived;
    public event EventHandler? ServerStarted;

    public override void ClearCache()
    {
        string serverPath = ConfigService.Config.AkiServerPath;
        if (string.IsNullOrEmpty(serverPath))
        {
            return;
        }

        // Combine the serverPath with the additional subpath.
        string serverCachePath = Path.Combine(serverPath, "user", "cache");
        if (Directory.Exists(serverCachePath))
        {
            Directory.Delete(serverCachePath, true);
        }

        Directory.CreateDirectory(serverCachePath);
    }

    public string[] GetCachedServerOutput()
        => [.. _cachedServerOutput];

    public bool IsUnhandledInstanceRunning()
    {
        Process[] akiServerProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(SERVER_EXE));

        if (akiServerProcesses.Length <= 0) return false;
        if (ProcessToManage == null || ProcessToManage.HasExited) return true;
        return akiServerProcesses.Any(akiServerProcess => ProcessToManage.Id != akiServerProcess.Id);
    }

    public override void Start(string? arguments)
    {
        if (State is RunningState.Running or RunningState.Starting)
        {
            return;
        }

        bool cal = ConfigService.Config.CloseAfterLaunch;
        ProcessToManage = new Process
        {
            StartInfo = new ProcessStartInfo
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

        ProcessToManage.OutputDataReceived += AkiServer_OutputDataReceived;
        ProcessToManage.Exited += (sender, e) =>
        {
            ExitedEvent(sender, e);
            IsStarted = false;
        };

        ProcessToManage.Start();
        UpdateRunningState(RunningState.Starting);

        if (!cal)
        {
            ProcessToManage.BeginOutputReadLine();
        }

        Uri serverUri = new("http://127.0.0.1:6969");

        string httpConfigPath = Path.Combine(ConfigService.Config.AkiServerPath, "Aki_Data", "Server", "configs",
            "http.json");
        //TODO: Refactor this
        if (File.Exists(httpConfigPath))
        {
            JObject httpConfig = JObject.Parse(File.ReadAllText(httpConfigPath));
            if (httpConfig.TryGetValue("ip", out JToken IPToken) &&
                httpConfig.TryGetValue("port", out JToken PortToken))
            {
                string ipAddress = IPToken.ToString();
                //TODO: Come back and refactor this
                string addressToUse = $"http://{(ipAddress == "0.0.0.0" ? serverUri.Host : IPToken)}:{PortToken}";
                serverUri = new Uri(addressToUse);
            }
        }

        _selfServer = new AkiServer(serverUri);
        Task.Run(ListenForHeartbeat);
    }

    private void AkiServer_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (OutputDataReceived != null)
        {
            _cachedServerOutput.Clear();
            OutputDataReceived?.Invoke(sender, e);
        }
        else
        {
            //TODO: Replace this with a FILO object like a queue
            if (_cachedServerOutput.Count > ServerLineLimit)
            {
                _cachedServerOutput.RemoveAt(0);
            }

            if (!string.IsNullOrEmpty(e.Data))
            {
                _cachedServerOutput.Add(e.Data);
            }
        }
    }

    private async Task ListenForHeartbeat()
    {
        try
        {
            if (_selfServer == null) return;

            int ping = await requestingService.GetPingAsync(_selfServer);
            if (ping != -1)
            {
                RunningState updatedState;
                if (ProcessToManage?.HasExited == true)
                {
                    updatedState = RunningState.NotRunning;
                }
                else
                {
                    //TODO: Refactor this
                    IsStarted = true;
                    ServerStarted?.Invoke(this, EventArgs.Empty);
                    updatedState = RunningState.Running;
                }
                
                UpdateRunningState(updatedState);

                return;
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Exception throw while attempting to ping local server.");
        }

        ProcessToManage?.Kill();
    }
}
