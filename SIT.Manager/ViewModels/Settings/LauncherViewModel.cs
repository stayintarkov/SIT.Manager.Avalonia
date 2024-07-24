using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Styling;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Config;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace SIT.Manager.ViewModels.Settings;

public partial class LauncherViewModel(IManagerConfigService configService,
                         ILocalizationService localizationService,
                         IPickerDialogService pickerDialogService) : SettingsViewModelBase(configService, pickerDialogService)
{
    private LauncherConfig LauncherSettings => _configsService.Config.LauncherSettings;

    private readonly FluentAvaloniaTheme? _faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();

    [ObservableProperty]
    private bool _isTestModeEnabled = false;

    [ObservableProperty]
    private List<CultureInfo> _availableLocalizations = localizationService.GetAvailableLocalizations();

    [ObservableProperty]
    private CultureInfo _currentLocalization = localizationService.DefaultLocale;

    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LauncherSettings.AccentColor))
        {
            if (_faTheme != null && _faTheme.CustomAccentColor != LauncherSettings.AccentColor)
            {
                _faTheme.CustomAccentColor = LauncherSettings.AccentColor;
            }
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        CurrentLocalization = AvailableLocalizations.FirstOrDefault(x => x.Name == LauncherSettings.CurrentLanguageSelected, localizationService.DefaultLocale);
        IsTestModeEnabled = LauncherSettings.EnableTestMode;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        LauncherSettings.PropertyChanged -= Config_PropertyChanged;
    }

    partial void OnCurrentLocalizationChanged(CultureInfo value)
    {
        LauncherSettings.CurrentLanguageSelected = value.Name;
        localizationService.SetLocalization(value);
    }

    partial void OnIsTestModeEnabledChanged(bool value)
    {
        LauncherSettings.EnableTestMode = value;
    }
}
