using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Gitea;

public class GiteaRelease
{
    [JsonPropertyName("assets")]
    public required List<GiteaAsset> Assets { get; set; }
    [JsonPropertyName("author")]
    public GiteaAuthor? Author { get; set; }
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("draft")]
    public bool Draft { get; set; }
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }
    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }
    [JsonPropertyName("tarball_url")]
    public string? TarballUrl { get; set; }
    [JsonPropertyName("target_commitish")]
    public string? TargetCommitish { get; set; }
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    [JsonPropertyName("zipball_url")]
    public string? ZipballUrl { get; set; }
}
