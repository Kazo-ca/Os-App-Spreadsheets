namespace Kazo.Spreadsheets;

/// <summary>
/// The GridCraft web build this app carries in <c>wwwroot/gridcraft/</c>, described by its
/// provenance file <c>gridcraft.json</c> (written by <c>tools/vendor-gridcraft.sh</c>, checked by
/// the tests).
/// </summary>
public static class GridCraftBuild
{
    /// <summary>
    /// The GridCraft version of the build. Kept equal to the <c>version</c> of
    /// <c>wwwroot/gridcraft/gridcraft.json</c> by a test, so the window can show it without reading
    /// the file at run time.
    /// </summary>
    public const string Version = "0.19.1";

    /// <summary>Folder of the build under the app's static files (<c>_content/Kazo.Spreadsheets/</c>).</summary>
    public const string Folder = "gridcraft";
}
