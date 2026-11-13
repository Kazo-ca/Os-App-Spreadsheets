# Kazo Spreadsheets

A spreadsheet for Kazo OS, for `.xlsx`, `.ods` and `.csv` workbooks. Kazo Spreadsheets is based on
[GridCraft](https://github.com/storytold/gridcraft) by the ArtCraft team, an open-source
spreadsheet written in Rust, which runs here in a Kazo OS window.

## Calculate

- Sheets, formulas and the worksheet functions you expect (more than 500), number formats,
  conditional formatting, tables, sorting and filters, charts, pivot tables.
- GridCraft's ribbon, its formula bar, its command search and its shortcuts.
- Several windows at once, each with its own workbooks.

## Your Kazo OS files

- Double-click a `.xlsx`, `.xlsm`, `.xlsb`, `.xls`, `.ods`, `.slk`, `.csv` or `.tsv` file in the
  File Explorer, or use "Open with" on a `.txt` or `.json` file: the workbook opens in Kazo
  Spreadsheets.
- Save writes `.xlsx`. A `.xlsx` you opened is rewritten in place; any other format is never
  overwritten with `.xlsx` data, so its first save asks for a name in the Kazo OS save dialog.
  Save As (F12 or Ctrl+Shift+S) and the first save of a new workbook use that dialog too.
- Exports to PDF, CSV and HTML are copies: the workbook stays bound to its file.
- Closing the window asks before losing unsaved changes, for every open workbook.
- File › Open and the pictures you insert come from Kazo OS files, through the Kazo OS dialog.
- Kazo Spreadsheets follows the OS theme (light or dark) and language; its settings are kept per
  Kazo OS user in `~/.config/ca.kazo.spreadsheets/`.

## Known limits

- **GridCraft is in alpha.** Most workbooks open and save well, but keep a copy of the important
  ones.
- GridCraft's own Save As button (File page) saves under the current name: use F12 or
  Ctrl+Shift+S for Save As.
- **Asian scripts**: the web version of GridCraft has no Chinese, Japanese or Korean font; such text
  may not show, and those interface languages fall back to English.
- GridCraft draws with **WebGPU or WebGL 2**: a browser or computer with neither shows an error
  screen instead of the workbook.
- The download is about 10 MB (27 MB once installed), and each window loads its own copy of
  GridCraft.
- The window stays on the screen where it opened: moving it to another screen would reload
  GridCraft and lose the open workbooks.
- Printing goes through the browser's print dialog.
- Needs Kazo OS 3.3.3 or later.

## License

Kazo Spreadsheets is free software under the MIT license. GridCraft is under the MIT or Apache-2.0
license; its notices and the licenses of the fonts it includes ship with the app (see
`THIRD-PARTY-NOTICES.md` in the repository). The ArtCraft name and logos are trademarks of the
ArtCraft team and are not used by Kazo Spreadsheets.
