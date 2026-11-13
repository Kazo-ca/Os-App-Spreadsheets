using Kazo.Os.App.Abstractions;
using Kazo.Os.AppSdk;
using Kazo.Os.AppSdk.Apps;
using Kazo.Os.AppSdk.Blazor;
using Kazo.Os.AppSdk.Craft;
using Kazo.Os.AppSdk.IO;
using Kazo.Spreadsheets.Documents;
using Kazo.Spreadsheets.Hosting;

namespace Kazo.Spreadsheets;

/// <summary>
/// What the window does with the module's proxy (doc/en/plan.md, work item 2): GridCraft's file
/// picker, the chords the window keeps, the user's GridCraft settings, and the OS theme and language.
/// </summary>
/// <remarks>
/// The proxy's messages arrive through a <c>[JSInvokable]</c> callback, outside the scope the OS opens
/// for app code: each handler that reaches the SDK (files, launching) enters the app's context itself.
/// </remarks>
public sealed partial class SpreadsheetsWindow
{
    /// <summary>
    /// How long GridCraft's settings writes are gathered before the file is written: counted from
    /// the first unsaved write, so a GridCraft that keeps writing still reaches the file.
    /// </summary>
    public static readonly TimeSpan SettingsWriteDelay = TimeSpan.FromMilliseconds(500);

    private WebStorageFile? _settingsFile;
    private CancellationTokenSource? _settingsWrite;

