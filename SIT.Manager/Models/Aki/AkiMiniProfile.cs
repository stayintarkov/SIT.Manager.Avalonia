using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Models.Aki;
public class AkiMiniProfile
{
    [JsonProperty("username")]
    public string Username { get; init; } = string.Empty;
    [JsonProperty("nickname")]
    public string Nickname { get; init; } = string.Empty;
    [JsonProperty("side")]
    public string Side { get; init; } = string.Empty;
    [JsonProperty("currlvl")]
    public int CurrentLevel { get; init; } = -1;
    [JsonProperty("currexp")]
    public int CurrentExperience { get; init; } = -1;
    [JsonProperty("prevexp")]
    public int PreviousExperience { get; init; } = -1;
    [JsonProperty("nextlvl")]
    public int NextExperience { get; init; } = -1;
    [JsonProperty("maxlvl")]
    public int MaxLevel { get; init; } = -1;
}
