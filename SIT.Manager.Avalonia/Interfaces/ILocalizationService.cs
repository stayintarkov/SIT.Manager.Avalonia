using SIT.Manager.Avalonia.Models;
using System.Globalization;

namespace SIT.Manager.Avalonia.Interfaces
{
    public interface ILocalizationService
    {
        LocalizationModel Localization { get; }
        /// <summary>
        /// Changes translation.
        /// </summary>
        void Translate(LocalizationModel localization, CultureInfo cultureInfo);
        string TranslateSource(string key, params string[] replaces);
    }
}