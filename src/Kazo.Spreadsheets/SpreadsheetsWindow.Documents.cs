using Kazo.Os.App.Abstractions;
using Kazo.Os.AppSdk;
using Kazo.Os.AppSdk.Apps;
using Kazo.Os.AppSdk.Blazor;
using Kazo.Os.AppSdk.Craft;
using Kazo.Os.AppSdk.IO;
using Kazo.Spreadsheets.Documents;
using Microsoft.JSInterop;

namespace Kazo.Spreadsheets;

/// <summary>
/// Opening, saving, Save As and closing (doc/en/plan.md, work item 2): the window reads and writes
/// the files with <see cref="KazoFile"/>, asks the OS dialogs, keeps the title and guards the close.
/// The module's <see cref="DocumentSession"/> decides where each save goes.
/// </summary>
/// <remarks>
/// GridCraft always says <c>saveAs: false</c> and saves its workbooks as <c>.xlsx</c> only
/// (doc/en/plan.md, step 0). Save As is therefore the window's: F12 and Ctrl+Shift+S arm it and
/// run GridCraft's <c>file.saveAs</c>, whose <c>save</c> the window then decides with
/// <c>saveAs: true</c> (decision 5, Q1 = A). Going through GridCraft's own save, rather than
/// <c>file.saveBytes</c>, keeps GridCraft's "unsaved changes" right. Every save that is not
/// <c>.xlsx</c> is an export (a copy), whatever the extension.
/// </remarks>
public sealed partial class SpreadsheetsWindow : IWindowCloseGuard
{
    /// <summary>The app's id, to start another Kazo Spreadsheets window on a file.</summary>
    private const string AppId = "ca.kazo.spreadsheets";

    /// <summary>The window's name for its app, at the end of its title.</summary>
    private const string AppName = "Kazo Spreadsheets";

    /// <summary>How long an armed Save As waits for GridCraft's save before it lapses.</summary>
    private static readonly TimeSpan SaveAsWindow = TimeSpan.FromSeconds(10);

    private readonly DocumentSession _session = new(SpreadsheetFormats.Formats, AppName);

    // Saves run one at a time: each may show a dialog, and the order is GridCraft's.
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    // Completed with the outcome of the next save (true = written), for the close guard.
    private TaskCompletionSource<bool>? _nextSave;

    // When the window asked GridCraft for a Save As: the next workbook save asks for a new path.
    private DateTimeOffset? _saveAsArmedAt;

    // A message over GridCraft: a failed write, a cancelled save, a copy written.
    private string? _notice;
    private bool _noticeIsError;

    /// <summary>Gets the title the OS shows for this window: <c>*name — Kazo Spreadsheets</c>.</summary>
    internal string DocumentTitle => _session.Title(L["Untitled"], _startup.State == FrameState.Running);

    /// <summary>
    /// Opens a file: in this window when it holds only an untouched blank workbook, otherwise in a
    /// new Kazo Spreadsheets window (a file opened from the OS never joins a window that has work).
    /// </summary>
    /// <param name="path">The file's absolute path.</param>
    /// <returns><see langword="true"/> when the file opened in this window.</returns>
    internal async Task<bool> OpenInNewWindowAsync(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        if (_frame is not null && _startup.State == FrameState.Running && _session.IsUntouchedBlank)
        {
            return await OpenFileHereAsync(path);
        }

        try
        {
            await KazoApps.LaunchAsync(AppId, [path], AppContext.CancellationToken);
        }
        catch (Exception exception) when (exception is KazoAppLaunchException or KazoUnauthorizedAccessException)
        {
            await ShowNoticeAsync(L["OpenFailed", CraftDocumentFormats.FileNameOf(path), exception.Message], error: true);
        }

        return false;
    }

    /// <summary>
    /// Asks before closing a window with work that is not in a file: GridCraft lists every workbook
    /// of its page (<c>ui.inspect</c>, read by the module's <see cref="OpenDocument.ListFrom(System.Text.Json.JsonElement, Func{string, string?})"/>, each named by <see cref="SpreadsheetFormats.SaveNameOf"/>); "Yes" saves each
    /// one and closes only once every write succeeded.
    /// </summary>
    /// <param name="cancellationToken">Cancels the close request.</param>
    /// <returns><see langword="true"/> to close.</returns>
    public async ValueTask<bool> ConfirmCloseAsync(CancellationToken cancellationToken = default)
    {
        if (_frame is not { } frame || _startup.State != FrameState.Running)
        {
            return true;
        }

        // A save still being written must end first: GridCraft already counts the workbook as
        // saved once it handed the bytes over, so closing now would lose the write.
        await _saveGate.WaitAsync(cancellationToken);
        _saveGate.Release();

        FrameCommandResult inspect = await frame.RequestAsync("ui.inspect", cancellationToken: cancellationToken);
        if (!inspect.Ok)
        {
            // GridCraft does not answer, so it cannot save either: closing without saving is the
            // only choice left, and it is the user's.
            if (!_session.IsDirty && _session.UnsavedNames.Count == 0)
            {
                return true;
            }

            MessageBoxDialogResult discard = await MessageBox(
                L["CloseTitle"], L["CloseUnanswered", _session.CurrentName ?? L["Untitled"]], MessageBoxType.YesNo, cancellationToken);
            return discard.Result == MessageBoxResult.Yes;
        }

        IReadOnlyList<OpenDocument> unsaved = _session.WithUnsavedWork(OpenDocument.ListFrom(inspect.Value, SpreadsheetFormats.SaveNameOf));
        if (unsaved.Count == 0)
        {
            return true;
        }

        string names = string.Join(", ", unsaved.Select(document => document.Title.Length > 0 ? document.Title : document.Name));
        MessageBoxDialogResult answer = await MessageBox(
            L["CloseTitle"], L["CloseUnsaved", names], MessageBoxType.YesNoCancel, cancellationToken);
        return answer.Result switch
        {
            MessageBoxResult.Yes => await SaveAllAsync(frame, unsaved, cancellationToken),
            MessageBoxResult.No => true,
            _ => false,
        };
    }

