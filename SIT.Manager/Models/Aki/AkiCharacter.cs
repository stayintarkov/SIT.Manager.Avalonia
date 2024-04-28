using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Aki;

public class AkiCharacter : ObservableObject
{
    [JsonPropertyName("Username")]
    public string Username { get; init; } = string.Empty;
    [JsonPropertyName("Password")]
    public string Password { get; init; } = string.Empty;
    [JsonPropertyName("ProfileID")]
    public string ProfileID { get; internal set; } = string.Empty;
    [JsonPropertyName("Edition")]
    public string Edition { get; internal set; } = "Edge of Darkness";

    public AkiCharacter(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public AkiCharacter()
    {
    }
}
