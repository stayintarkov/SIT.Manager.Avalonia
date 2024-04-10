using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SIT.Manager.Models.Aki;
public partial class AkiServer(Uri address) : ObservableObject
{
    [JsonPropertyName("Address")]
    public Uri Address { get; } = address;
    [JsonPropertyName("Characters")]
    public List<AkiCharacter> Characters { get; init; } = [];
    [JsonIgnore]
    public string Name { get; internal set; } = string.Empty;
    [JsonIgnore]
    public int Ping { get; internal set; } = -1;
}
