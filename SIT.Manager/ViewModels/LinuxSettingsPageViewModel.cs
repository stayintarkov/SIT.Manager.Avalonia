using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

public partial class LinuxSettingsPageViewModel : ObservableObject
{
    // TODO: Check which services are needed for this ViewModel.
    private readonly IManagerConfigService _configsService;
    private readonly IPickerDialogService _pickerDialogService;
    private readonly IBarNotificationService _barNotificationService;
    private readonly ILocalizationService _localizationService;
    
    [ObservableProperty]
    private LinuxConfig _config;
    // DXVK Versions
    [ObservableProperty] 
    private List<string> _dxvkVersions;
    
    public IAsyncRelayCommand ChangePrefixLocationCommand { get; }
    public IAsyncRelayCommand ChangeRunnerLocationCommand { get; }
    
    public LinuxSettingsPageViewModel(IManagerConfigService configService,
                                      IPickerDialogService pickerDialogService,
                                      IBarNotificationService barNotificationService,
                                      ILocalizationService localizationService)
    {
        _configsService = configService;
        _pickerDialogService = pickerDialogService;
        _barNotificationService = barNotificationService;
        _localizationService = localizationService;
        
        _config = (LinuxConfig) _configsService.Config;
        
        _config.PropertyChanged += (o, e) => OnPropertyChanged(e);
        
        // Find dxvk versions
        //_dxvkVersions = _versionService.GetDXVKVersions();
        
        ChangePrefixLocationCommand = new AsyncRelayCommand(ChangePrefixLocation);
        ChangeRunnerLocationCommand = new AsyncRelayCommand(ChangeRunnerLocation);
    }
    
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        _configsService.UpdateConfig(Config);
    }

    private async Task ChangePrefixLocation()
    {
        IStorageFolder? newPath = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (newPath != null)
        {
            Config.WinePrefix = newPath.Path.AbsolutePath;
        }
        else
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("LinuxSettingsPageViewModelConfigErrorPrefix"));
        }
    }
    
    /// <summary>
    /// Gets the path containing the required filename based on the folder picker selection from a user
    /// </summary>
    /// <param name="filename">The filename to look for in the user specified directory</param>
    /// <returns>The path if the file exists, otherwise an empty string</returns>
    private async Task<string> GetPathLocation(string filename)
    {
        IStorageFolder? directorySelected = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (directorySelected == null)
        {
            return string.Empty;
        }

        return File.Exists(Path.Combine(directorySelected.Path.LocalPath, filename)) ? directorySelected.Path.LocalPath : string.Empty;
    }
    
    private async Task ChangeRunnerLocation()
    {
        string newPath = await GetPathLocation("wine");
        if (!string.IsNullOrEmpty(newPath))
        {
            Config.WineRunner = Path.Combine(newPath, "wine");
        }
        else
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("LinuxSettingsPageViewModelConfigErrorRunner"));
        }
    }
}
