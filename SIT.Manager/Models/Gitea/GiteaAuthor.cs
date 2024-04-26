using System;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Gitea;

public class GiteaAuthor
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
    [JsonPropertyName("created")]
    public DateTime Created { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("followers_count")]
    public int FollowersCount { get; set; }
    [JsonPropertyName("following_count")]
    public int FollowingCount { get; set; }
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }
    [JsonPropertyName("language")]
    public string? Language { get; set; }
    [JsonPropertyName("last_login")]
    public DateTime LastLogin { get; set; }
    [JsonPropertyName("location")]
    public string? Location { get; set; }
    [JsonPropertyName("login")]
    public string? Login { get; set; }
    [JsonPropertyName("login_name")]
    public string? LoginName { get; set; }
    [JsonPropertyName("prohibit_login")]
    public bool ProhibitLogin { get; set; }
    [JsonPropertyName("restricted")]
    public bool Restricted { get; set; }
    [JsonPropertyName("starred_repos_count")]
    public int StarredReposCount { get; set; }
    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }
    [JsonPropertyName("website")]
    public string? Website { get; set; }
}
