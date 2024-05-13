using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class Banner
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("pic")]
    public Bundle? Pic { get; set; }
}
