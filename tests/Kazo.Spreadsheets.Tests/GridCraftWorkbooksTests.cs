using System.Text.Json;
using Kazo.Os.AppSdk.Craft;
using Kazo.Spreadsheets.Documents;

namespace Kazo.Spreadsheets.Tests;

/// <summary>GridCraft's <c>ui.inspect</c> answer, read for the close guard.</summary>
public sealed class GridCraftWorkbooksTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

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

        IReadOnlyList<OpenDocument> workbooks = GridCraftWorkbooks.ListFrom(inspect);

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
    [InlineData("""{ "documents": [ { "id": 1, "path": "/a.docx" } ], "activeDocument": 0 }""")]
    public void An_answer_that_is_not_GridCraft_s_lists_nothing(string text)
    {
        Assert.Empty(GridCraftWorkbooks.ListFrom(Json(text)));
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

        OpenDocument workbook = Assert.Single(GridCraftWorkbooks.ListFrom(inspect));
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

        IReadOnlyList<OpenDocument> unsaved = session.WithUnsavedWork(GridCraftWorkbooks.ListFrom(inspect));

        Assert.Equal([2L, 3L], unsaved.Select(workbook => workbook.Id));
    }
}
