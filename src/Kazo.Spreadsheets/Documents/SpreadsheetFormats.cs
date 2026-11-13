using Kazo.Os.App.Abstractions;
using Kazo.Os.AppSdk.Craft;

namespace Kazo.Spreadsheets.Documents;

/// <summary>
/// The files Kazo Spreadsheets knows (doc/en/plan.md, decision 6, Benoit's Q2 = A): the workbooks
/// and the delimited text files it becomes the app of, the files it only offers in "Open with", and
/// the copies GridCraft exports without changing the workbook's file.
/// </summary>
/// <remarks>
/// GridCraft saves a workbook as <c>.xlsx</c> only. The other formats it opens are read, and a
/// save of such a workbook carries another name (<c>Budget.xlsx</c>, or
/// <c>Budget (imported).xlsx</c> for <c>.ods</c>, <c>.xls</c> and <c>.xlsb</c>): the session then
/// asks for a path, so an <c>.ods</c> file is never rewritten with <c>.xlsx</c> bytes.
/// </remarks>
public static class SpreadsheetFormats
{
    /// <summary>The extension of the workbooks GridCraft saves.</summary>
    public const string WorkbookExtension = ".xlsx";

    /// <summary>Gets every format, with what Kazo Spreadsheets does with it.</summary>
    public static CraftDocumentFormats Formats { get; } = new(
    [
        // Default app (open and edit): the workbooks and the delimited text files (Q2 = A).
        new(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", IsDefault: true, IsCopy: false),
        new(".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12", IsDefault: true, IsCopy: false),
        new(".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12", IsDefault: true, IsCopy: false),
        new(".xls", "application/vnd.ms-excel", IsDefault: true, IsCopy: false),
        new(".ods", "application/vnd.oasis.opendocument.spreadsheet", IsDefault: true, IsCopy: false),
        new(".slk", "application/x-sylk", IsDefault: true, IsCopy: false),
        new(".csv", "text/csv", IsDefault: true, IsCopy: false),
        new(".tsv", "text/tab-separated-values", IsDefault: true, IsCopy: false),

        // "Open with" only: the OS editor stays their app.
        new(".txt", "text/plain", IsDefault: false, IsCopy: false),
        new(".json", "application/json", IsDefault: false, IsCopy: false),

        // Copies GridCraft exports (File › Export, Print to PDF): the workbook keeps its file.
        // CSV is listed above for opening; an exported .csv is a copy all the same, decided by
        // GridCraft's own name (see IsCopyName).
        new(".pdf", "application/pdf", IsDefault: false, IsCopy: true, Opens: false),
        new(".html", "text/html", IsDefault: false, IsCopy: true, Opens: false),
    ]);

    /// <summary>Gets the extensions the OS Open dialog proposes, in the order above.</summary>
    public static IReadOnlyList<string> OpenExtensions => Formats.OpenExtensions;

    /// <summary>Gets the file associations of the manifest.</summary>
    public static IReadOnlyList<FileHandler> FileHandlers => Formats.FileHandlers;

    /// <summary>
    /// The name GridCraft gives the save of a workbook titled <paramref name="title"/>: its stem
    /// and <c>.xlsx</c> (<c>file.saveBytes</c> builds it the same way).
    /// </summary>
    /// <param name="title">The workbook's title in GridCraft (<c>ui.inspect</c>).</param>
    /// <returns>For example <c>Budget.xlsx</c> for <c>Budget.csv</c>, <c>Book1.xlsx</c> for <c>Book1</c>.</returns>
    public static string SaveNameOf(string title)
    {
        string stem = Path.GetFileNameWithoutExtension(title ?? string.Empty);
        return (stem.Length == 0 ? "Book" : stem) + WorkbookExtension;
    }

    /// <summary>
    /// Whether a file GridCraft saves is a copy (an export) rather than the workbook: every save
    /// that is not <c>.xlsx</c>. GridCraft saves the workbook itself only as <c>.xlsx</c>, so a
    /// <c>.csv</c> it hands over is File › Export, even though <c>.csv</c> files open in it.
    /// </summary>
    /// <param name="name">The file name GridCraft gave.</param>
    /// <returns><see langword="true"/> for a copy.</returns>
    public static bool IsCopyName(string name) =>
        !string.Equals(CraftDocumentFormats.ExtensionOf(name), WorkbookExtension, StringComparison.OrdinalIgnoreCase);
}
