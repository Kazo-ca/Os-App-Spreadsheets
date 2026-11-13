using System.Text.Json;
using Kazo.Os.AppSdk.Craft;

namespace Kazo.Spreadsheets.Documents;

/// <summary>
/// Reads the workbooks open in a GridCraft page from its <c>ui.inspect</c> answer, for the close
/// guard (doc/en/plan.md, "What the module needs to know about GridCraft").
/// </summary>
/// <remarks>
/// <see cref="OpenDocument.ListFrom"/> reads WordCraft's answer (<c>documents[].id</c> and
/// <c>path</c>); GridCraft gives <c>documents[{title, dirty}]</c> without ids, and its windows
/// <c>windows[{window, doc, index, title, dirty, active, hidden}]</c>, where <c>doc</c> is the
/// workbook's id and <c>index</c> its place in <c>documents</c>. A workbook shown in two GridCraft
/// windows is listed once. The answer comes from the iframe: anything malformed is skipped.
/// </remarks>
public static class GridCraftWorkbooks
{
    /// <summary>The workbooks of a <c>ui.inspect</c> answer, in GridCraft's order.</summary>
    /// <param name="inspect">The answer of <c>ui.inspect</c>.</param>
    /// <returns>
    /// One <see cref="OpenDocument"/> per workbook: <see cref="OpenDocument.Id"/> is GridCraft's
    /// workbook id (the <c>doc</c> of <c>engine.execute</c>), <see cref="OpenDocument.Name"/> the
    /// name its next save carries (<see cref="SpreadsheetFormats.SaveNameOf"/>), and
    /// <see cref="OpenDocument.Title"/> its title. Empty when the answer is not GridCraft's.
    /// </returns>
    public static IReadOnlyList<OpenDocument> ListFrom(JsonElement inspect)
    {
        if (inspect.ValueKind != JsonValueKind.Object
            || !inspect.TryGetProperty("documents", out JsonElement documents)
            || documents.ValueKind != JsonValueKind.Array
            || !inspect.TryGetProperty("windows", out JsonElement windows)
            || windows.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<OpenDocument> list = [];
        HashSet<long> seen = [];
        int count = documents.GetArrayLength();
        foreach (JsonElement window in windows.EnumerateArray())
        {
            if (window.ValueKind != JsonValueKind.Object
                || !TryGetLong(window, "doc", out long id)
                || !TryGetLong(window, "index", out long index)
                || index < 0 || index >= count)
            {
                continue;
            }

            bool active = window.TryGetProperty("active", out JsonElement flag) && flag.ValueKind == JsonValueKind.True;
            if (!seen.Add(id))
            {
                // Another window of a workbook already listed: it only says whether it is active.
                if (active)
                {
                    int at = list.FindIndex(document => document.Id == id);
                    list[at] = list[at] with { Active = true };
                }

                continue;
            }

            JsonElement document = documents[(int)index];
            string title = document.ValueKind == JsonValueKind.Object
                && document.TryGetProperty("title", out JsonElement text)
                && text.ValueKind == JsonValueKind.String
                ? text.GetString() ?? string.Empty
                : string.Empty;
            bool dirty = document.ValueKind == JsonValueKind.Object
                && document.TryGetProperty("dirty", out JsonElement changed)
                && changed.ValueKind == JsonValueKind.True;
            list.Add(new OpenDocument(id, SpreadsheetFormats.SaveNameOf(title), title, dirty, active));
        }

        return list;
    }

    private static bool TryGetLong(JsonElement element, string name, out long value)
    {
        value = 0;
        return element.TryGetProperty(name, out JsonElement number)
            && number.ValueKind == JsonValueKind.Number
            && number.TryGetInt64(out value);
    }
}
