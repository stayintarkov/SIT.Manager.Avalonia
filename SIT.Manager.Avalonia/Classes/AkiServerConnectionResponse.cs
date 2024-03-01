using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIT.Manager.Avalonia.Classes
{
    public class AkiServerConnectionResponse
    {
        [JsonPropertyName("backendUrl")]
        public string BackendUrl { get; init; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("editions")]
        public string[] Editions { get; init; } = [];
        [JsonPropertyName("profileDescriptions")]
        public Dictionary<string, string> Descriptions { get; init; } = [];
    }
}
