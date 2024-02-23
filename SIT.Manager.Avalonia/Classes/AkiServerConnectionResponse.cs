using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
