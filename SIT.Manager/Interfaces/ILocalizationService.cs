using System;
using System.Collections.Generic;
using System.Globalization;

namespace SIT.Manager.Interfaces;

public interface ILocalizationService
{
    CultureInfo DefaultLocale { get; }

    event EventHandler<EventArgs>? LocalizationChanged;

    /// <summary>
    /// Function that loads the Available Localizations when program starts.
    /// </summary>
    List<CultureInfo> GetAvailableLocalizations();
    /// <summary>
    /// Changes the localization based on your culture info. This specific function changes it inside of Settings. And mainly changes all dynamic Resources in pages.
    /// </summary>
    /// <param name="cultureInfo">the current culture</param>
    void Translate(CultureInfo cultureInfo);
    /// <summary>
    /// Changes the localization in .cs files that contains strings that you cannot change inside the page.
    /// Functions contain neat parameters that help modify source strings, like in C#, but inside a Resource file.
    /// Example will be: Your path is %1. %1 → path. | Output: Your path is: C:\Users\...
    /// where % is the definition of parameter, and 1…n is the hierarchy of parameters passed to the function.
    /// </summary>
    /// <param name="key">string that you are accessing in Localization\*culture-info*.axaml file</param>
    /// <param name="replaces">parameters in hierarchy, example: %1, %2, %3, "10", "20, "30" | output: 10, 20, 30</param>
    string TranslateSource(string key, params string[] replaces);
}
