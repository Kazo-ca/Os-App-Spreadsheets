using Kazo.Os.App.Abstractions;
using Kazo.Spreadsheets.Documents;

namespace Kazo.Spreadsheets;

/// <summary>
/// The app's manifest (doc/en/plan.md, decision 1). The build generates <c>manifest.json</c> from
/// this class (Kazo.Os.App.Build); it is never instantiated at run time.
/// </summary>
/// <remarks>
/// Work item 1 declared the base: identity, the window and its size, several windows at once.
/// Work item 2 adds the document formats (decision 6, <see cref="SpreadsheetFormats"/>), the OS
/// dialogs and the capabilities they need, with the window on the Kazo.Os.AppSdk.Craft module.
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
    /// <remarks>
    /// The OS Open and Save dialogs, the user's files (a file chosen in a dialog outside the home
    /// folder is reached through the dialog's grant), <c>apps.launch</c> to open a file in another
    /// window, and the private folder where GridCraft's settings live
    /// (<c>~/.config/ca.kazo.spreadsheets/</c>).
    /// </remarks>
    public override IReadOnlyList<string> Capabilities =>
    [
        AppCapabilities.DialogFileOpen,
        AppCapabilities.DialogFileSave,
        AppCapabilities.FileSystemUserHomeRead,
        AppCapabilities.FileSystemUserHomeWrite,
        AppCapabilities.FileSystemPrivateRead,
        AppCapabilities.FileSystemPrivateWrite,
        AppCapabilities.AppsLaunch,
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Workbooks, CSV and TSV with <c>open</c> and <c>edit</c> (Kazo Spreadsheets becomes their
    /// app); text and JSON with <c>open</c> only, for "Open with" (decision 6, Q2 = A).
    /// </remarks>
    public override IReadOnlyList<FileHandler> FileHandlers => SpreadsheetFormats.FileHandlers;

    /// <inheritdoc />
    public override IReadOnlyList<string> Icons => ["icons/app.svg"];

    /// <inheritdoc />
    public override IReadOnlyList<AssetManifest> Assets =>
        [new("icons/app.svg", AssetKind.Image, new string('0', 64), "app-icon")];

    /// <inheritdoc />
    /// <remarks>
    /// Sizes are the outer size of the OS window (title bar included): 1100 × 760 by default, room
    /// for GridCraft's ribbon and about a dozen columns; at least GridCraft's minimum plus the frame.
    /// The window stays on its screen (ADR 0263 of Kazo OS, issue #4): moving it to another screen
    /// moves it into another browser document, which reloads GridCraft's iframe and loses the
    /// workbooks it holds.
    /// </remarks>
    public override WindowOptions? Window => new()
    {
        DefaultWidth = 1100,
        DefaultHeight = 760,
        MinWidth = ContentMinWidth + WindowChromeWidth,
        MinHeight = ContentMinHeight + WindowChromeHeight,
        CanChangeScreen = false,
    };

    // Borders and title bar of an OS window (the tallest title bar of the Kazo OS themes, 2.35rem).
    private const int WindowChromeWidth = 2;
    private const int WindowChromeHeight = 40;
}
