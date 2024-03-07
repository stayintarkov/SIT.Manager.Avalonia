using Avalonia.Markup.Xaml.Styling;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SIT.Manager.Avalonia.Services
{
    public partial class LocalizationService : ILocalizationService
    {
        private const string DEFAULT_LANGUAGE = "en-US";

        private readonly IManagerConfigService _configService;

        private ResourceInclude? resourceInclude;

        public LocalizationService(IManagerConfigService configService)
        {
            _configService = configService;
            VerifyLocaleAvailability();
        }

        private void VerifyLocaleAvailability()
        {
            string currentLanguage = _configService.Config.CurrentLanguageSelected;
            List<CultureInfo> availableLanguages = GetAvailableLocalizations();
            if (!availableLanguages.Any(x => x.Name == _configService.Config.CurrentLanguageSelected))
            {
                _configService.Config.CurrentLanguageSelected = DEFAULT_LANGUAGE;
            }
        }

        /// <summary>
        /// Changes the localization based on your culture info. This specific function changes it inside of Settings. And mainly changes all dynamic Resources in pages.
        /// </summary>
        /// <param name="cultureInfo">the current culture</param>
        public void Translate(CultureInfo cultureInfo)
        {
            resourceInclude = null;
            var translations = App.Current.Resources.MergedDictionaries.OfType<ResourceInclude>().FirstOrDefault(x => x.Source?.OriginalString?.Contains("/Localization/") ?? false);
            try
            {
                if (translations != null) App.Current.Resources.MergedDictionaries.Remove(translations);
                LoadTranslationResources(cultureInfo.Name);
            }
            catch // if there was no translation found for your computer localization give default English.
            {
                LoadTranslationResources(DEFAULT_LANGUAGE);
            }
        }

        private void LoadTranslationResources(string cultureInfo)
        {
            CultureInfo culture = new(cultureInfo);
            App.Current.Resources.MergedDictionaries.Add(CreateResourceLocalization(culture.Name));
            _configService.Config.CurrentLanguageSelected = culture.Name;
        }

        /// <summary>
        /// Changes the localization in .cs files that contains strings that you cannot change inside the page.
        /// Functions contain neat parameters that help modify source strings, like in C#, but inside a Resource file.
        /// Example will be: Your path is %1. %1 → path. | Output: Your path is: C:\Users\...
        /// where % is the definition of parameter, and 1…n is the hierarchy of parameters passed to the function.
        /// </summary>
        /// <param name="key">string that you are accessing in Localization\*culture-info*.axaml file</param>
        /// <param name="replaces">parameters in hierarchy, example: %1, %2, %3, "10", "20, "30" | output: 10, 20, 30</param>
        public string TranslateSource(string key, params string[] replaces)
        {
            if (resourceInclude == null)
            {
                try
                {
                    resourceInclude = CreateResourceLocalization(_configService.Config.CurrentLanguageSelected);
                }
                catch // If there was an issue loading current Culture language, we will default by English.
                {
                    resourceInclude = CreateResourceLocalization("en-US");
                }
            }

            string result = "not found";
            if (resourceInclude.TryGetResource(key, null, out object? translation) || (resourceInclude = CreateResourceLocalization("en-US")).TryGetResource(key, null, out translation))
            {
                if (translation != null)
                {
                    result = (string) translation;
                    for (int i = 0; i < replaces.Length; i++)
                    {
                        result = result.Replace($"%{i + 1}", replaces[i]);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Function that loads the Available Localizations when program starts.
        /// </summary>
        public List<CultureInfo> GetAvailableLocalizations()
        {
            List<CultureInfo?> result = [];
            var assembly = typeof(LocalizationService).Assembly;
            string folderName = string.Format("{0}.Localization", assembly.GetName().Name);
            result = assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(folderName) && r.EndsWith(".axaml"))
                .Select(r =>
                {
                    string languageCode = r.Split('.')[^2];
                    try
                    {
                        return new CultureInfo(languageCode);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .ToList();

            if (result.Count == 0) result.Add(new CultureInfo(DEFAULT_LANGUAGE));
            List<CultureInfo> resultNotNull = [];
            foreach (var r in result)
            {
                if (r != null) resultNotNull.Add(r);
            }
            return resultNotNull;
        }

        /// <summary>
        /// Creates a Resource that will load Localization later on.
        /// </summary>
        /// <returns>Resource with Localization</returns>
        private ResourceInclude CreateResourceLocalization(string locale)
        {
            string url = $"avares://SIT.Manager.Avalonia/Localization/{locale}.axaml";
            Uri self = new("resm:Styles?assembly=SIT.Manager.Avalonia");
            return new ResourceInclude(self)
            {
                Source = new Uri(url)
            };
        }
    }
}