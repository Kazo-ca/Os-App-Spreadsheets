using Kazo.Os.AppSdk.Craft;

namespace Kazo.Spreadsheets.Hosting;

/// <summary>
/// The chords the iframe gives back to the window instead of GridCraft: the OS chords, Ctrl+N for
/// a new window, and Save As (doc/en/plan.md, decision 5, Benoit's Q1 = A).
/// </summary>
/// <remarks>
/// GridCraft always tells the host <c>saveAs: false</c>, so its own F12 and Ctrl+Shift+S would
/// rewrite the bound file. The window takes these chords and runs Save As itself.
/// </remarks>
public static class SpreadsheetKeys
{
    /// <summary>Gets the F12 chord (Save As).</summary>
    public static ReservedKey SaveAs { get; } = ReservedKeys.Parse("F12", ReservedKeyTarget.App);

    /// <summary>Gets the Ctrl+Shift+S chord (Save As).</summary>
    public static ReservedKey SaveAsShifted { get; } = ReservedKeys.Parse("Ctrl+Shift+S", ReservedKeyTarget.App);

    /// <summary>Gets every chord the window reserves.</summary>
    public static IReadOnlyList<ReservedKey> All { get; } = [.. ReservedKeys.All, SaveAs, SaveAsShifted];

    /// <summary>
    /// Reads a typed chord. The proxy reports the key as the browser gave it (<c>S</c> with Shift),
    /// so the comparison ignores the case of the key and takes the physical code when it matches.
    /// </summary>
    /// <param name="pressed">The chord the proxy reported.</param>
    /// <returns>What the window does.</returns>
    public static KeyAction ActionOf(ReservedKey pressed)
    {
        ArgumentNullException.ThrowIfNull(pressed);
        if (Matches(pressed, ReservedKeys.NewWindow))
        {
            return KeyAction.NewWindow;
        }

        return Matches(pressed, SaveAs) || Matches(pressed, SaveAsShifted) ? KeyAction.SaveAs : KeyAction.None;
    }

    private static bool Matches(ReservedKey pressed, ReservedKey chord)
    {
        if (pressed.Ctrl != chord.Ctrl || pressed.Alt != chord.Alt || pressed.Shift != chord.Shift || pressed.Meta != chord.Meta)
        {
            return false;
        }

        bool sameKey = pressed.Key is { } key && chord.Key is { } wanted
            && string.Equals(key, wanted, StringComparison.OrdinalIgnoreCase);
        bool sameCode = pressed.Code is { } code && chord.Code is { } wantedCode
            && string.Equals(code, wantedCode, StringComparison.Ordinal);
        return sameKey || sameCode;
    }
}

/// <summary>What a chord typed in GridCraft asks the window for.</summary>
public enum KeyAction
{
    /// <summary>Nothing the window knows.</summary>
    None,

    /// <summary>A new, empty Kazo Spreadsheets window.</summary>
    NewWindow,

    /// <summary>Save the workbook under a new name.</summary>
    SaveAs,
}
