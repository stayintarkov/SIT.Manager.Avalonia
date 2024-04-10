using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Models.Aki;
public class AkiCharacter(AkiServer parent, string username, string password) : ObservableObject
{
#pragma warning disable CA1507 // Use nameof to express symbol names
    [JsonProperty("Username")]
    public string Username { get; init; } = username;
    [JsonProperty("Password")]
    public string Password { get; init; } = password;
    [JsonProperty("ProfileID")]
    public string ProfileID { get; internal set; } = "";
    [JsonProperty("Edition")]
#pragma warning restore CA1507 // Use nameof to express symbol names
    public string Edition { get; internal set; } = "Edge of Darkness";
    [JsonIgnore]
    public AkiServer ParentServer = parent;
}
