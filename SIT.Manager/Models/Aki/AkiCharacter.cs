using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Aki;
public class AkiCharacter : ObservableObject
{
    [JsonPropertyName("Username")]
    public string Username { get; init; }
    [JsonPropertyName("Password")]
    public string Password { get; init; }
    [JsonPropertyName("ProfileID")]
    public string ProfileID { get; internal set; } = "";
    [JsonPropertyName("Edition")]
    public string Edition { get; internal set; } = "Edge of Darkness";
    [JsonIgnore]
    public AkiServer ParentServer;

    public AkiCharacter(AkiServer parent, string username, string password)
    {
        ParentServer = parent;
        Username = username;
        Password = password;
    }

    [JsonConstructor]
    public AkiCharacter(AkiServer parentServer, string username, string password, string edition, string profileID)
    {
        this.ParentServer = parentServer;
        this.Username = username;
        this.Password = password;
        this.Edition = edition;
        this.ProfileID = profileID;
    }
}
