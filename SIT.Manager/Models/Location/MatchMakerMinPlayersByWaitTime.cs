using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class MatchMakerMinPlayersByWaitTime
{
    [JsonPropertyName("minPlayers")]
    public int MinPlayers { get; set; }
    [JsonPropertyName("time")]
    public int Time { get; set; }
}
