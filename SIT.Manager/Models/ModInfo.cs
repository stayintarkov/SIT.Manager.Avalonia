using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.Models;

public partial class ModInfo : ObservableObject
{
    [ObservableProperty]
    public string _name = string.Empty;
    [ObservableProperty]
    public string _modVersion = string.Empty;
    [ObservableProperty]
    public string _path = string.Empty;
    [ObservableProperty]
    public bool _isEnabled = true;
    [ObservableProperty]
    public bool _isRequired = false;
}
