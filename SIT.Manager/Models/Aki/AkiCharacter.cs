﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SIT.Manager.Models.Aki;
public class AkiCharacter(AkiServer parent, string username, string password) : ObservableObject
{
    [JsonPropertyName("Username")]
    public string Username { get; init; } = username;
    [JsonPropertyName("Password")]
    public string Password { get; init; } = password;
    [JsonPropertyName("ProfileID")]
    public string ProfileID { get; internal set; } = "";
    [JsonPropertyName("Edition")]
    public string Edition { get; internal set; } = "Edge of Darkness";
    [JsonIgnore]
    public AkiServer ParentServer = parent;
}