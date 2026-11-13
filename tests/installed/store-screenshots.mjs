// The Kazo OS Store's screenshots (store/screenshots/), taken by install-and-open.mjs when it is
// given a screenshots folder, in the OS where Kazo Spreadsheets was just installed. The workbooks
// are GridCraft's own samples (its original data, `file.new { sample }`), so no third-party content
// appears.

const STEP_MS = 60_000;

/** egui paints on input: move the pointer over the workbook so the last frame is drawn. */
async function repaint(page, id, windowById) {
    const box = await windowById(page, id).locator('iframe.craft-frame-iframe').boundingBox();
    for (let step = 0; step < 6; step++) {
        await page.mouse.move(box.x + box.width / 2 + step * 7, box.y + box.height - 60 - step * 5);
        await page.waitForTimeout(400);
    }
}

/**
 * Opens one of GridCraft's samples in the window and saves it under `fileName` in the home folder,
 * through the Kazo OS save dialog, so the title bar names the workbook shown.
 */
async function saveSampleAs({ page, id, frame, execute, windowById }, sample, fileName) {
    await execute(frame, 'file.new', { sample });
    await execute(frame, 'file.save');
    const dialog = page.locator('section.file-dialog', { hasText: 'Save the workbook' });
    await dialog.waitFor({ state: 'visible', timeout: STEP_MS });
    await dialog.getByLabel('File name').fill(fileName);
    await dialog.getByRole('button', { name: 'Save' }).click();
    await dialog.waitFor({ state: 'hidden', timeout: STEP_MS });
    await page.waitForFunction(
        ([windowId, wanted]) => document.querySelector(`article[data-window-id="${windowId}"] header.window-chrome h2`)?.textContent?.trim() === wanted,
        [id, `${fileName} — Kazo Spreadsheets`],
        { timeout: STEP_MS });
    await repaint(page, id, windowById);
}

export async function takeScreenshots({ page, second, reopened, execute, shoot, windowById, openApp, HOME }) {
    const context = { page, id: second, frame: reopened, execute, windowById };
    const chrome = windowById(page, second).locator('header.window-chrome').first();

    // The File Explorer of the journey is reopened at the end, listing the new files.
    for (const close of await page.locator('article[data-window-id]', { hasText: 'File Explorer' }).locator('header.window-chrome .chrome-button-close').all()) {
        await close.click();
    }

    // 1. The sales sample (a table, conditional formatting, charts), the window maximized.
    await chrome.dblclick({ position: { x: 300, y: 12 } });
    await saveSampleAs(context, 'sales', 'Sales.xlsx');
    await shoot(page, '1.png');

    // 2. The budget sample, in a window of the desktop.
    await chrome.dblclick({ position: { x: 300, y: 12 } });
    await saveSampleAs(context, 'budget', 'Budget.xlsx');
    await shoot(page, '2.png');

    // 3. The File Explorer in front: the workbooks open in Kazo Spreadsheets with a double-click.
    await openApp(page, 'File Explorer');
    const row = page.locator(`tr[data-item-path="${HOME}/Budget.xlsx"]`).first();
    await row.waitFor({ timeout: STEP_MS });
    await row.click({ position: { x: 40, y: 10 } });
    await shoot(page, '3.png');
}
