using System.Globalization;
using MacWinUI.Core.Localization;
using Xunit;

namespace MacWinUI.Core.Tests.Localization;

public sealed class AppLanguageResolverTests
{
    [Theory]
    [InlineData("zh-CN")]
    [InlineData("zh-Hans")]
    [InlineData("zh-TW")]
    public void SystemLanguage_MapsEveryChineseCultureToSimplifiedChinese(string cultureName)
    {
        var result = AppLanguageResolver.Resolve(
            AppLanguage.System,
            CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(AppLanguageResolver.SimplifiedChineseCultureName, result.Name);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void SystemLanguage_MapsUnsupportedCulturesToEnglish(string cultureName)
    {
        var result = AppLanguageResolver.Resolve(
            AppLanguage.System,
            CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(AppLanguageResolver.EnglishCultureName, result.Name);
    }

    [Fact]
    public void ExplicitLanguage_OverridesSystemCulture()
    {
        Assert.Equal(
            AppLanguageResolver.EnglishCultureName,
            AppLanguageResolver.Resolve(
                AppLanguage.English,
                CultureInfo.GetCultureInfo("zh-CN")).Name);
        Assert.Equal(
            AppLanguageResolver.SimplifiedChineseCultureName,
            AppLanguageResolver.Resolve(
                AppLanguage.SimplifiedChinese,
                CultureInfo.GetCultureInfo("en-US")).Name);
    }
}
