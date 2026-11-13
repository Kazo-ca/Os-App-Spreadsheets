using System.Globalization;
using Kazo.Os.AppSdk.Craft;

namespace Kazo.Spreadsheets.Hosting;

/// <summary>
/// The interface language GridCraft gets from the window (<c>app.language.set</c>): the Kazo
/// user's language when GridCraft has it, otherwise English.
/// </summary>
/// <remarks>
/// The choice is the module's <see cref="CraftLanguage.Pick"/>, which falls back to the two-letter
/// code (<c>pt</c>, <c>sr</c>) when the app offers no other. GridCraft 0.19.1 takes two-letter codes
/// and has no command that lists them, so the list is here. Chinese, Japanese and Korean stay English: the
/// web build has no CJK font, so their labels would not draw (GridCraft does the same with the
/// browser language).
/// </remarks>
public static class GridCraftLanguage
{
    /// <summary>The language GridCraft falls back to.</summary>
    public const string English = CraftLanguage.English;

    /// <summary>Gets the codes <c>app.language.set</c> takes, without the ones the web build cannot draw.</summary>
    public static IReadOnlyList<string> Supported { get; } =
        ["en", "es", "el", "ru", "pt", "de", "cs", "fr", "sr", "tr", "pl", "it", "uk", "vi", "nl", "ro"];

    /// <summary>The GridCraft code for a culture.</summary>
    /// <param name="culture">The Kazo user's culture.</param>
    /// <returns>A code of <see cref="Supported"/>.</returns>
    public static string Pick(CultureInfo culture) => CraftLanguage.Pick(culture, Supported);
}
