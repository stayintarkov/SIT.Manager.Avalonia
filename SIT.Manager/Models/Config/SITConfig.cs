using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;

namespace SIT.Manager.Models.Config;

public partial class SITConfig : ObservableObject
{
    [ObservableProperty] private AkiServer? _lastServer = null;

    [ObservableProperty] private string _sitEFTInstallPath = string.Empty;
    
    [ObservableProperty] private string _sitTarkovVersion = string.Empty;
    
    [ObservableProperty] private string _sitVersion = string.Empty;
    
    [ObservableProperty] private DateTime _lastSitUpdateCheckTime = DateTime.MinValue;
    
    public List<AkiServer> BookmarkedServers { get; init; } = [];
}
