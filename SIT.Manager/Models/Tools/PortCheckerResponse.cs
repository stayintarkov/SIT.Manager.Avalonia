using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Tools;

public class PortCheckerResponse
{
    [JsonPropertyName("akiSuccess")] public bool AkiSuccess { get; init; } = false;
    [JsonPropertyName("natSuccess")] public bool NatSuccess { get; init; } = false;
    [JsonPropertyName("relaySuccess")] public bool RelaySuccess { get; init; } = false;

    [JsonPropertyName("portsUsed")] public PortCheckerPorts PortsUsed { get; init; } = new();

    [JsonPropertyName("ipAddress")] public string? IpAddress { get; init; } = "x.x.x.x";
}

public class PortCheckerPorts
{
    [JsonPropertyName("akiPort")] public string AkiPort { get; set; } = "6969";
    [JsonPropertyName("relayPort")] public string RelayPort { get; set; } = "6970";
    [JsonPropertyName("natPort")] public string NatPort { get; set; } = "6971";
}