    // After `ready`: the window's file (after Retry), else the file asked for. GridCraft starts on
    // an empty workbook in host mode, so a window with no file has nothing more to do.
    private async Task PrepareDocumentAsync()
    {
        // Runs after `ready`, a [JSInvokable] callback: the app's scope must be entered for KazoFile.
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        string? path = (_session.CurrentName is { } current ? _session.PathOf(current) : null) ?? _request.Path;
        if (path is not null)
        {
            await OpenFileHereAsync(path);
        }

        await UpdateTitleAsync();
    }

    // Reads a file and sends it to GridCraft; a file that does not read leaves a message.
    private async Task<bool> OpenFileHereAsync(string path)
    {
        if (_frame is not { } frame)
        {
            return false;
        }

        string name = CraftDocumentFormats.FileNameOf(path);
        byte[] bytes;
        try
        {
            bytes = await KazoFile.ReadAllBytesAsync(path, AppContext.CancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            await ShowNoticeAsync(L["OpenFailed", name, exception.Message], error: true);
            return false;
        }

        if (!await frame.OpenAsync(name, bytes))
        {
            await ShowNoticeAsync(L["OpenFailed", name, L["NotConnected"]], error: true);
            return false;
        }

        // Bound under the file's own name. GridCraft saves the workbook as its stem with .xlsx: the
        // same name for an .xlsx file, which Save then rewrites; another name for any other format
        // (Budget.csv saves as Budget.xlsx), so Save asks for a path and never rewrites a .csv,
        // .ods or .xlsm file with .xlsx bytes.
        _session.Opened(name, path);
        await UpdateTitleAsync();
        return true;
    }

    private async Task OnDirtyChangedAsync(bool dirty)
    {
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        bool before = _session.IsDirty;
        _session.SetFrameDirty(dirty);
        if (before != _session.IsDirty)
        {
            await UpdateTitleAsync();
        }
    }

    /// <summary>
    /// Save As (F12, Ctrl+Shift+S): the next workbook GridCraft saves asks for a new path, then
    /// GridCraft saves the workbook shown.
    /// </summary>
    /// <returns>A task that completes once GridCraft took the command.</returns>
    private async Task SaveAsAsync()
    {
        if (_frame is not { } frame || _startup.State != FrameState.Running)
        {
            return;
        }

        _saveAsArmedAt = DateTimeOffset.UtcNow;
        FrameCommandResult started = await frame.RequestAsync(
            "engine.execute",
            new { command = "file.saveAs", @params = new { } },
            cancellationToken: AppContext.CancellationToken);
        if (!started.Ok)
        {
            _saveAsArmedAt = null;
            await ShowNoticeAsync(L["SaveFailed", _session.CurrentName ?? L["Untitled"], started.Error ?? string.Empty], error: true);
        }
    }

    // One file GridCraft saves: decided here and by the session, written here.
    private async Task<bool> OnSavingAsync(FrameSave save)
    {
        // Called from JavaScript ([JSInvokable]): the app's scope must be entered for KazoFile.
        using IDisposable scope = KazoExecutionContext.Enter(AppContext);
        await _saveGate.WaitAsync(AppContext.CancellationToken);
        bool written = false;
        try
        {
            written = await SaveAsync(save);
        }
        finally
        {
            _saveGate.Release();
            Interlocked.Exchange(ref _nextSave, null)?.TrySetResult(written);
            await UpdateTitleAsync();
        }

        return written;
    }

    private async Task<bool> SaveAsync(FrameSave save)
    {
        if (string.IsNullOrEmpty(save.Name))
        {
            return false;
        }

        SaveDecision decision = Decide(save.Name);
        try
        {
            return await CarryOutAsync(decision, save.Bytes);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Whatever went wrong (a dialog, the file system), the workbook must not pass for saved.
            _session.SaveFailed(decision);
            await ShowNoticeAsync(L["SaveFailed", save.Name, exception.Message], error: true);
            return false;
        }
    }

    // An export is a copy; a workbook is a Save As when the window armed one, else the session's rule.
    private SaveDecision Decide(string name)
    {
        if (SpreadsheetFormats.IsCopyName(name))
        {
            string? bound = _session.CurrentName is { } current ? _session.PathOf(current) : null;
            return new SaveDecision(SaveAction.AskForCopy, name, null, bound is null ? null : CraftDocumentFormats.FolderOf(bound));
        }

        bool saveAs = _saveAsArmedAt is { } armed && DateTimeOffset.UtcNow - armed <= SaveAsWindow;
        _saveAsArmedAt = null;
        return _session.Decide(name, saveAs);
    }

    // Asks for the path when the decision says so, writes the bytes, and records the outcome.
    private async Task<bool> CarryOutAsync(SaveDecision decision, byte[] bytes)
    {
        string? path = decision.AsksForPath ? await AskSavePathAsync(decision) : decision.Path;
        if (path is null)
        {
            _session.SaveFailed(decision);
            if (decision.SavesTheDocument)
            {
                await ShowNoticeAsync(L["SaveCancelled", decision.Name], error: false);
            }

            return false;
        }

        try
        {
            await KazoFile.WriteAllBytesAsync(path, bytes, AppContext.CancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _session.SaveFailed(decision);
            await ShowNoticeAsync(L["SaveFailed", CraftDocumentFormats.FileNameOf(path), exception.Message], error: true);
            return false;
        }

        _session.Saved(decision, path);
        if (decision.Action == SaveAction.AskForCopy)
        {
            await ShowNoticeAsync(L["CopySaved", CraftDocumentFormats.FileNameOf(path)], error: false);
            return true;
        }

        // GridCraft has no command to rename a workbook: its title keeps the old name, and its
        // next save, under that name, goes to the file just chosen (the binding).
        await ClearNoticeAsync();
        return true;
    }

    // The OS save dialog, in the workbook's folder, for the format GridCraft chose.
    private async Task<string?> AskSavePathAsync(SaveDecision decision)
    {
        string extension = CraftDocumentFormats.ExtensionOf(decision.Name);
        CraftDocumentFormat? format = SpreadsheetFormats.Formats.Find(decision.Name);
        SaveFileDialogResult result = await SaveFileAsync(new SaveFileDialogRequest
        {
            Title = decision.Action == SaveAction.AskForCopy ? L["SaveCopyTitle"] : L["SaveTitle"],
            InitialDirectory = DialogFolder(decision.Folder),
            SuggestedFileName = decision.Name,
            DefaultExtension = extension.Length > 0 ? extension : null,
            Filters = extension.Length > 0
                ? [new FileDialogFilter(L["FilterFormat", extension], [extension], format is null ? [] : [format.MediaType])]
                : [],
            ConfirmOverwrite = true,
        });
        return result.Status == FileDialogStatus.Selected && result.Resource is { } chosen
            ? CraftDocumentFormats.WithExtensionOf(chosen.Path.Value, decision.Name)
            : null;
    }

    // "Yes" in the close dialog: each workbook is shown and saved in turn; the window closes only
    // when all of them reached a file.
    private async Task<bool> SaveAllAsync(CraftFrame frame, IReadOnlyList<OpenDocument> documents, CancellationToken cancellationToken)
    {
        foreach (OpenDocument document in documents)
        {
            TaskCompletionSource<bool> outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _nextSave = outcome;

            // Shown first, then saved: GridCraft counts a save handed to the page as the save of
            // the workbook in its active window.
            FrameCommandResult started = await frame.RequestAsync(
                "engine.execute",
                new { command = "window.activate", @params = new { doc = document.Id } },
                cancellationToken: cancellationToken);
            if (started.Ok)
            {
                started = await frame.RequestAsync(
                    "engine.execute",
                    new { command = "file.save", @params = new { } },
                    cancellationToken: cancellationToken);
            }

            if (!started.Ok)
            {
                _nextSave = null;
                await ShowNoticeAsync(L["SaveFailed", document.Title, started.Error ?? string.Empty], error: true);
                return false;
            }

            if (!await outcome.Task.WaitAsync(cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private VirtualPath DialogFolder(string? folder) => VirtualPath.Parse(folder ?? AppContext.HomeDirectory);

    private async Task UpdateTitleAsync()
    {
        try
        {
            await SetWindowTitleAsync(DocumentTitle);
        }
        catch (Exception exception) when (exception is InvalidOperationException or JSDisconnectedException)
        {
            // The window is closing.
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task ShowNoticeAsync(string text, bool error)
    {
        _notice = text;
        _noticeIsError = error;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearNoticeAsync()
    {
        if (_notice is not null)
        {
            _notice = null;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void DismissNotice() => _notice = null;
}
