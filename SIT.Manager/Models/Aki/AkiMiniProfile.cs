using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Aki;

public class AkiMiniProfile
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;
    [JsonPropertyName("nickname")]
    public string Nickname { get; init; } = string.Empty;
    [JsonPropertyName("side")]
    public string Side { get; init; } = string.Empty;
    [JsonPropertyName("currlvl")]
    public int CurrentLevel { get; init; } = -1;
    [JsonPropertyName("currexp")]
    public int CurrentExperience { get; init; } = -1;
    [JsonPropertyName("prevexp")]
    public int PreviousExperience { get; init; } = -1;
    [JsonPropertyName("nextlvl")]
    public int NextExperience { get; init; } = -1;
    [JsonPropertyName("maxlvl")]
    public int MaxLevel { get; init; } = -1;
}
