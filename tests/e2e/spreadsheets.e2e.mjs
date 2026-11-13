// End-to-end runs of the Kazo Spreadsheets window inside the Kazo OS host (doc/en/plan.md, work
// item 2): `npm run test:e2e` after `dotnet build Kazo.Spreadsheets.slnx -c Debug`. Each test opens
// a fresh page. GridCraft starts on WebGPU by default, on WebGL 2 with the `--webgl` launch argument.
import { after, before, test } from 'node:test';
import assert from 'node:assert/strict';
import { openApp, runInTerminal, startHost } from './host.mjs';
import {
    bootPage, execute, frameOf, iframeOf, launchBrowser, rendererOf, request, sheetWindows, waitForCell, waitForWindow, windowById,
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

test('Kazo Spreadsheets opens from the Applications menu, on WebGPU, in the user\'s theme and with the keyboard', { timeout: 300_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await openApp(page, 'Kazo Spreadsheets');
    const id = await waitForWindow(page, 1);
    assert.equal(await page.locator('.sheet-screen').count(), 0, 'no loading or error screen is left');
    const frame = await frameOf(page, id);
    assert.equal(new URL(frame.url()).searchParams.get('author'), 'Neo', 'the user signs notes and comments');
    assert.equal(await rendererOf(frame), 'webgpu');

    // The window sent the OS theme (dark) to GridCraft.
    const ui = await request(frame, 'ui.set', {});
    assert.equal(ui.dark, true);

    // Typed in the grid: the keyboard reaches GridCraft.
    const box = await iframeOf(page, id).boundingBox();
    await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    await execute(frame, 'selection.set', { range: 'B2' });
    await page.keyboard.type('1234', { delay: 20 });
    await page.keyboard.press('Enter');
    await waitForCell(frame, 'B2', '1234');

    assert.deepEqual(errors, []);
    await page.close();
});

test('the --webgl launch argument starts GridCraft on WebGL 2', { timeout: 300_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await runInTerminal(page, 'launch ca.kazo.spreadsheets --webgl');
    const id = await waitForWindow(page, 1);
    const frame = await frameOf(page, id);
    assert.equal(new URL(frame.url()).searchParams.has('webgl'), true);
    assert.equal(await rendererOf(frame), 'webgl2');
    await execute(frame, 'cell.set', { cell: 'A1', input: 'WebGL' });
    await waitForCell(frame, 'A1', 'WebGL');

    assert.deepEqual(errors, []);
    await page.close();
});

test('two windows run their own GridCraft, and Ctrl+N opens another', { timeout: 300_000 }, async () => {
    const { page, errors } = await bootPage(browser, host.baseUrl);

    await openApp(page, 'Kazo Spreadsheets');
    const firstId = await waitForWindow(page, 1);
    await openApp(page, 'Kazo Spreadsheets');
    const secondId = await waitForWindow(page, 2, [firstId]);
    const first = await frameOf(page, firstId);
    const second = await frameOf(page, secondId);
    assert.notEqual(first, second);

    // Each GridCraft keeps its own workbook.
    await execute(second, 'cell.set', { cell: 'A1', input: 'second' });
    await waitForCell(second, 'A1', 'second');
    assert.doesNotMatch(JSON.stringify(await request(first, 'cell.get', { cell: 'A1' })), /second/);

    // Ctrl+N in GridCraft: the window opens a third Kazo Spreadsheets window.
    const box = await iframeOf(page, secondId).boundingBox();
    await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    await page.keyboard.press('Control+n');
    await waitForWindow(page, 3, [firstId, secondId]);
    assert.equal(await sheetWindows(page).count(), 3);
    assert.equal(await windowById(page, firstId).count(), 1);

    assert.deepEqual(errors, []);
    await page.close();
});
