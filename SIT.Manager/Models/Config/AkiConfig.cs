using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.Models.Config;

public partial class AkiConfig : ObservableObject
{
    [ObservableProperty] private string _akiServerPath = string.Empty;
    
    [ObservableProperty] private Color _consoleFontColor = Colors.LightBlue;
    
    [ObservableProperty] private string _consoleFontFamily = "Consolas";
    
    [ObservableProperty] private string _sptAkiVersion = string.Empty;
    
    [ObservableProperty] private string _sitModVersion = string.Empty;
}
