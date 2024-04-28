using Microsoft.Win32;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIT.Manager.Services;
public partial class DiagnosticService : IDiagnosticService
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

    [GeneratedRegex(@"(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})([^.0-9])")]
    private static partial Regex ipv4Regex();

    public async Task<string> CleanseLogFile(string fileData, bool bleachIt)
    {
        var data = fileData.Replace(await _externalIP.Value, "xx.xx.xx.xx");

        if (bleachIt)
        {
            // cf. RFC1918 Address Allocation for Private Internets
            data = ipv4Regex().Replace(data, (match) =>
            {
                var g1 = int.Parse(match.Groups[1].Value);
                var g2 = int.Parse(match.Groups[2].Value);
                var g3 = int.Parse(match.Groups[3].Value);
                var g4 = int.Parse(match.Groups[4].Value);
                var is172Private = g1 == 172 && g2 >= 16 && g2 <= 31;
                var isInvalidIP = g1 == 0 || g4 == 0;
                var isLocalhost = g1 == 127 && g2 == 0 && g3 == 0 && g4 == 1;
                if (g1 == 192 && g2 == 168 || g1 == 10 || is172Private || isLocalhost || isInvalidIP)
                {
                    return match.Value;
                }

                var g5 = match.Groups[5];
                return "xx.xx.xx.xx" + g5.Value;
            });
        }

        return data;
    }

    public async Task<string> GetLogFile(string logFilePath, bool bleachIt = false)
    {
        string logFileName = Path.GetFileName(logFilePath);
        if (File.Exists(logFilePath))
        {
            try
            {
                string fileData;
                using (FileStream fs = File.Open(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new(fs))
                    {
                        fileData = await CleanseLogFile(await sr.ReadToEndAsync(), bleachIt);
                    }
                }
                return fileData;
            }
            catch (IOException ex)
            {
                return $"Problem reading {logFileName}\n{ex}";
            }
        }
        else
        {
            return $"{logFileName} didn't exist at path {logFilePath}";
        }
    }

    // TODO: Clean this up a little. It has a bunch of duplication
    public async Task<Stream> GenerateDiagnosticReport(DiagnosticsOptions options)
    {
        List<Tuple<string, string>> diagnosticLogs = new(4);

        if (options.IncludeDiagnosticLog)
            diagnosticLogs.Add(new("diagnostics.log", GenerateDiagnosticLog()));

        if (options.IncludeClientLog)
        {
            string eftLogPath = EFTLogPath;
            string eftLogData = await GetLogFile(eftLogPath, bleachIt: true);
            diagnosticLogs.Add(new(Path.GetFileName(eftLogPath), eftLogData));
        }

        if(options.IncludeManagerLog)
        {
            string managerLogPath = "Logs";
            foreach(string logFile in Directory.GetFiles(managerLogPath))
            {
                string logFileData = await GetLogFile(logFile, bleachIt: true);
                diagnosticLogs.Add(new(Path.GetFileName(logFile), logFileData));
            }
        }

        if (options.IncludeManagerCrash)
        {
            string crashLogPath = "crash.log";
            if (File.Exists(crashLogPath))
            {
                string crashFileData = await GetLogFile(crashLogPath, bleachIt: true);
                diagnosticLogs.Add(new(Path.GetFileName(crashLogPath), crashFileData));
            }
        }

        if (!string.IsNullOrEmpty(_configService.Config.AkiServerPath))
        {
            if (options.IncludeServerLog)
            {
                DirectoryInfo serverLogDirectory = new(Path.Combine(_configService.Config.AkiServerPath, "user", "logs"));
                if (serverLogDirectory.Exists)
                {
                    IEnumerable<FileInfo> files = serverLogDirectory.GetFiles("*.log");
                    files = files.OrderByDescending(x => x.LastWriteTime);
                    if (files.Any())
                    {
                        string serverLogFile = files.First().FullName;
                        string serverLogData = await GetLogFile(serverLogFile, bleachIt: true);
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

    private string GenerateDiagnosticLog()
    {
        //TODO: Add more diagnostics if needed
        StringBuilder sb = new("#--- DIAGNOSTICS LOG ---#\n\n");

        //Versioning information
        sb.AppendLine("#-- Versions --#");
        sb.AppendLine($"SIT: {_configService.Config.SitVersion}");
        sb.AppendLine($"EFT: {_configService.Config.SitTarkovVersion}");
        sb.AppendLine($"AKI: {_configService.Config.SptAkiVersion}");
        sb.AppendLine();

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
        sb.AppendLine();

        sb.AppendLine("#-- Registry Information --#");
        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov"))
        {
            if (key != null)
            {
                foreach (string valueName in key.GetValueNames())
                {
                    sb.AppendLine($"{valueName}: {key.GetValue(valueName)}");
                }
            }
        }

        //TODO: Add system hardware reporting
        return sb.ToString();
    }
}
