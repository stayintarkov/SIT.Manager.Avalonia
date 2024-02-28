using Avalonia.Markup.Xaml.Styling;
using System.Linq;
using System;

namespace SIT.Manager.Avalonia.Models
{
    public static class LocalizationConfig
    {
        public enum Languages
        {
            English,
            Ukrainian,
            Russian
        }
        /// <summary>
        /// Changes translation. Depending on what file was called.
        /// </summary>
        /// <param name="targetLanguage">targetLanguage for example just English. no en-US</param>
        public static void Translate(string targetLanguage)
        {
            var translations = App.Current.Resources.MergedDictionaries.OfType<ResourceInclude>().FirstOrDefault(x => x.Source?.OriginalString?.Contains("/Localization/") ?? false);

            if (translations != null) App.Current.Resources.MergedDictionaries.Remove(translations);

            App.Current.Resources.MergedDictionaries.Add(
                new ResourceInclude(new Uri($"avares://SIT.Manager.Avalonia/Localization/{targetLanguage}.axaml"))
                {
                    Source = new Uri($"avares://SIT.Manager.Avalonia/Localization/{targetLanguage}.axaml")
                });
        }

        /// <summary>
        /// Used to show the translated strings when you are starting the application.
        /// </summary>
        /// <param name="languageID">number or enum number. to update the translation, useful when you are starting the application and using config loading your language.</param>
        public static void UpdateTranslationStrings(Languages languageID)
        {
            switch (languageID)
            {
                case Languages.English:
                    Translate("English");
                    break;
                case Languages.Ukrainian:
                    Translate("Ukrainian");
                    break;
                case Languages.Russian:
                    Translate("Russian");
                    break;
                default:
                    Translate("English");
                    break;
            }
        }
    }
}