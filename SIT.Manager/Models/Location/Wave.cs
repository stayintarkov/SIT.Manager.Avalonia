using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class Wave
{
    [JsonIgnore]
    public int Name { get; set; }
    public string? BotPreset { get; set; }
    public string? BotSide { get; set; }
    public string? SpawnPoints { get; set; }
    public string? WildSpawnType { get; set; }
    [JsonPropertyName("isPlayers")]
    public bool IsPlayers { get; set; }
    [JsonPropertyName("number")]
    public int Number { get; set; }
    [JsonPropertyName("slots_max")]
    public int SlotsMax { get; set; }
    [JsonPropertyName("slots_min")]
    public int SlotsMin { get; set; }
    [JsonPropertyName("time_max")]
    public int TimeMax { get; set; }
    [JsonPropertyName("time_min")]
    public int TimeMin { get; set; }
}
