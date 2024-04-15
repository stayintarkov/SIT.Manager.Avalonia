using Microsoft.Win32;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Services;
public class DiagnosticService : IDiagnosticService
{
    private readonly IManagerConfigService _configService;
    private readonly HttpClient _httpClient;
    private readonly Lazy<Task<string>> _externalIP;
    public static string EFTLogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow", "Battlestate Games", "EscapeFromTarkov", "Player.log");
    public DiagnosticService(IManagerConfigService configService, HttpClient client)
    {
        _configService = configService;
        _httpClient = client;

        _externalIP = new Lazy<Task<string>>(async () =>
        {
            HttpResponseMessage resp = await _httpClient.GetAsync("https://ipv4.icanhazip.com/");
            return (await resp.Content.ReadAsStringAsync()).Trim();
        });
    }

    public async Task<string> CleanseLogFile(string fileData)
    {
        return fileData.Replace(await _externalIP.Value, "[REDACTED]");
    }

    public async Task<string> GetLogFile(string logFilePath)
    {
        string logFileName = Path.GetFileName(logFilePath);
        if (File.Exists(logFilePath))
        {
            try
            {
                string fileData;
                using (FileStream fs = File.Open(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using(StreamReader  sr = new(fs))
                    {
                        fileData = await CleanseLogFile(await sr.ReadToEndAsync());
                    }
                }
                return fileData;
            }
            catch(IOException ex)
            {
                return $"Problem reading {logFileName}\n{ex}";
            }
        }
        else
        {
            return $"{logFileName} didn't exist at path {logFilePath}";
        }
    }

    //TODO: Clean this up a little. It has a bunch of duplication
    public async Task<Stream> GenerateDiagnosticReport(DiagnosticsOptions options)
    {
        List<Tuple<string, string>> diagnosticLogs = new(4);

        if (options.IncludeDiagnosticLog)
            diagnosticLogs.Add(new("diagnostics.log", GenerateDiagnosticLog()));

        if (options.IncludeClientLog)
        {
            string eftLogPath = EFTLogPath;
            string eftLogData = await GetLogFile(eftLogPath);
            diagnosticLogs.Add(new(Path.GetFileName(eftLogPath), eftLogData));
        }

        if (!string.IsNullOrEmpty(_configService.Config.AkiServerPath))
        {
            if (options.IncludeServerLog)
            {
                DirectoryInfo serverLogDirectory = new(Path.Combine(_configService.Config.AkiServerPath, "user", "logs"));
                if (serverLogDirectory.Exists)
                {
                    IEnumerable<FileInfo> files = serverLogDirectory.GetFiles("*.log");
                    files = files.OrderBy(x => x.LastWriteTime);
                    if (files.Any())
                    {
                        string serverLogFile = files.First().FullName;
                        string serverLogData = await GetLogFile(serverLogFile);
                        diagnosticLogs.Add(new(Path.GetFileName(serverLogFile), serverLogData));
                    }
                }
            }

            if (options.IncludeHttpJson)
            {
                string httpJsonPath = Path.Combine(_configService.Config.AkiServerPath, "Aki_Data", "Server", "configs", "http.json");
                string httpJsonData = await GetLogFile(httpJsonPath);
                diagnosticLogs.Add(new(Path.GetFileName(httpJsonPath), httpJsonData));
            }
        }

        MemoryStream ms = new();
        using (ZipArchive zipArchive = new(ms, ZipArchiveMode.Create, true))
        {
            foreach (Tuple<string, string> entryData in diagnosticLogs)
            {
                var entry = zipArchive.CreateEntry(entryData.Item1);
                using (Stream entryStream = entry.Open())
                using (StreamWriter sw = new(entryStream))
                {
                    await sw.WriteAsync(entryData.Item2);
                }
            }
        }
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    private static string GenerateDiagnosticLog()
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

        sb.AppendLine("#-- Registry Information --#");
        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov"))
        {
            if (key != null)
            {
                foreach (string valueName in key.GetValueNames())
                {
                    sb.Append($"{valueName}: {key.GetValue(valueName)}");
                }
            }
        }

        //TODO: Add system hardware reporting
        return sb.ToString();
    }
}
