using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class Bundle
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }
    [JsonPropertyName("rcid")]
    public string? Rcid { get; set; }
}
