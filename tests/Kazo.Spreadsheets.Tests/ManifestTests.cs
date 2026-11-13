using Kazo.Os.App.Abstractions;

namespace Kazo.Spreadsheets.Tests;

/// <summary>The app's manifest class, the source of the generated manifest.json (doc/en/plan.md, decision 1).</summary>
public sealed class ManifestTests
{
    private readonly SpreadsheetsApp _app = new();

    [Fact]
    public void The_app_is_the_Kazo_Spreadsheets_window_app()
    {
        Assert.Equal("ca.kazo.spreadsheets", _app.Id);
        Assert.Equal("Kazo Spreadsheets", _app.Name);
        Assert.Equal("pub.kazo", _app.PublisherId);
        Assert.Equal("productivity", _app.Category);
        Assert.Equal(AppKind.Window, _app.Kind);
        Assert.Equal(typeof(SpreadsheetsWindow), _app.EntryPoint);
    }

    [Fact]
    public void Several_windows_can_run_at_once()
    {
        Assert.False(_app.SingleInstancePerUser);
    }

    [Fact]
    public void The_window_opens_at_1100_by_760_and_keeps_GridCraft_minimum()
    {
        WindowOptions window = Assert.IsType<WindowOptions>(_app.Window);
        Assert.Equal(1100, window.DefaultWidth);
        Assert.Equal(760, window.DefaultHeight);
        Assert.Equal(760, window.MinWidth);
        Assert.Equal(480, window.MinHeight);
    }

    [Fact]
    public void The_icon_is_declared_as_the_app_icon()
    {
        Assert.Equal(["icons/app.svg"], _app.Icons);
        AssetManifest icon = Assert.Single(_app.Assets);
        Assert.Equal("icons/app.svg", icon.Path);
        Assert.Equal("app-icon", icon.Role);
    }
}
