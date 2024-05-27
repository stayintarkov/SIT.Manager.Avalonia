using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Config;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Settings;

public partial class SptAkiViewModel : SettingsViewModelBase
{
    private readonly IBarNotificationService _barNotificationService;
    private readonly ILocalizationService _localizationService;
    private readonly IVersionService _versionService;
    private AkiConfig _akiConfig => Config.AkiSettings;

    [ObservableProperty]
    private FontFamily _selectedConsoleFontFamily = FontFamily.Default;

    [ObservableProperty]
    private List<FontFamily> _installedFonts;

    public IAsyncRelayCommand ChangeAkiServerLocationCommand { get; }

    public SptAkiViewModel(IBarNotificationService barNotificationService,
        IManagerConfigService configService,
        ILocalizationService localizationService,
        IPickerDialogService pickerDialogService,
        IVersionService versionService) : base(configService, pickerDialogService)
    {
        _barNotificationService = barNotificationService;
        _localizationService = localizationService;
        _versionService = versionService;

        List<FontFamily> installedFonts = [.. FontManager.Current.SystemFonts];
        installedFonts.Add(FontFamily.Parse("Bender"));
        InstalledFonts = [.. installedFonts.OrderBy(x => x.Name)];

        ChangeAkiServerLocationCommand = new AsyncRelayCommand(ChangeAkiServerLocation);
    }

    private async Task ChangeAkiServerLocation()
    {
        string targetPath = await GetPathLocation("Aki.Server.exe");
        if (!string.IsNullOrEmpty(targetPath))
        {
            _akiConfig.AkiServerPath = targetPath;
            _akiConfig.SptAkiVersion = _versionService.GetSptAkiVersion(targetPath);
            _akiConfig.SitModVersion = _versionService.GetSitModVersion(targetPath);
            _barNotificationService.ShowInformational(_localizationService.TranslateSource("SettingsPageViewModelConfigTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigInformationSPTAKIDescription", targetPath));
        }
        else
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigErrorSPTAKI"));
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        SelectedConsoleFontFamily = InstalledFonts.FirstOrDefault(x => x.Name == _akiConfig.ConsoleFontFamily, FontFamily.Parse("Bender"));
    }

    partial void OnSelectedConsoleFontFamilyChanged(FontFamily value)
    {
        _akiConfig.ConsoleFontFamily = value.Name;
    }
}
