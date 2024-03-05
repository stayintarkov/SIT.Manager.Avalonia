using Avalonia;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.Styling;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly IManagerConfigService _configsService;
    private readonly ILocalizationService _localizationService;
    private readonly IBarNotificationService _barNotificationService;
    private readonly IPickerDialogService _pickerDialogService;
    private readonly IVersionService _versionService;

    [ObservableProperty]
    private ManagerConfig _config;

    [ObservableProperty]
    private FontFamily _selectedConsoleFontFamily;

    [ObservableProperty]
    private List<FontFamily> _installedFonts;

    [ObservableProperty]
    private List<CultureInfo> _availableLocalization = [];
    
    [ObservableProperty]
    private CultureInfo _currentLocalization;

    [ObservableProperty]
    private Color? _currentAccentColor;
    
    [ObservableProperty]
    private string _managerVersionString;

    public static bool ShowLinuxSettings => OperatingSystem.IsLinux();

    public IAsyncRelayCommand ChangeInstallLocationCommand { get; }

    public IAsyncRelayCommand ChangeAkiServerLocationCommand { get; }

    private FluentAvaloniaTheme? faTheme;

    public SettingsPageViewModel(IManagerConfigService configService,
                                 ILocalizationService localizationService,
                                 IBarNotificationService barNotificationService,
                                 IPickerDialogService pickerDialogService,
                                 IVersionService versionService) 
    {
        _configsService = configService;
        _pickerDialogService = pickerDialogService;
        _barNotificationService = barNotificationService;
        _versionService = versionService;
        _localizationService = localizationService;

        _config = _configsService.Config;
        _currentAccentColor = _config.AccentColor;

        _currentLocalization = new CultureInfo(Config.CurrentLanguageSelected);
        _availableLocalization = _localizationService.GetAvailableLocalizations();
        _localizationService.Translate(_currentLocalization);

        _config.PropertyChanged += (o, e) => OnPropertyChanged(e);

        List<FontFamily> installedFonts = [.. FontManager.Current.SystemFonts];
        installedFonts.Add(FontFamily.Parse("Bender"));
        _installedFonts = [.. installedFonts.OrderBy(x => x.Name)];

        _selectedConsoleFontFamily = InstalledFonts.FirstOrDefault(x => x.Name == _config.ConsoleFontFamily, FontFamily.Parse("Bender"));

        _managerVersionString = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N/A";

        ChangeInstallLocationCommand = new AsyncRelayCommand(ChangeInstallLocation);
        ChangeAkiServerLocationCommand = new AsyncRelayCommand(ChangeAkiServerLocation);
    }

    /// <summary>
    /// Gets the path containing the required filename based on the folder picker selection from a user
    /// </summary>
    /// <param name="filename">The filename to look for in the user specified directory</param>
    /// <returns>The path if the file exists, otherwise an empty string</returns>
    private async Task<string> GetPathLocation(string filename) {
        IStorageFolder? directorySelected = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (directorySelected != null) {
            if (File.Exists(Path.Combine(directorySelected.Path.LocalPath, filename))) {
                return directorySelected.Path.LocalPath;
            }
        }
        return string.Empty;
    }

    private async Task ChangeInstallLocation() {
        string targetPath = await GetPathLocation("EscapeFromTarkov.exe");
        if (!string.IsNullOrEmpty(targetPath)) {
            Config.InstallPath = targetPath;
            Config.TarkovVersion = _versionService.GetEFTVersion(targetPath);
            Config.SitVersion = _versionService.GetSITVersion(targetPath);
            _barNotificationService.ShowInformational(_localizationService.TranslateSource("SettingsPageViewModelConfigTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigInformationEFTDescription", targetPath));
        }
        else {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigErrorEFTDescription"));
        }
    }

    private async Task ChangeAkiServerLocation() {
        string targetPath = await GetPathLocation("Aki.Server.exe");
        if (!string.IsNullOrEmpty(targetPath)) {
            Config.AkiServerPath = targetPath;
            _barNotificationService.ShowInformational(_localizationService.TranslateSource("SettingsPageViewModelConfigTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigInformationSPTAKIDescription", targetPath));
        }
        else {
            _barNotificationService.ShowError(_localizationService.TranslateSource("SettingsPageViewModelErrorTitle"), _localizationService.TranslateSource("SettingsPageViewModelConfigErrorSPTAKI"));
        }
    }

    partial void OnSelectedConsoleFontFamilyChanged(FontFamily value) {
        Config.ConsoleFontFamily = value.Name;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);
        _configsService.UpdateConfig(Config);
    }

    partial void OnCurrentLocalizationChanged(CultureInfo value)
    {
        _localizationService.Translate(value);
        Config.CurrentLanguageSelected = value.Name;
    }

    partial void OnCurrentAccentColorChanged(Color? value)
    {
        if (faTheme == null) faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme != null)
        {
            faTheme.CustomAccentColor = value;
            Config.AccentColor = value;
        }
    }
}