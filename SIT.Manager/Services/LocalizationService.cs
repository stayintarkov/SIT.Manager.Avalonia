using Avalonia.Markup.Xaml.Styling;
using SIT.Manager.Interfaces;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SIT.Manager.Services;

public partial class LocalizationService : ILocalizationService
{
    private const string ASSEMBLY_NAME = "SIT.Manager";
    private const string DEFAULT_LANGUAGE = "en-US";

    private readonly IManagerConfigService _configService;
    private string _currentSelectedLanguage => _configService.Config.LauncherSettings.CurrentLanguageSelected;

    private ResourceInclude? _resourceInclude;

    public CultureInfo DefaultLocale => new(DEFAULT_LANGUAGE);

    public event EventHandler<EventArgs>? LocalizationChanged;

    public LocalizationService(IManagerConfigService configService)
    {
        _configService = configService;
        VerifyLocaleAvailability();
    }

    /// <summary>
    /// Creates a Resource that will load Localization later on.
    /// </summary>
    /// <returns>Resource with Localization</returns>
    private static ResourceInclude CreateResourceLocalization(string locale)
    {
        string url = $"avares://SIT.Manager.ASM/Localization/{locale}.axaml";
        Uri self = new($"resm:Styles?assembly={ASSEMBLY_NAME}");
        return new ResourceInclude(self)
        {
            Source = new Uri(url)
        };
    }

    private void VerifyLocaleAvailability()
    {
        List<CultureInfo> availableLanguages = GetAvailableLocalizations();
        if (availableLanguages.All(x => x.Name != _currentSelectedLanguage))
        {
            _configService.Config.LauncherSettings.CurrentLanguageSelected = DEFAULT_LANGUAGE;
        }
    }

    /// <summary>
    /// Function that loads the Available Localizations when program starts.
    /// </summary>
    public List<CultureInfo> GetAvailableLocalizations()
    {
        Assembly assembly = typeof(LocalizationService).Assembly;
        const string folderName = $"{ASSEMBLY_NAME}.Localization";

        List<CultureInfo> result = [];
        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(folderName) || !resourceName.EndsWith(".axaml")) continue;

            int startPos = resourceName.IndexOf(folderName, StringComparison.Ordinal) + folderName.Length + 1;
            int endPos = resourceName.IndexOf('.', startPos);
            string languageCode = resourceName.Substring(startPos, endPos - startPos);
            result.Add(new CultureInfo(languageCode));
        }
        
        return result;
    }

    /// <summary>
    /// Changes the localization based on your culture info. This specific function changes it inside of Settings. And mainly changes all dynamic Resources in pages.
    /// </summary>
    /// <param name="cultureInfo">the current culture</param>
    public void SetLocalization(CultureInfo cultureInfo)
    {
        _resourceInclude = null;
        ResourceInclude? translations = App.Current.Resources.MergedDictionaries.OfType<ResourceInclude>()
            .FirstOrDefault(x => x.Source?.OriginalString?.Contains("/Localization/") ?? false);
        try
        {
            if (translations != null) App.Current.Resources.MergedDictionaries.Remove(translations);
            LoadTranslationResources(cultureInfo.Name);
        }
        catch // if there was no translation found for your computer localization give default English.
        {
            LoadTranslationResources(DEFAULT_LANGUAGE);
        }
        LocalizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadTranslationResources(string cultureInfo)
    {
        CultureInfo culture = new(cultureInfo);
        App.Current.Resources.MergedDictionaries.Add(CreateResourceLocalization(culture.Name));
        _configService.Config.LauncherSettings.CurrentLanguageSelected = culture.Name;
    }

    /// <summary>
    /// Changes the localization in .cs files that contains strings that you cannot change inside the page.
    /// Functions contain neat parameters that help modify source strings, like in C#, but inside a Resource file.
    /// Example will be: Your path is %1. %1 → path. | Output: Your path is: C:\Users\...
    /// where % is the definition of parameter, and 1…n is the hierarchy of parameters passed to the function.
    /// </summary>
    /// <param name="key">string that you are accessing in Localization\*culture-info*.axaml file</param>
    /// <param name="replaces">parameters in hierarchy, example: {0}, {1}, {2}, "10", "20, "30" | output: 10, 20, 30</param>
    public string TranslateSource(string key, params object?[] replaces)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        if (_resourceInclude == null)
        {
            try
            {
                _resourceInclude = CreateResourceLocalization(_currentSelectedLanguage);
            }
            catch // If there was an issue loading current Culture language, we will default by English.
            {
                _resourceInclude = CreateResourceLocalization("en-US");
            }
        }

        string result = "[DEV PROBLEM] No key found";
        if (!_resourceInclude.TryGetResource(key, null, out object? translation) &&
            !CreateResourceLocalization(DEFAULT_LANGUAGE).TryGetResource(key, null, out translation))
            return result;

        if (translation != null)
            result = string.Format((string) translation, replaces);
        return result;
    }
}
