// End-to-end runs of opening, saving, Save As and closing (doc/en/plan.md, work item 2, decisions
// 5 and 6), inside the Kazo OS host: `npm run test:e2e` after `dotnet build Kazo.Spreadsheets.slnx -c Debug`.
//
// GridCraft is driven through its page API (window.gridcraft in the iframe) to fill cells and to
// ask for a save, as its ribbon and Ctrl+S do; F12 is typed on the keyboard; everything else goes
// through the OS: its save dialog, its message box, the window's title bar and close button, and
// the Terminal (`xdg-open` starts the app associated with the file type).
import { after, before, test } from 'node:test';
import assert from 'node:assert/strict';
import { openApp, runInTerminal, startHost } from './host.mjs';
import {
    HOME, STEP_TIMEOUT_MS, answerSaveDialog, bootPage, execute, expectTitle, frameOf, iframeOf, launchBrowser, request, waitForCell,
    waitForWindow, windowById,
} from './common.mjs';

let host;
let browser;

before(async () => {
    host = await startHost();
    browser = await launchBrowser();
});

after(async () => {
    await browser?.close();
    host?.stop();
});

/** Waits for the Terminal's output to show `text`. */
async function expectInTerminal(page, text) {
    await page.locator('.xterm-rows').last().getByText(text, { exact: false }).first().waitFor({ timeout: STEP_TIMEOUT_MS });
}

test('a new workbook is saved where the user says, saved again in place, and reopens from the OS', { timeout: 400_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await openApp(page, 'Kazo Spreadsheets');
    const id = await waitForWindow(page, 1);
    const frame = await frameOf(page, id);
    await expectTitle(page, id, 'Untitled — Kazo Spreadsheets');

    await execute(frame, 'cell.set', { cell: 'A1', input: 'Saved by Kazo Spreadsheets' });
    await expectTitle(page, id, '*Untitled — Kazo Spreadsheets');

    // The first save of a new workbook asks where to write it.
    await execute(frame, 'file.save');
    await answerSaveDialog(page, 'Save the workbook', 'budget.xlsx');
    await expectTitle(page, id, 'budget.xlsx — Kazo Spreadsheets');

    // The next save writes the same file, with no dialog.
    await execute(frame, 'cell.set', { cell: 'A2', input: '42' });
    await expectTitle(page, id, '*budget.xlsx — Kazo Spreadsheets');
    await execute(frame, 'file.save');
    await expectTitle(page, id, 'budget.xlsx — Kazo Spreadsheets');
    assert.equal(await page.locator('section.file-dialog').count(), 0);

    // `xdg-open` starts the app associated with .xlsx: a second window, on the file.
    await runInTerminal(page, `xdg-open ${HOME}/budget.xlsx`);
    const second = await waitForWindow(page, 2, [id]);
    await expectTitle(page, second, 'budget.xlsx — Kazo Spreadsheets');
    const reopened = await frameOf(page, second);
    await waitForCell(reopened, 'A1', 'Saved by Kazo Spreadsheets');
    await waitForCell(reopened, 'A2', '42');

    assert.deepEqual(errors, []);
    await page.close();
});

test('F12 is Save As: it asks for a new file and binds the workbook to it; an export is a copy', { timeout: 400_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await openApp(page, 'Kazo Spreadsheets');
    const id = await waitForWindow(page, 1);
    const frame = await frameOf(page, id);
    await execute(frame, 'cell.set', { cell: 'A1', input: 'Report' });
    await execute(frame, 'file.save');
    await answerSaveDialog(page, 'Save the workbook', 'report.xlsx');
    await expectTitle(page, id, 'report.xlsx — Kazo Spreadsheets');

    // GridCraft says saveAs: false; F12 goes to the window, which asks for a new path.
    const box = await iframeOf(page, id).boundingBox();
    await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    await page.keyboard.press('F12');
    await answerSaveDialog(page, 'Save the workbook', 'report-2.xlsx');
    await expectTitle(page, id, 'report-2.xlsx — Kazo Spreadsheets');

    // Ctrl+Shift+S does the same.
    await page.keyboard.press('Control+Shift+S');
    await answerSaveDialog(page, 'Save the workbook', 'report-3.xlsx');
    await expectTitle(page, id, 'report-3.xlsx — Kazo Spreadsheets');

    // And a plain save now writes report-3.xlsx in place.
    await execute(frame, 'cell.set', { cell: 'A2', input: 'three' });
    await expectTitle(page, id, '*report-3.xlsx — Kazo Spreadsheets');
    await execute(frame, 'file.save');
    await expectTitle(page, id, 'report-3.xlsx — Kazo Spreadsheets');

    // A PDF is a copy: its own dialog, a message, and the workbook stays report-3.xlsx.
    await execute(frame, 'file.exportPdf');
    await answerSaveDialog(page, 'Save a copy', 'report.pdf');
    await windowById(page, id).locator('.sheet-notice', { hasText: 'Copy saved: report.pdf.' }).waitFor({ timeout: STEP_TIMEOUT_MS });
    await expectTitle(page, id, 'report-3.xlsx — Kazo Spreadsheets');

    await runInTerminal(page, `ls ${HOME}`);
    await expectInTerminal(page, 'report-3.xlsx');
    const listing = await page.locator('.xterm-rows').last().innerText();
    for (const name of ['report.xlsx', 'report-2.xlsx', 'report.pdf']) {
        assert.match(listing, new RegExp(name.replace('.', '\\.')));
    }

    assert.deepEqual(errors, []);
    await page.close();
});

