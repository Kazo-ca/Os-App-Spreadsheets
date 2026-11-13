using Kazo.Os.AppSdk.Craft;
using Kazo.Spreadsheets.Documents;

namespace Kazo.Spreadsheets.Tests;

/// <summary>The formats of decision 6 and the names GridCraft gives its saves (doc/en/plan.md, step 0).</summary>
public sealed class SpreadsheetFormatsTests
{
    [Fact]
    public void The_Open_dialog_proposes_every_format_GridCraft_reads()
    {
        Assert.Equal(
            [".xlsx", ".xlsm", ".xlsb", ".xls", ".ods", ".slk", ".csv", ".tsv", ".txt", ".json"],
            SpreadsheetFormats.OpenExtensions);
    }

    [Theory]
    [InlineData("Book1", "Book1.xlsx")]
    [InlineData("Budget.csv", "Budget.xlsx")]
    [InlineData("Budget.xlsx", "Budget.xlsx")]
    [InlineData("Budget (imported).xlsx", "Budget (imported).xlsx")]
    [InlineData("", "Book.xlsx")]
    public void A_workbook_is_saved_under_its_stem_with_xlsx(string title, string expected)
    {
        Assert.Equal(expected, SpreadsheetFormats.SaveNameOf(title));
    }

    [Theory]
    [InlineData("Budget.xlsx", false)]
    [InlineData("Budget.XLSX", false)]
    [InlineData("Budget.pdf", true)]
    [InlineData("Budget.csv", true)]
    [InlineData("Budget.html", true)]
    [InlineData("Budget", true)]
    public void Every_save_that_is_not_xlsx_is_a_copy(string name, bool copy)
    {
        Assert.Equal(copy, SpreadsheetFormats.IsCopyName(name));
    }

    [Fact]
    public void An_ods_workbook_is_never_rewritten_with_xlsx_bytes()
    {
        DocumentSession session = new(SpreadsheetFormats.Formats, "Kazo Spreadsheets");
        session.Opened("Budget.ods", "/home/ada/Budget.ods");

        // GridCraft names an imported .ods "Budget (imported).xlsx": another name, so a path is asked.
        SaveDecision decision = session.Decide("Budget (imported).xlsx", saveAs: false);

        Assert.Equal(SaveAction.AskAndBind, decision.Action);
        Assert.Equal("/home/ada", decision.Folder);
    }

    [Fact]
    public void An_xlsx_workbook_is_rewritten_by_save_and_asked_again_by_save_as()
    {
        DocumentSession session = new(SpreadsheetFormats.Formats, "Kazo Spreadsheets");
        session.Opened("Budget.xlsx", "/home/ada/Budget.xlsx");

        Assert.Equal(SaveAction.Rewrite, session.Decide("Budget.xlsx", saveAs: false).Action);
        Assert.Equal(SaveAction.AskAndBind, session.Decide("Budget.xlsx", saveAs: true).Action);
    }

    [Theory]
    [InlineData("Budget.csv")]
    [InlineData("Budget.xlsm")]
    [InlineData("Budget.tsv")]
    public void A_file_of_another_format_asks_for_a_path_instead_of_being_rewritten(string file)
    {
        DocumentSession session = new(SpreadsheetFormats.Formats, "Kazo Spreadsheets");
        session.Opened(file, "/home/ada/" + file);

        SaveDecision decision = session.Decide(SpreadsheetFormats.SaveNameOf(file), saveAs: false);

        Assert.Equal(SaveAction.AskAndBind, decision.Action);
        Assert.Equal("Budget.xlsx", decision.Name);
    }

    [Fact]
    public void The_window_title_shows_the_bound_file()
    {
        DocumentSession session = new(SpreadsheetFormats.Formats, "Kazo Spreadsheets");
        session.Opened("Notes.csv", "/home/ada/Notes.csv");
        session.SetFrameDirty(true);

        Assert.Equal("*Notes.csv — Kazo Spreadsheets", session.Title("Untitled"));
    }
}
