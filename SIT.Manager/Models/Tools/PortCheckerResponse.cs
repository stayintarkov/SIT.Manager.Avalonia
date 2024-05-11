using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Tools;

public class PortCheckerResponse
{
    [JsonPropertyName("akiSuccess")]
    public bool AkiSuccess { get; init; }
    [JsonPropertyName("natSuccess")]
    public bool NatSuccess { get; init; }
    [JsonPropertyName("relaySuccess")]
    public bool RelaySuccess { get; init; }
    [JsonPropertyName("portsUsed")]
    public KeyValuePair<string, int> PortsUsed { get; init; }
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; init; }
}
