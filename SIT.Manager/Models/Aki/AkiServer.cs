using Avalonia.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SIT.Manager.Models.Aki;
public partial class AkiServer(Uri address) : ObservableObject
{
    [JsonProperty(nameof(Address))]
    public Uri Address { get; } = address;
    [JsonProperty(nameof(Characters))]
    public readonly List<AkiCharacter> Characters = [];
    [JsonIgnore]
    public string Name { get; internal set; } = string.Empty;
    [JsonIgnore]
    public int Players { get; internal set; } = 0;
    [JsonIgnore]
    public int Ping { get; internal set; } = -1;
}
