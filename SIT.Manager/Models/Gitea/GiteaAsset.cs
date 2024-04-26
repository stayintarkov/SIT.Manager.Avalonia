using System;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Gitea;

public class GiteaAsset
{
    [JsonPropertyName("browser_download_url")]
    public required string BrowserDownloadUrl { get; set; }
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("download_count")]
    public int DownloadCount { get; set; }
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("size")]
    public int Size { get; set; }
    [JsonPropertyName("uuid")]
    public string? UUID { get; set; }
}
