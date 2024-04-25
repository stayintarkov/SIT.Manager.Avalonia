using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Aki;

public class AkiServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("editions")]
    public string[] Editions { get; init; } = [];
    [JsonPropertyName("profileDescriptions")]
    public Dictionary<string, string> Descriptions { get; init; } = [];
}
