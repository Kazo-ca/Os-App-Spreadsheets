using Kazo.Os.App.Abstractions;

namespace Kazo.Spreadsheets;

/// <summary>
/// The app's manifest (doc/en/plan.md, decision 1). The build generates <c>manifest.json</c> from
/// this class (Kazo.Os.App.Build); it is never instantiated at run time.
/// </summary>
/// <remarks>
/// Work item 1 declares the base: identity, the window and its size, several windows at once.
/// Work item 2 adds the document formats (decision 6), the OS dialogs and the capabilities they
/// need, when the window runs GridCraft through the Kazo.Os.AppSdk.Craft module.
/// </remarks>
[AppManifest]
public sealed class SpreadsheetsApp : AppDefinition
{
    /// <summary>
    /// Smallest usable width of GridCraft, in pixels: its ribbon groups collapse below that, and
    /// the name box, formula bar and a few columns still fit.
    /// </summary>
    public const int ContentMinWidth = 758;

    /// <summary>Smallest usable height of GridCraft, in pixels: ribbon, formula bar, some rows and the sheet tabs.</summary>
    public const int ContentMinHeight = 440;

    /// <inheritdoc />
    public override string Id => "ca.kazo.spreadsheets";

    /// <inheritdoc />
    public override string Name => "Kazo Spreadsheets";

    /// <inheritdoc />
    public override AppKind Kind => AppKind.Window;

    /// <inheritdoc />
    public override Type EntryPoint => typeof(SpreadsheetsWindow);

    /// <inheritdoc />
    public override string PublisherId => "pub.kazo";

    /// <inheritdoc />
    public override string Description => "A spreadsheet for .xlsx and .ods workbooks, built on GridCraft";

    /// <inheritdoc />
    public override string Category => "productivity";

    /// <inheritdoc />
    /// <remarks>The tag that publishes to the Kazo OS Store must be v + this version.</remarks>
    public override string Version => "0.1.0";

    /// <inheritdoc />
    /// <remarks>
    /// Several windows at once: each one runs its own GridCraft in its own iframe, because the
    /// GridCraft canvas id is fixed and allows one instance per HTML document (doc/en/plan.md).
    /// </remarks>
    public override bool SingleInstancePerUser => false;

    /// <inheritdoc />
    /// <remarks>None yet: work item 2 asks for the dialogs and the files it opens and saves.</remarks>
    public override IReadOnlyList<string> Capabilities => [];

    /// <inheritdoc />
    public override IReadOnlyList<string> Icons => ["icons/app.svg"];

    /// <inheritdoc />
    public override IReadOnlyList<AssetManifest> Assets =>
        [new("icons/app.svg", AssetKind.Image, new string('0', 64), "app-icon")];

    /// <inheritdoc />
    /// <remarks>
    /// Sizes are the outer size of the OS window (title bar included): 1100 × 760 by default, room
    /// for GridCraft's ribbon and about a dozen columns; at least GridCraft's minimum plus the frame.
    /// </remarks>
    public override WindowOptions? Window => new()
    {
        DefaultWidth = 1100,
        DefaultHeight = 760,
        MinWidth = ContentMinWidth + WindowChromeWidth,
        MinHeight = ContentMinHeight + WindowChromeHeight,
    };

    // Borders and title bar of an OS window (the tallest title bar of the Kazo OS themes, 2.35rem).
    private const int WindowChromeWidth = 2;
    private const int WindowChromeHeight = 40;
}
