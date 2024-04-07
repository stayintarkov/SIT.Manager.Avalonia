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
    [JsonProperty("Name")]
    public string Username = username;
    [JsonProperty("Password")]
    public string Password = password;
    [JsonProperty("ProfileID")]
    public string ProfileID = "";
    [JsonProperty("Edition")]
    public string Edition = "Edge of Darkness";
    [JsonIgnore]
    public AkiServer ParentServer = parent;
}
