using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Settings;

public partial class EftViewModel : SettingsViewModelBase
{
    private readonly IBarNotificationService _barNotificationService;
    private readonly ILocalizationService _localizationService;
    private readonly IVersionService _versionService;

    public IAsyncRelayCommand ChangeInstallLocationCommand { get; }

    public EftViewModel(IBarNotificationService barNotificationService,
        IManagerConfigService configService,
        ILocalizationService localizationService,
        IPickerDialogService pickerDialogService,
        IVersionService versionService) : base(configService, pickerDialogService)
    {
        _barNotificationService = barNotificationService;
        _localizationService = localizationService;
        _versionService = versionService;

        ChangeInstallLocationCommand = new AsyncRelayCommand(ChangeInstallLocation);
    }

    private async Task ChangeInstallLocation()
    {
        string targetPath = await GetPathLocation("EscapeFromTarkov.exe");
        if (!string.IsNullOrEmpty(targetPath))
        {
            Config.InstallPath = targetPath;
            Config.TarkovVersion = _versionService.GetEFTVersion(targetPath);
            Config.SitVersion = _versionService.GetSITVersion(targetPath);
            _barNotificationService.ShowInformational(_localizationService.TranslateSource("SettingsPageViewModelConfigTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigInformationEFTDescription", targetPath));
        }
        else
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigErrorEFTDescription"));
        }
    }
}