test('a workbook opened from another format is never rewritten with .xlsx bytes', { timeout: 400_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await openApp(page, 'Kazo Spreadsheets');
    const id = await waitForWindow(page, 1);
    const frame = await frameOf(page, id);
    await execute(frame, 'cell.set', { cell: 'A1', input: 'macros' });
    await execute(frame, 'file.save');
    await answerSaveDialog(page, 'Save the workbook', 'source.xlsx');
    await expectTitle(page, id, 'source.xlsx — Kazo Spreadsheets');

    // The same workbook as a .xlsm file, opened by its association.
    await runInTerminal(page, `cp ${HOME}/source.xlsx ${HOME}/kept.xlsm`);
    await runInTerminal(page, `xdg-open ${HOME}/kept.xlsm`);
    const second = await waitForWindow(page, 2, [id]);
    await expectTitle(page, second, 'kept.xlsm — Kazo Spreadsheets');
    const opened = await frameOf(page, second);
    await waitForCell(opened, 'A1', 'macros');

    // GridCraft saves it as kept.xlsx: the window asks where, it does not rewrite kept.xlsm.
    await execute(opened, 'cell.set', { cell: 'A2', input: 'changed' });
    await execute(opened, 'file.save');
    await answerSaveDialog(page, 'Save the workbook', 'kept.xlsx');
    await expectTitle(page, second, 'kept.xlsx — Kazo Spreadsheets');

    assert.deepEqual(errors, []);
    await page.close();
});

test('closing with changes asks; Cancel keeps the window, Yes saves before closing', { timeout: 400_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await openApp(page, 'Kazo Spreadsheets');
    const id = await waitForWindow(page, 1);
    const frame = await frameOf(page, id);
    await execute(frame, 'cell.set', { cell: 'A1', input: 'Do not lose me' });
    await expectTitle(page, id, '*Untitled — Kazo Spreadsheets');

    const close = windowById(page, id).locator('header.window-chrome .chrome-button-close');
    const question = page.locator('.message-box-dialog', { hasText: 'Save the changes' });

    // Cancel: nothing closes, nothing is lost.
    await close.click();
    await question.waitFor({ timeout: STEP_TIMEOUT_MS });
    await question.getByRole('button', { name: 'Cancel' }).click();
    await question.waitFor({ state: 'hidden', timeout: STEP_TIMEOUT_MS });
    assert.equal(await windowById(page, id).count(), 1);
    await waitForCell(frame, 'A1', 'Do not lose me');

    // Yes, then Cancel in the save dialog: the window stays open, and the message says so.
    await close.click();
    await question.waitFor({ timeout: STEP_TIMEOUT_MS });
    await question.getByRole('button', { name: 'Yes' }).click();
    const saveDialog = page.locator('section.file-dialog', { hasText: 'Save the workbook' });
    await saveDialog.waitFor({ timeout: STEP_TIMEOUT_MS });
    await saveDialog.getByRole('button', { name: 'Cancel' }).click();
    await windowById(page, id).locator('.sheet-notice', { hasText: 'was not saved' }).waitFor({ timeout: STEP_TIMEOUT_MS });
    assert.equal(await windowById(page, id).count(), 1);

    // Yes, then a file name: the workbook is written, then the window closes.
    await close.click();
    await question.waitFor({ timeout: STEP_TIMEOUT_MS });
    await question.getByRole('button', { name: 'Yes' }).click();
    await answerSaveDialog(page, 'Save the workbook', 'closing.xlsx');
    await windowById(page, id).waitFor({ state: 'detached', timeout: STEP_TIMEOUT_MS });

    await runInTerminal(page, `xdg-open ${HOME}/closing.xlsx`);
    const reopened = await waitForWindow(page, 1);
    await expectTitle(page, reopened, 'closing.xlsx — Kazo Spreadsheets');
    await waitForCell(await frameOf(page, reopened), 'A1', 'Do not lose me');

    assert.deepEqual(errors, []);
    await page.close();
});

test('GridCraft\'s File › Open shows the OS dialog, and a workbook opens in the untouched window', { timeout: 400_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await openApp(page, 'Kazo Spreadsheets');
    const id = await waitForWindow(page, 1);
    const frame = await frameOf(page, id);
    await execute(frame, 'cell.set', { cell: 'C3', input: 'picked' });
    await execute(frame, 'file.save');
    await answerSaveDialog(page, 'Save the workbook', 'picked.xlsx');
    await windowById(page, id).locator('header.window-chrome .chrome-button-close').click();
    await windowById(page, id).waitFor({ state: 'detached', timeout: STEP_TIMEOUT_MS });

    // A new window: its blank workbook is untouched, so the picked workbook replaces it here.
    await openApp(page, 'Kazo Spreadsheets');
    const fresh = await waitForWindow(page, 1);
    const blank = await frameOf(page, fresh);
    // What File › Open › Browse does: GridCraft's dialog "open" starts its browser file picker.
    await request(blank, 'ui.dialog', { name: 'open' });
    const dialog = page.locator('section.file-dialog[role="dialog"]').last();
    await dialog.getByRole('heading', { name: 'Open a workbook' }).waitFor({ timeout: STEP_TIMEOUT_MS });
    await dialog.getByText('picked.xlsx', { exact: true }).dblclick().catch(async error => {
        if (process.env.SPREADSHEETS_E2E_SHOTS) {
            await page.screenshot({ path: `${process.env.SPREADSHEETS_E2E_SHOTS}/open-dialog.png` });
        }
        throw error;
    });
    await dialog.waitFor({ state: 'detached', timeout: STEP_TIMEOUT_MS });

    await expectTitle(page, fresh, 'picked.xlsx — Kazo Spreadsheets');
    await waitForCell(blank, 'C3', 'picked');
    assert.equal(await page.locator('article[data-window-id]', { has: page.locator('.sheet-window') }).count(), 1);

    assert.deepEqual(errors, []);
    await page.close();
});
