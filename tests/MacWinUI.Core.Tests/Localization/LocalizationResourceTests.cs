using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace MacWinUI.Core.Tests.Localization;

public sealed partial class LocalizationResourceTests
{
    [Fact]
    public void EnglishAndChineseResources_HaveMatchingNonEmptyKeysAndPlaceholders()
    {
        var english = Load("Strings.en-US.xaml");
        var chinese = Load("Strings.zh-CN.xaml");

        Assert.Equal(english.Keys.Order(), chinese.Keys.Order());
        foreach (var key in english.Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(english[key]), $"English resource {key} is empty.");
            Assert.False(string.IsNullOrWhiteSpace(chinese[key]), $"Chinese resource {key} is empty.");
            Assert.Equal(
                PlaceholderIndexes(english[key]),
                PlaceholderIndexes(chinese[key]));
        }
    }

    private static Dictionary<string, string> Load(string fileName)
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Localization", fileName));
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var entries = document.Root!
            .Elements()
            .Select(element => new
            {
                Key = (string?)element.Attribute(xaml + "Key"),
                Value = element.Value
            })
            .Where(entry => entry.Key is not null)
            .ToArray();
        var duplicate = entries
            .GroupBy(entry => entry.Key!, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        Assert.Null(duplicate);
        return entries.ToDictionary(
            entry => entry.Key!,
            entry => entry.Value,
            StringComparer.Ordinal);
    }

    private static string[] PlaceholderIndexes(string value) => PlaceholderRegex()
        .Matches(value)
        .Select(match => match.Groups[1].Value)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [GeneratedRegex(@"\{(\d+)(?:[^}]*)\}")]
    private static partial Regex PlaceholderRegex();
}
