using System.Globalization;
using MacWinUI.Core.Localization;

namespace MacWinUI.App.Localization;

public interface IAppLocalizationService
{
    AppLanguage SelectedLanguage { get; }

    CultureInfo EffectiveCulture { get; }

    event EventHandler? LanguageChanged;

    void Apply(AppLanguage language);

    string GetString(string key);

    string Format(string key, params object[] arguments);
}
