using System.Text.Json;
using Kazo.Os.AppSdk.Craft;
using Kazo.Spreadsheets.Documents;

namespace Kazo.Spreadsheets.Tests;

/// <summary>
/// GridCraft's <c>ui.inspect</c> answer, read for the close guard by the module's
/// <see cref="OpenDocument.ListFrom(JsonElement, Func{string, string?})"/> with the save name of
/// <see cref="SpreadsheetFormats.SaveNameOf"/>.
/// </summary>
public sealed class WorkbookListTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    // What SpreadsheetsWindow.ConfirmCloseAsync does with the answer.
    private static IReadOnlyList<OpenDocument> Workbooks(JsonElement inspect) => OpenDocument.ListFrom(inspect, SpreadsheetFormats.SaveNameOf);

    [Fact]
    public void Each_workbook_is_listed_once_with_its_id_save_name_and_state()
    {
        JsonElement inspect = Json("""
            {
              "documents": [ { "title": "Book1", "dirty": false }, { "title": "Budget.csv", "dirty": true } ],
              "activeDocument": 1,
              "windows": [
                { "window": 1, "doc": 7, "index": 0, "title": "Book1", "dirty": false, "active": false, "hidden": false },
                { "window": 2, "doc": 9, "index": 1, "title": "Budget.csv:1", "dirty": true, "active": false, "hidden": false },
                { "window": 3, "doc": 9, "index": 1, "title": "Budget.csv:2", "dirty": true, "active": true, "hidden": false }
              ]
            }
            """);

        IReadOnlyList<OpenDocument> workbooks = Workbooks(inspect);

        Assert.Equal(
            [
                new OpenDocument(7, "Book1.xlsx", "Book1", Dirty: false, Active: false),
                new OpenDocument(9, "Budget.xlsx", "Budget.csv", Dirty: true, Active: true),
            ],
            workbooks);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{ "documents": [] }""")]
    [InlineData("""{ "documents": [ { "title": "Book1", "dirty": true } ] }""")]
    [InlineData("""{ "documents": [ { "title": "Book1", "dirty": true } ], "windows": {} }""")]
    public void An_answer_without_workbooks_and_their_windows_lists_nothing(string text)
    {
        Assert.Empty(Workbooks(Json(text)));
    }

    [Fact]
    public void Malformed_windows_are_skipped()
    {
        JsonElement inspect = Json("""
            {
              "documents": [ { "title": "Book1", "dirty": true } ],
              "windows": [
                { "doc": "7", "index": 0 },
                { "doc": 7, "index": 5 },
                { "doc": 7, "index": -1 },
                { "doc": 7 },
                "window",
                { "doc": 8, "index": 0, "active": true }
              ]
            }
            """);

        OpenDocument workbook = Assert.Single(Workbooks(inspect));
        Assert.Equal(8, workbook.Id);
        Assert.True(workbook.Dirty);
    }

    [Fact]
    public void Unsaved_work_is_the_dirty_workbooks_and_the_failed_saves()
    {
        DocumentSession session = new(SpreadsheetFormats.Formats, "Kazo Spreadsheets");
        session.SaveFailed(session.Decide("Book2.xlsx", saveAs: false));
        JsonElement inspect = Json("""
            {
              "documents": [ { "title": "Book1", "dirty": false }, { "title": "Book2", "dirty": false }, { "title": "Book3", "dirty": true } ],
              "windows": [
                { "doc": 1, "index": 0 }, { "doc": 2, "index": 1 }, { "doc": 3, "index": 2 }
              ]
            }
            """);

        IReadOnlyList<OpenDocument> unsaved = session.WithUnsavedWork(Workbooks(inspect));

        Assert.Equal([2L, 3L], unsaved.Select(workbook => workbook.Id));
    }
}
