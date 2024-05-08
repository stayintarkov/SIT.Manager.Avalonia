using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class ColliderParams
{
    [JsonPropertyName("_parent")]
    public string? Parent { get; set; }
    [JsonPropertyName("_props")]
    public Props? Props { get; set; }
}
