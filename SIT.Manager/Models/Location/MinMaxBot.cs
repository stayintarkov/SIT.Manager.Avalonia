using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class MinMaxBot
{
    public string? WildSpawnType { get; set; }
    [JsonPropertyName("max")]
    public int Max { get; set; }
    [JsonPropertyName("min")]
    public int Min { get; set; }
}
