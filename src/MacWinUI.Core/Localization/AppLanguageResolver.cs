using System.Globalization;

namespace MacWinUI.Core.Localization;

public static class AppLanguageResolver
{
    public const string EnglishCultureName = "en-US";
    public const string SimplifiedChineseCultureName = "zh-CN";

    public static CultureInfo Resolve(AppLanguage language, CultureInfo systemCulture)
    {
        ArgumentNullException.ThrowIfNull(systemCulture);

        return language switch
        {
            AppLanguage.SimplifiedChinese => CultureInfo.GetCultureInfo(SimplifiedChineseCultureName),
            AppLanguage.English => CultureInfo.GetCultureInfo(EnglishCultureName),
            _ when systemCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) =>
                CultureInfo.GetCultureInfo(SimplifiedChineseCultureName),
            _ => CultureInfo.GetCultureInfo(EnglishCultureName)
        };
    }
}
