using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace SIT.Manager.ViewModels.Settings;

public partial class LauncherViewModel(IManagerConfigService configService,
                         ILocalizationService localizationService,
                         IModService modService,
                         IPickerDialogService pickerDialogService) : SettingsViewModelBase(configService, pickerDialogService)
{
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly IModService _modService = modService;

    private readonly FluentAvaloniaTheme? faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();

    [ObservableProperty]
    private bool _isTestModeEnabled = false;

    [ObservableProperty]
    private List<CultureInfo> _availableLocalizations = localizationService.GetAvailableLocalizations();

    [ObservableProperty]
    private CultureInfo _currentLocalization = localizationService.DefaultLocale;

    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Config.AccentColor))
        {
            if (faTheme != null && faTheme.CustomAccentColor != Config.AccentColor)
            {
                faTheme.CustomAccentColor = Config.AccentColor;
            }
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        CurrentLocalization = AvailableLocalizations.FirstOrDefault(x => x.Name == Config.CurrentLanguageSelected, _localizationService.DefaultLocale);
        IsTestModeEnabled = Config.EnableTestMode;

        Config.PropertyChanged += Config_PropertyChanged;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        Config.PropertyChanged -= Config_PropertyChanged;
    }

    partial void OnCurrentLocalizationChanged(CultureInfo value)
    {
        if (value != null)
        {
            Config.CurrentLanguageSelected = value.Name;
            _localizationService.Translate(value);
        }
    }

    async partial void OnIsTestModeEnabledChanged(bool value)
    {
        // If test mode is enabled then we want to check that there's no mods we don't approve of currently installed.
        if (value)
        {
            List<string> installedMods = _configsService.Config.InstalledMods.Keys.ToList();
            int compatibleModCount = installedMods.Count(x => _modService.RecommendedModInstalls.Contains(x));
            int totalIncompatibleMods = installedMods.Count - compatibleModCount;
            if (totalIncompatibleMods > 0)
            {
                await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("SettingsPageViewModelEnableDevModeErrorTitle"),
                    Content = _localizationService.TranslateSource("SettingsPageViewModelEnableDevModeErrorDescription", totalIncompatibleMods.ToString()),
                    CloseButtonText = _localizationService.TranslateSource("SettingsPageViewModelEnableDevModeErrorButtonOk")
                }.ShowAsync();
            }
            else
            {
                Config.EnableTestMode = value;
            }
        }
        else
        {
            Config.EnableTestMode = value;
        }
    }
}
