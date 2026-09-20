using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using MacWinUI.Core.Localization;
using Microsoft.Extensions.Logging;

namespace MacWinUI.App.Localization;

public sealed class AppLocalizationService : IAppLocalizationService
{
    private readonly ResourceDictionary _applicationResources;
    private readonly ResourceDictionary _englishFallback;
    private readonly ILogger<AppLocalizationService> _logger;
    private ResourceDictionary? _activeDictionary;

    public AppLocalizationService(
        ResourceDictionary applicationResources,
        ILogger<AppLocalizationService> logger)
    {
        _applicationResources = applicationResources;
        _logger = logger;
        _englishFallback = LoadDictionary(AppLanguageResolver.EnglishCultureName);
        EffectiveCulture = CultureInfo.GetCultureInfo(AppLanguageResolver.EnglishCultureName);
    }

    public AppLanguage SelectedLanguage { get; private set; } = AppLanguage.System;

    public CultureInfo EffectiveCulture { get; private set; }

    public event EventHandler? LanguageChanged;

    public void Apply(AppLanguage language)
    {
        if (!Enum.IsDefined(language))
        {
            language = AppLanguage.System;
        }

        var effectiveCulture = AppLanguageResolver.Resolve(language, CultureInfo.CurrentUICulture);
        ResourceDictionary dictionary;
        try
        {
            dictionary = effectiveCulture.Name == AppLanguageResolver.EnglishCultureName
                ? _englishFallback
                : LoadDictionary(effectiveCulture.Name);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not load language resources for {Culture}; English will be used.",
                effectiveCulture.Name);
            effectiveCulture = CultureInfo.GetCultureInfo(AppLanguageResolver.EnglishCultureName);
            dictionary = _englishFallback;
        }

        if (_activeDictionary is not null)
        {
            _applicationResources.MergedDictionaries.Remove(_activeDictionary);
        }

        _activeDictionary = dictionary;
        if (!_applicationResources.MergedDictionaries.Contains(dictionary))
        {
            _applicationResources.MergedDictionaries.Add(dictionary);
        }

        SelectedLanguage = language;
        EffectiveCulture = effectiveCulture;
        var xmlLanguage = XmlLanguage.GetLanguage(effectiveCulture.IetfLanguageTag);
        if (Application.Current is not null)
        {
            foreach (Window window in Application.Current.Windows)
            {
                window.Language = xmlLanguage;
                window.InvalidateMeasure();
            }
        }
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_activeDictionary?[key] is string localized && !string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        if (_englishFallback[key] is string fallback && !string.IsNullOrWhiteSpace(fallback))
        {
            _logger.LogWarning("Language resource {ResourceKey} is missing from the active language.", key);
            return fallback;
        }

        _logger.LogWarning("Language resource {ResourceKey} is missing from all languages.", key);
        return key;
    }

    public string Format(string key, params object[] arguments) =>
        string.Format(EffectiveCulture, GetString(key), arguments);

    private static ResourceDictionary LoadDictionary(string cultureName) => new()
    {
        Source = new Uri($"/MacWinUI.App;component/Resources/Strings.{cultureName}.xaml", UriKind.RelativeOrAbsolute)
    };
}