    // Reads the user's GridCraft settings (gridcraft.ui, gridcraft.prefs) before the iframe starts:
    // GridCraft reads them as it starts. They live in ~/.config/ca.kazo.spreadsheets/.
    private async Task<IReadOnlyDictionary<string, string>?> LoadSettingsAsync()
    {
        _settingsFile ??= new WebStorageFile(AppContext.HomeDirectory, AppId);
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        try
        {
            return new Dictionary<string, string>(await _settingsFile.LoadAsync(AppContext.CancellationToken), StringComparer.Ordinal);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    // The proxy's events of one frame (subscribed in StartFrameAsync, removed in StopFrameAsync).
    private void ConnectProxy(CraftFrame frame, bool connect)
    {
        if (connect)
        {
            frame.KeyPressed += OnKeyPressedAsync;
            frame.PickRequested += OnPickRequestedAsync;
            frame.SettingChanged += OnSettingChangedAsync;
            frame.SchemeChanged += OnSchemeChangedAsync;
        }
        else
        {
            frame.KeyPressed -= OnKeyPressedAsync;
            frame.PickRequested -= OnPickRequestedAsync;
            frame.SettingChanged -= OnSettingChangedAsync;
            frame.SchemeChanged -= OnSchemeChangedAsync;
        }
    }

    // GridCraft is listening: it takes the OS theme (ui.set dark) and the user's language
    // (app.language.set), which GridCraft has instead of WordCraft's ui.theme and ui.language. A
    // refused command leaves GridCraft as it was: the workbook matters more.
    private async Task ApplyThemeAndLanguageAsync(CraftFrame frame)
    {
        await OnSchemeChangedAsync(await frame.GetColorSchemeAsync());
        await frame.RequestAsync(
            "engine.execute",
            new { command = "app.language.set", @params = new { language = GridCraftLanguage.Pick(AppContext.Culture) } },
            cancellationToken: AppContext.CancellationToken);
    }

    private async Task OnSchemeChangedAsync(string scheme)
    {
        if (_frame is { } frame)
        {
            await frame.RequestAsync("ui.set", new { dark = scheme != "light" }, cancellationToken: AppContext.CancellationToken);
        }
    }

    // Ctrl+N: a new Kazo Spreadsheets window, as File › New does in a desktop spreadsheet.
    // F12 and Ctrl+Shift+S: Save As (decision 5).
    private Task OnKeyPressedAsync(ReservedKey key) => SpreadsheetKeys.ActionOf(key) switch
    {
        KeyAction.NewWindow => InvokeAsync(LaunchNewWindowAsync),
        KeyAction.SaveAs => InvokeAsync(SaveAsAsync),
        _ => Task.CompletedTask,
    };

    // A new, empty Kazo Spreadsheets window.
    private async Task LaunchNewWindowAsync()
    {
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        try
        {
            await KazoApps.LaunchAsync(AppId, [], AppContext.CancellationToken);
        }
        catch (Exception exception) when (exception is KazoAppLaunchException or UnauthorizedAccessException)
        {
            await ShowProblemAsync(L["NewWindowFailed", exception.Message]);
        }
    }

    // GridCraft opened its file picker: the OS dialog instead. Its two pickers are File › Open
    // (workbooks and text) and Insert › Picture. A workbook is not handed to GridCraft: it hears
    // "cancelled", and the file opens through OpenInNewWindowAsync (here only for an untouched
    // blank workbook), so every workbook of a window has its file. A picture is read and handed over.
    private Task OnPickRequestedAsync(PickRequest request) => InvokeAsync(async () =>
    {
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        CraftFrame? frame = _frame;
        if (frame is null)
        {
            return;
        }

        bool workbook = request.AcceptsAny(SpreadsheetFormats.OpenExtensions);
        IReadOnlyList<PickedFile>? files = null;
        string? document = null;
        try
        {
            OpenFileDialogResult choice = await OpenFileAsync(
                new OpenFileDialogRequest
                {
                    Title = L[workbook ? "PickWorkbookTitle" : "PickPictureTitle"],
                    InitialDirectory = VirtualPath.Parse(AppContext.HomeDirectory),
                    AllowMultiple = request.Multiple && !workbook,
                    Filters = FiltersOf(request, workbook),
                },
                AppContext.CancellationToken);
            if (choice.Status == FileDialogStatus.Selected && choice.Resources.Count > 0)
            {
                if (workbook)
                {
                    document = choice.Resources[0].Path.Value;
                }
                else
                {
                    files = await ReadPickedAsync(choice.Resources);
                }
            }
        }
        finally
        {
            // GridCraft waits on its picker until it hears back: it always does, even on a failure.
            await frame.AnswerPickAsync(request.Id, files);
        }

        // After the answer: GridCraft's picker is closed before a workbook may open in this window.
        if (document is not null)
        {
            await OpenInNewWindowAsync(document);
        }
    });

    private async Task<IReadOnlyList<PickedFile>?> ReadPickedAsync(IReadOnlyList<SelectedFileResource> resources)
    {
        List<PickedFile> files = [];
        foreach (SelectedFileResource resource in resources)
        {
            string path = resource.Path.Value;
            string name = KazoPath.GetFileName(path) ?? path;
            try
            {
                byte[] bytes = await KazoFile.ReadAllBytesAsync(path, AppContext.CancellationToken);
                files.Add(new PickedFile(name, string.Empty, bytes));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                await ShowProblemAsync(L["ReadFailed", name, exception.Message]);
                return null;
            }
        }

        return files;
    }

    // The extensions GridCraft accepts, then every file (GridCraft checks what it gets).
    private List<FileDialogFilter> FiltersOf(PickRequest request, bool workbook)
    {
        List<FileDialogFilter> filters = [];
        if (request.Extensions.Count > 0)
        {
            filters.Add(new FileDialogFilter(L[workbook ? "WorkbooksFilter" : "PicturesFilter"], request.Extensions, []));
        }

        filters.Add(new FileDialogFilter(L["AllFiles"], [], []));
        return filters;
    }

    // GridCraft wrote a setting: the user's file is written a moment later, with every write
    // gathered until then. A write already waiting takes the new value: it saves the file as it
    // is when it runs.
    private Task OnSettingChangedAsync(string key, string? value)
    {
        if (_settingsFile is null || !_settingsFile.Set(key, value) || _settingsWrite is not null)
        {
            return Task.CompletedTask;
        }

        CancellationTokenSource write = CancellationTokenSource.CreateLinkedTokenSource(AppContext.CancellationToken);
        _settingsWrite = write;
        _ = WriteSettingsLaterAsync(write);
        return Task.CompletedTask;
    }

    private async Task WriteSettingsLaterAsync(CancellationTokenSource write)
    {
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        CancellationToken cancellationToken = write.Token;
        try
        {
            await Task.Delay(SettingsWriteDelay, cancellationToken);
            // From here a new write schedules its own save, since this one may already have read the values.
            if (ReferenceEquals(_settingsWrite, write))
            {
                _settingsWrite = null;
                write.Dispose();
            }

            await _settingsFile!.SaveAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The window closed: FlushSettingsAsync writes what was waiting.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The settings stay in this window's GridCraft; the next write tries again.
        }
    }

    // Writes what is still waiting when the window closes.
    private async Task FlushSettingsAsync()
    {
        if (_settingsWrite is null || _settingsFile is null)
        {
            return;
        }

        _settingsWrite.Cancel();
        _settingsWrite.Dispose();
        _settingsWrite = null;
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        try
        {
            await _settingsFile.SaveAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // Closing: nothing more to do.
        }
    }

    private async Task ShowProblemAsync(string message)
    {
        try
        {
            await MessageBox(L["ProblemTitle"], message, MessageBoxType.Ok, MessageBoxIcon.Error, AppContext.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The window closed.
        }
    }
}
