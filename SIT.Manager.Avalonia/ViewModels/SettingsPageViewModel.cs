using Avalonia;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
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
    private List<LocalizationModel> _availableLocalization = [];
    
    [ObservableProperty]
    private LocalizationModel _localization;

    [ObservableProperty]
    private string _managerVersionString;

    public IAsyncRelayCommand ChangeInstallLocationCommand { get; }

    public IAsyncRelayCommand ChangeAkiServerLocationCommand { get; }

    public SettingsPageViewModel(IManagerConfigService configService,
                                 ILocalizationService localizationService,
                                 IBarNotificationService barNotificationService,
                                 IPickerDialogService pickerDialogService,
                                 IVersionService versionService) {
        _configsService = configService;
        _pickerDialogService = pickerDialogService;
        _barNotificationService = barNotificationService;
        _versionService = versionService;
        _localizationService = localizationService;

        _config = _configsService.Config;


        /*
        THIS IS BROKEN, IT WORKS, ONLY IF LOCALIZATION FOLDER EXIST IN ROOT DIRECTORY OF THE PROJECT.
        COPY ALWAYS OR COPY IF NEWER DOESN'T HELP CREATE LOCALIZATION FOLDER FOR THE FIRST TIME ONLY AFTER SECOND BUILD.
        EXISTS ONLY BECAUSE OF DYNAMIC LOADING OF LANGUAGES. IF YOU DO KNOW HOW TO FIX THIS, BE MY GUEST TO FIX THIS.
        DirectoryInfo localizationFolder = new("./Localization");
        if (localizationFolder.Exists)
        {
            foreach (var languageFI in localizationFolder.EnumerateFiles())
            {
                string language = languageFI.Name[0..(languageFI.Name.Length - languageFI.Extension.Length)];
                _availableLocalization.Add(new LocalizationModel() { FullNameLanguage = CultureInfo.GetCultureInfo(language).NativeName, ShortNameLanguage = language });
            }
        }
        else // if there is no localization at all in Localization folder we will add at least default English
        {
            _availableLocalization.Add(new LocalizationModel() { FullNameLanguage = CultureInfo.GetCultureInfo("en-US").NativeName, ShortNameLanguage = "en-US" });
        }
        */

        // Right now, I don't know any better way to load localization files. Only adding them to List using hardcode.
        _availableLocalization.Add(new LocalizationModel() { FullNameLanguage = CultureInfo.GetCultureInfo("en-US").NativeName, ShortNameLanguage = "en-US" });
        _availableLocalization.Add(new LocalizationModel() { FullNameLanguage = CultureInfo.GetCultureInfo("uk-UA").NativeName, ShortNameLanguage = "en-US" });
        _availableLocalization.Add(new LocalizationModel() { FullNameLanguage = CultureInfo.GetCultureInfo("ru-RU").NativeName, ShortNameLanguage = "en-US" });

        var localization = AvailableLocalization.FirstOrDefault(lang => lang.ShortNameLanguage == _config.CurrentLanguageSelected.ShortNameLanguage);
        if (localization != null)
        {
            _localization = localization;
        }
        else
        {
            _localization = new LocalizationModel() { FullNameLanguage = CultureInfo.CurrentCulture.NativeName, ShortNameLanguage = CultureInfo.CurrentCulture.Name };
        }
        _localizationService.Translate(_localization, new(_localization.ShortNameLanguage));

        _localization.PropertyChanged += (o, e) => OnPropertyChanged(e);
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
            _barNotificationService.ShowInformational("Config", $"EFT installation path set to '{targetPath}'");
        }
        else {
            _barNotificationService.ShowError("Error", $"The selected folder was invalid. Make sure it's a proper EFT game folder.");
        }
    }

    private async Task ChangeAkiServerLocation() {
        string targetPath = await GetPathLocation("Aki.Server.exe");
        if (!string.IsNullOrEmpty(targetPath)) {
            Config.AkiServerPath = targetPath;
            _barNotificationService.ShowInformational("Config", $"SPT-AKI installation path set to '{targetPath}'");
        }
        else {
            _barNotificationService.ShowError("Error", "The selected folder was invalid. Make sure it's a proper SPT-AKI server folder.");
        }
    }

    partial void OnSelectedConsoleFontFamilyChanged(FontFamily value) {
        Config.ConsoleFontFamily = value.Name;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
        base.OnPropertyChanged(e);
        _localizationService.Translate(Localization, new (Localization.ShortNameLanguage));
        _configsService.UpdateConfig(Config);
    }
}
