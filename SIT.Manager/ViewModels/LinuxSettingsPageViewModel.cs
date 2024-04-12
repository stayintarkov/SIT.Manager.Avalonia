using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using System.Collections.Generic;
using System.ComponentModel;

namespace SIT.Manager.ViewModels;

public partial class LinuxSettingsPageViewModel : ObservableObject
{
    // TODO: Check which services are needed for this ViewModel.
    private readonly IManagerConfigService _configsService;
    
    [ObservableProperty]
    private LinuxConfig _config;
    
    // DXVK Versions
    [ObservableProperty] 
    private List<string> _dxvkVersions;
    
    public LinuxSettingsPageViewModel(IManagerConfigService configService)
    {
        _configsService = configService;

        _config = (LinuxConfig) _configsService.Config;
        
        _config.PropertyChanged += (o, e) => OnPropertyChanged(e);
        
        // Find dxvk versions
        //_dxvkVersions = _versionService.GetDXVKVersions();
    }
    
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        _configsService.UpdateConfig(Config);
    }
}
