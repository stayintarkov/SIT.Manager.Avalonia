using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
