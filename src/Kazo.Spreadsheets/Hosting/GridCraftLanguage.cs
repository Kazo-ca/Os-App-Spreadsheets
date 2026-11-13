using System.Globalization;

namespace Kazo.Spreadsheets.Hosting;

/// <summary>
/// The interface language GridCraft gets from the window (<c>app.language.set</c>): the Kazo
/// user's language when GridCraft has it, otherwise English.
/// </summary>
/// <remarks>
/// <see cref="Kazo.Os.AppSdk.Craft.CraftLanguage.Pick"/> maps to WordCraft's codes
/// (<c>zh-hans</c>, <c>pt-br</c>, <c>sr-latn</c>); GridCraft 0.19.1 takes two-letter codes and has
/// no command that lists them, so the list is here. Chinese, Japanese and Korean stay English: the
/// web build has no CJK font, so their labels would not draw (GridCraft does the same with the
/// browser language).
/// </remarks>
public static class GridCraftLanguage
{
    /// <summary>The language GridCraft falls back to.</summary>
    public const string English = "en";

    /// <summary>Gets the codes <c>app.language.set</c> takes, without the ones the web build cannot draw.</summary>
    public static IReadOnlyList<string> Supported { get; } =
        ["en", "es", "el", "ru", "pt", "de", "cs", "fr", "sr", "tr", "pl", "it", "uk", "vi", "nl", "ro"];

    /// <summary>The GridCraft code for a culture.</summary>
    /// <param name="culture">The Kazo user's culture.</param>
    /// <returns>A code of <see cref="Supported"/>.</returns>
    public static string Pick(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        string code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        return Supported.Contains(code) ? code : English;
    }
}
