using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Models.Aki;
public class AkiServerInfo
{
    [JsonProperty("name")]
    public string Name { get; init; } = string.Empty;
    [JsonProperty("editions")]
    public string[] Editions { get; init; } = [];
    [JsonProperty("profileDescriptions")]
    public Dictionary<string, string> Descriptions { get; init; } = [];
}
