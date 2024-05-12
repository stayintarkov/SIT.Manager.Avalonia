using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Tools;

public class PortCheckerResponse
{
    [JsonPropertyName("akiSuccess")] public bool AkiSuccess { get; init; } = false;
    [JsonPropertyName("natSuccess")] public bool NatSuccess { get; init; } = false;
    [JsonPropertyName("relaySuccess")] public bool RelaySuccess { get; init; } = false;
    
    [JsonPropertyName("portsUsed")]
    public Dictionary<string, string> PortsUsed { get; init; } = new()
    {
        { "akiPort", "6969" }, { "relayPort", "6970" }, { "natPort", "6971" }
    };

    [JsonPropertyName("ipAddress")] public string? IpAddress { get; init; } = "x.x.x.x";
}
