using System.Globalization;
using Kazo.Spreadsheets.Hosting;

namespace Kazo.Spreadsheets.Tests;

/// <summary>The language the window gives GridCraft (<c>app.language.set</c>).</summary>
public sealed class GridCraftLanguageTests
{
    [Theory]
    [InlineData("fr-CA", "fr")]
    [InlineData("fr", "fr")]
    [InlineData("en-US", "en")]
    [InlineData("pt-BR", "pt")]
    [InlineData("de-DE", "de")]
    [InlineData("uk-UA", "uk")]
    [InlineData("pt-PT", "pt")]
    [InlineData("sr-Latn-RS", "sr")]
    [InlineData("sr-Cyrl-RS", "sr")]
    public void A_language_GridCraft_has_is_kept(string culture, string expected)
    {
        Assert.Equal(expected, GridCraftLanguage.Pick(CultureInfo.GetCultureInfo(culture)));
    }

    [Theory]
    [InlineData("ja-JP")]
    [InlineData("zh-CN")]
    [InlineData("zh-TW")]
    [InlineData("ko-KR")]
    [InlineData("sv-SE")]
    [InlineData("ar")]
    public void Other_languages_and_the_ones_the_web_build_cannot_draw_are_English(string culture)
    {
        Assert.Equal(GridCraftLanguage.English, GridCraftLanguage.Pick(CultureInfo.GetCultureInfo(culture)));
    }

    [Fact]
    public void The_invariant_culture_is_English()
    {
        Assert.Equal(GridCraftLanguage.English, GridCraftLanguage.Pick(CultureInfo.InvariantCulture));
    }
}
