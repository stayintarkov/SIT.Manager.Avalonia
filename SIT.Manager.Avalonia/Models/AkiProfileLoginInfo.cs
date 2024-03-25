using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Models;
public class AkiProfileLoginInfo(AkiProfile akiProfile)
{
    [JsonPropertyName("username")]
    public string Username { get; } = akiProfile.Username;
    [JsonPropertyName("email")]
    public string Email => Username;
    [JsonPropertyName("edition")]
    public string Edition { get; set; } = akiProfile.Edition;
    [JsonPropertyName("password")]
    public string Password { get; init; } = akiProfile.Password;
    [JsonPropertyName("backendUrl")]
    public string BackendUrl { get; init; } = akiProfile.Server.Address
}
