using Kazo.Os.AppSdk.Craft;
using Kazo.Spreadsheets.Hosting;

namespace Kazo.Spreadsheets.Tests;

/// <summary>The chords the window takes from GridCraft (decision 5, Q1 = A).</summary>
public sealed class SpreadsheetKeysTests
{
    // What the proxy reports: the key as the browser gives it, the physical code, the modifiers.
    private static ReservedKey Typed(string key, string code, bool ctrl = false, bool shift = false, bool alt = false) =>
        new(key, code, ctrl, alt, shift, Meta: false, ReservedKeyTarget.App);

    [Fact]
    public void The_window_reserves_the_OS_chords_new_window_and_save_as()
    {
        Assert.Superset(ReservedKeys.All.ToHashSet(), SpreadsheetKeys.All.ToHashSet());
        Assert.Contains(SpreadsheetKeys.SaveAs, SpreadsheetKeys.All);
        Assert.Contains(SpreadsheetKeys.SaveAsShifted, SpreadsheetKeys.All);
        Assert.All([SpreadsheetKeys.SaveAs, SpreadsheetKeys.SaveAsShifted], key => Assert.Equal(ReservedKeyTarget.App, key.Target));
    }

    [Fact]
    public void F12_is_save_as()
    {
        Assert.Equal(KeyAction.SaveAs, SpreadsheetKeys.ActionOf(Typed("F12", "F12")));
    }

    [Theory]
    [InlineData("S")]
    [InlineData("s")]
    public void Ctrl_Shift_S_is_save_as_whatever_the_case_of_the_key(string key)
    {
        Assert.Equal(KeyAction.SaveAs, SpreadsheetKeys.ActionOf(Typed(key, "KeyS", ctrl: true, shift: true)));
    }

    [Fact]
    public void Ctrl_Shift_S_on_another_layout_is_found_by_its_code()
    {
        Assert.Equal(KeyAction.SaveAs, SpreadsheetKeys.ActionOf(Typed("Ś", "KeyS", ctrl: true, shift: true)));
    }

    [Fact]
    public void Ctrl_N_is_a_new_window()
    {
        Assert.Equal(KeyAction.NewWindow, SpreadsheetKeys.ActionOf(Typed("n", "KeyN", ctrl: true)));
    }

    [Theory]
    [InlineData("s", "KeyS", true, false)]
    [InlineData("F12", "F12", false, true)]
    [InlineData("n", "KeyN", true, true)]
    [InlineData("a", "KeyA", true, false)]
    public void Other_chords_do_nothing(string key, string code, bool ctrl, bool shift)
    {
        Assert.Equal(KeyAction.None, SpreadsheetKeys.ActionOf(Typed(key, code, ctrl, shift)));
    }
}
