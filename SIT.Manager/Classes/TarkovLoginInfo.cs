using System;
using System.Text.Json.Serialization;

namespace SIT.Manager.Classes;

[Serializable]
public class TarkovLoginInfo
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;
    [JsonPropertyName("email")]
    public string Email => Username;
    [JsonPropertyName("edition")]
    public string Edition { get; set; } = "Edge Of Darkness";
    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
    [JsonPropertyName("backendUrl")]
    public string BackendUrl { get; init; } = string.Empty;
}
