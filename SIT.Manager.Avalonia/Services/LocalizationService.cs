using Avalonia.Markup.Xaml.Styling;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace SIT.Manager.Avalonia.Services
{
    public partial class LocalizationService(IManagerConfigService configService) : ILocalizationService
    {
        private readonly IManagerConfigService _configService = configService;

        private LocalizationModel _localization = new();
        public LocalizationModel Localization
        {
            get => _localization;
            private set { _localization = value; }
        }

        public void Translate(LocalizationModel localization, CultureInfo cultureInfo)
        {
            resourceInclude = null;
            var translations = App.Current.Resources.MergedDictionaries.OfType<ResourceInclude>().FirstOrDefault(x => x.Source?.OriginalString?.Contains("/Localization/") ?? false);

            try
            {
                if (translations != null) App.Current.Resources.MergedDictionaries.Remove(translations);
                App.Current.Resources.MergedDictionaries.Add(
                new ResourceInclude(new Uri($"avares://SIT.Manager.Avalonia/Localization/{cultureInfo.Name}.axaml"))
                {
                    Source = new Uri($"avares://SIT.Manager.Avalonia/Localization/{cultureInfo.Name}.axaml")
                });
                localization.ShortNameLanguage = cultureInfo.Name;
                localization.FullNameLanguage = cultureInfo.NativeName;
                _configService.Config.CurrentLanguageSelected = localization;
            }
            catch // if there was no translation found for your computer localization give default English.
            {
                App.Current.Resources.MergedDictionaries.Add(
                new ResourceInclude(new Uri($"avares://SIT.Manager.Avalonia/Localization/en-US.axaml"))
                {
                    Source = new Uri($"avares://SIT.Manager.Avalonia/Localization/en-US.axaml")
                });
                CultureInfo culture = new("en-US");
                localization.ShortNameLanguage = culture.Name;
                localization.FullNameLanguage = culture.NativeName;
                _configService.Config.CurrentLanguageSelected = localization;
            }
        }

        private ResourceInclude? resourceInclude;
        private string currentLanguage = string.Empty;
        public string TranslateSource(string key, params string[] replaces)
        {
            if (resourceInclude == null || string.IsNullOrEmpty(currentLanguage) || currentLanguage != _configService.Config.CurrentLanguageSelected.ShortNameLanguage)
            {
                try
                {
                    resourceInclude = new ResourceInclude(new Uri($"avares://SIT.Manager.Avalonia/Localization/{_configService.Config.CurrentLanguageSelected.ShortNameLanguage}.axaml"))
                    {
                        Source = new Uri($"avares://SIT.Manager.Avalonia/Localization/{_configService.Config.CurrentLanguageSelected.ShortNameLanguage}.axaml")
                    };
                }
                catch
                {
                    resourceInclude = new ResourceInclude(new Uri("avares://SIT.Manager.Avalonia/Localization/en-US.axaml"))
                    {
                        Source = new Uri("avares://SIT.Manager.Avalonia/Localization/en-US.axaml")
                    };
                }
            }

            string result = "not found";
            if (resourceInclude.TryGetResource(key, null, out object? translation))
            {
                if (translation != null)
                {
                    result = (string)translation;
                    for (int i = 0; i < replaces.Length; i++)
                    {
                        result = result.Replace($"%{i + 1}", replaces[i]);
                    }
                }
            }
            return result;
        }
    }
}