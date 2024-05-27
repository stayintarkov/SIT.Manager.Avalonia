using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Config;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Settings;

public partial class LinuxViewModel : SettingsViewModelBase
{
    // TODO: Check which services are needed for this ViewModel.
    private readonly IBarNotificationService _barNotificationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private LinuxConfig _linuxConfig = new();

    // DXVK Versions TODO
    [ObservableProperty]
    private List<string> _dxvkVersions = [];

    public IAsyncRelayCommand ChangePrefixLocationCommand { get; }
    public IAsyncRelayCommand ChangeRunnerLocationCommand { get; }

    public LinuxViewModel(IBarNotificationService barNotificationService,
        IManagerConfigService configService,
        ILocalizationService localizationService,
        IPickerDialogService pickerDialogService) : base(configService, pickerDialogService)
    {
        _barNotificationService = barNotificationService;
        _localizationService = localizationService;

        ChangePrefixLocationCommand = new AsyncRelayCommand(ChangePrefixLocation);
        ChangeRunnerLocationCommand = new AsyncRelayCommand(ChangeRunnerLocation);
    }

    [RelayCommand]
    private void AddEnv()
    {
        LinuxConfig.WineEnv.Add("", "");
    }

    [RelayCommand]
    private void DeleteEnv()
    {
        // TODO: get the selected item and remove it from the dictionary
    }

    private async Task ChangePrefixLocation()
    {
        IStorageFolder? newPath = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (newPath != null)
        {
            LinuxConfig.WinePrefix = newPath.Path.AbsolutePath;
        }
        else
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("LinuxSettingsPageViewModelConfigErrorPrefix"));
        }
    }

    private async Task ChangeRunnerLocation()
    {
        string newPath = await GetPathLocation(Path.Combine("bin", "wine"));
        if (!string.IsNullOrEmpty(newPath))
        {
            LinuxConfig.WineRunner = Path.Combine(newPath, "bin", "wine");
        }
        else
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("LinuxSettingsPageViewModelConfigErrorRunner"));
        }
    }
    

    protected override void OnActivated()
    {
        base.OnActivated();

        LinuxConfig = _configsService.Config.LinuxSettings;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    }
}
