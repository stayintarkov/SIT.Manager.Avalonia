using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PeNet;
using PeNet.Header.Resource;
using SIT.Manager.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SIT.Manager.Services;

public partial class VersionService(ILogger<VersionService> logger) : IVersionService
{
    private const string EFTFileName = "EscapeFromTarkov.exe";
    private const string SITAssemblyName = "StayInTarkov.dll";
    private const string AkiFileName = "Aki.Server.exe";
    private readonly ILogger<VersionService> _logger = logger;

    [GeneratedRegex("[0]{1,}\\.[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,2}\\-[0-9]{1,5}")]
    private static partial Regex EFTVersionRegex();

    [GeneratedRegex("[1]{1,}\\.[0-9]{1,2}\\.[0-9]{1,5}\\.[0-9]{1,5}")]
    private static partial Regex SITVersionRegex();

    private static string GetFileProductVersionString(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;

        // Use the first traditional / recommended method first
        string fileVersion = FileVersionInfo.GetVersionInfo(filePath).ProductVersion ?? string.Empty;

        // If the above doesn't return anything attempt to read the executable itself
        if (string.IsNullOrEmpty(fileVersion))
        {
            PeFile peHeader = new(filePath);
            StringFileInfo? stringFileInfo = peHeader.Resources?.VsVersionInfo?.StringFileInfo;
            if (stringFileInfo != null)
            {
                StringTable? fileInfoTable = stringFileInfo.StringTable.Length != 0 ? stringFileInfo.StringTable[0] : null;
                fileVersion = fileInfoTable?.ProductVersion ?? string.Empty;
            }
        }

        return fileVersion;
    }

    private string GetComponentVersion(string path)
    {
        string fileName = Path.GetFileName(path);
        string fileVersion = GetFileProductVersionString(path);
        if (string.IsNullOrEmpty(fileVersion))
        {
            _logger.LogWarning("Check {fileName} Version: File did not exist at {filePath}", fileName, fileVersion);
        }
        else
        {
            _logger.LogInformation("{fileName} Version is now: {fileVersion}", fileVersion, fileName);
        }

        return fileVersion;
    }

    //TODO: Move hardcoded strings to constants
    public string GetSptAkiVersion(string path)
    {
        // TODO fix this when installed on linux
        string filePath = Path.Combine(path, AkiFileName);
        return GetComponentVersion(filePath);
    }

    public string GetEFTVersion(string path)
    {
        string filePath = path;
        if (Path.GetFileName(path) != EFTFileName)
            filePath = Path.Combine(path, EFTFileName);

        return EFTVersionRegex().Match(GetComponentVersion(filePath)).Value.Replace('-', '.');
    }

    public string GetSITVersion(string path)
    {
        string filePath = Path.Combine(path, "BepInEx", "plugins", SITAssemblyName);
        return SITVersionRegex().Match(GetComponentVersion(filePath)).Value;
    }

    public string GetSitModVersion(string path)
    {
        string filePath = Path.Combine(path, "user", "mods", "SITCoop", "package.json");
        string fileVersion = string.Empty;
        if (!File.Exists(filePath)) return fileVersion;
        
        //TODO: Replace this with JsonNode/Element
        Utf8JsonReader reader = new(File.ReadAllBytes(filePath));
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? jsonDocument))
            return fileVersion;

        if (jsonDocument.RootElement.TryGetProperty("version", out JsonElement jsonElement))
            fileVersion = jsonElement.GetString() ?? string.Empty;
        
        return fileVersion;
    }
}
