> [Version française](../fr/plan.md)

# Plan: the Kazo Spreadsheets app for Kazo OS

Step 0 of the Kazo OS [Craft app port guide](https://github.com/Kazo-ca/Os/blob/main/doc/en/wasm/dev/craft-app-port-guide.md),
written on 2026-11-13 (`TODO` issue [#1](https://github.com/Kazo-ca/Os-App-Spreadsheets/issues/1)).
This document sets the goal, what reading GridCraft showed, the decisions and the order of the
work items. The method comes from Kazo Word ([`Kazo-ca/Os-App-Word`](https://github.com/Kazo-ca/Os-App-Word),
[integration plan](https://github.com/Kazo-ca/Os-App-Word/blob/main/doc/en/integration-plan.md)).

## Goal

Kazo Spreadsheets is the **spreadsheet of Kazo OS**. It wraps the web build of
[GridCraft](https://github.com/storytold/gridcraft) (ArtCraft team, `MIT OR Apache-2.0`), a
spreadsheet written in Rust (egui, wgpu: WebGPU with a WebGL 2 fallback), in alpha, and connects
its host mode (`craft-web-host`) to the files, dialogs and file associations of Kazo OS. Nothing of
GridCraft is rewritten.

Difference with Kazo Word: the Kazo code shared by the Craft apps (iframe, proxy, file picker,
shortcuts, settings, `DocumentSession`, save rule) is **not copied** here. It comes from the
`Kazo.Os.AppSdk.Craft` module shipped by Kazo OS
([ADR 0247](https://github.com/Kazo-ca/Os/blob/craft-host/base/doc/en/wasm/dev/adr/0247-shared-craft-app-host-module.md),
series `craft-host/base` of `Kazo-ca/Os`). Kazo Spreadsheets keeps only its window, its list of
formats, its iframe page, the GridCraft build and its manifest.

## Reading GridCraft (step 0, 2026-11-13)

Read on the fork [`Kazo-ca/gridcraft`](https://github.com/Kazo-ca/gridcraft) (`main` at commit
`c0ee120`, 2026-11-12, version 0.19.1) and on the official zip `gridcraft-web-0.19.1.zip`.

| Point | Finding |
| --- | --- |
| Web build | `apps/gridcraft-web` (Trunk, `public_url = "./"`), depends on `craft-web-host` and `craft-web-api` at `craft-libs` revision `aa84415` (the rest of the app: `bb57d42`). Rust 1.95 at least. |
| Official zip | `https://github.com/storytold/gridcraft/releases/download/v0.19.1/gridcraft-web-0.19.1.zip`, SHA-256 `48c5d1b101befebb3c5ba3b7a14914f01c3207934813ed07dc4251b7ae116a78`, 9.7 MB. Content: `index.html`, glue `gridcraft-web-<hash>.js` (174 KB), `gridcraft-web-<hash>_bg.wasm` (**26.3 MB**), `snippets/`, icon, licences, `HOSTING.md`. |
| Size | Well under the Kazo OS limits (128 MiB per file, 64 MiB compressed package). Less than half of WordCraft (60 MB). |
| Canvas | `gridcraft_canvas`, hard-coded (one instance per HTML document: one iframe per window, as in Kazo Word). Loading screen `gridcraft_loading`. |
| Host mode | `data-host="page"` on the canvas: `gridcraft:*` `CustomEvent`s. `ready` (app version, `file: false`: GridCraft reads bytes), `open { name, bytes }` (refused over 512 MiB; no name: `Book.xlsx`), `save { name, saveAs, bytes }`, `dirty`, `failed`. `?author=` signs notes and comments. In host mode: empty workbook at start, no sample. |
| `save` | Sent for Save, Save As (`.xlsx`) and the PDF, CSV and HTML exports. **`saveAs` is always `false`**: GridCraft does not tell the host that a save is a "Save As" (WordCraft does, through its `host_save` service). No `id`, no confirmation (craft-libs #611, as in WordCraft). |
| `dirty` | True when **any** workbook open in the page has changes (WordCraft: only the displayed document). |
| Name sent by `save` | The workbook title with the format's extension. A workbook opened as `.ods`, `.xls` or `.csv` is saved as `Budget.xlsx`: the name no longer matches the bound file, so the Kazo Word rule asks for a path through the OS dialog (wanted: an `.ods` is not rewritten with `.xlsx` bytes). |
| Commands (`window.gridcraft`) | `request`, `execute`, `commands`, `docInspect`, `uiInspect`, `screenshot` (`craft-web-api`). `ui.inspect` gives `documents[{title, dirty}]` and `activeDocument`. **No `ui.theme` nor `ui.language`**: the theme goes through `ui.set { dark }` (or `view.darkMode`), the language through `app.language.set { language }`. `file.new`, `file.save`, `file.saveAs`, `file.saveBytes { format }`, `file.close`, `ui.backstage { page }` exist; no `file.rename`. |
| Browser picker (`rfd`) | Two uses: File › Open (`.xlsx .xlsm .xls .xlsb .ods .csv .tsv .txt .slk .json`) and Insert picture (`.png .jpg .jpeg`). The same `input type=file` as WordCraft: the module's picker replaces it. |
| Formats | Opened: the list above. Saved workbook: `.xlsx` only. Copies (the binding does not change): PDF, CSV, HTML. |
| Settings | `localStorage` `gridcraft.ui` and `gridcraft.prefs` (the module keeps them per Kazo user). |
| App chrome | `?chrome=none|notitlebar|nofiletab` removes GridCraft's title bar and/or File tab. By default everything is drawn. |
| Printing | The browser print dialog on a PDF (hidden iframe), as in Kazo Word. |
| Language | GridCraft follows the browser language except Chinese, Japanese and Korean; French exists. |
| Brand | ArtCraft name and logo are trademarks. A **modified** build removes them; the Store listing says "based on GridCraft by the ArtCraft team", with no logo. |

## Decisions

| # | Subject | Decision |
| --- | --- | --- |
| 1 | Name and identity | "**Kazo Spreadsheets**" (Benoit, 2026-11-13; replaces "Kazo Sheet" of the Kazo Word plan). Id `ca.kazo.spreadsheets`, publisher `pub.kazo`, assembly `Kazo.Spreadsheets`, category `productivity`, several windows. |
| 2 | Shared code | The Kazo OS `Kazo.Os.AppSdk.Craft` module (ADR 0247, Benoit, 2026-11-13). Kazo Spreadsheets builds against the OS submodule on `craft-host/base` until the OS that contains the module is released; its Store publication waits for that release. |
| 3 | Integration | One iframe per window, page `wwwroot/host/index.html` with the canvas `gridcraft_canvas` `data-host="page"` `data-craft-app="gridcraft"` (ADR 0247 §3). |
| 4 | Build provenance | As in Kazo Word: first the **official 0.19.1 zip** pinned by SHA-256, in the repository under `wwwroot/gridcraft/` with `gridcraft.json`; then built from the `Kazo-ca/gridcraft` fork (work item 3). |
| 5 | Save As | **Q1 = A** (Benoit, 2026-11-13): the window's "Save As…" (`file.saveBytes` then the OS dialog), F12 and Ctrl+Shift+S sent to that command by the proxy; the Save As button of GridCraft's File page stays a Save (listing limit); upstream issue and PR prepared by Claude, published by Benoit. |
| 6 | Associated formats | **Q2 = A** (Benoit, 2026-11-13): default app for `.xlsx .xlsm .xlsb .xls .ods .slk .csv .tsv`; "Open with" only for `.txt` and `.json`. |
| 7 | GridCraft chrome | Its title bar and File tab stay (Save, Undo, Search, theme live there); `?chrome` stays a possible setting later. |

## Questions asked to Benoit

Answered on 2026-11-13: "I agree with your recommendations" (A and A).

**Q1. Save As.** GridCraft always sends `saveAs: false`. When a workbook is bound to
`~/Documents/Budget.xlsx`, Save As (F12, Ctrl+Shift+S, File › Save As) rewrites `Budget.xlsx`
instead of asking for a new name.

- **A (recommended)**: work around it in Kazo and fix it upstream. The window has its own
  "Save As…" (window menu) that asks GridCraft for `file.saveBytes` then opens the OS dialog; the
  module's proxy sends F12 and Ctrl+Shift+S to that command. The Save As button of GridCraft's File
  page stays a Save (limit noted in the listing). Meanwhile Claude prepares the upstream issue and
  PR (reuse WordCraft's `host_save`), which Benoit publishes.
- B: fix it only in our fork (work item 3), so a modified build from v1 (ArtCraft marks removed),
  and offer the PR upstream.
- C: accept the limit for v1, with no workaround.

**Q2. Formats associated with Kazo Spreadsheets.**

- **A (recommended)**: default app for `.xlsx .xlsm .xlsb .xls .ods .slk .csv .tsv`; "Open with"
  only for `.txt` and `.json` (the OS editor stays the default).
- B: default app only for workbooks (`.xlsx .xlsm .xlsb .xls .ods .slk`); `.csv` and `.tsv` as
  "Open with".

## What the module needs to know about GridCraft

Passed on to the `craft-host/base` series (work items 2 and 3), since GridCraft differs from
WordCraft:

- theme and language through other commands (`ui.set { dark }`, `app.language.set { language }`):
  the module must take them from the app (for example in `CraftFrameOptions`);
- `saveAs` always `false` and a `save` name carrying the format's extension (Q1);
- `dirty` covers every workbook in the page, not only the displayed one;
- more formats can be opened than saved (`CraftDocumentFormat`: openable, default, copy).

## Work items

Series `spreadsheets/base`, one branch `spreadsheets/<n>-<topic>` and one PR to
`spreadsheets/base` per work item, one thread per work item. Benoit merges `spreadsheets/base`
into `main`. The module does much of what Kazo Word's work items 2 and 3 did: they are merged
here.

| No. | Branch | Content | Depends on |
| --- | --- | --- | --- |
| 0 | `spreadsheets/0-plan` | this plan (fr and en) | — |
| 1 | `spreadsheets/1-solution` | rename the template solution (`Kazo.Spreadsheets`, `.Os`, `.Tests`), manifest, `tools/vendor-gridcraft.sh` (pinned 0.19.1 zip) and `gridcraft.json`, manifest and provenance tests, OS submodule on 3.3.2 | plan merged; can start now |
| 2 | `spreadsheets/2-window` | window on the module: `host/index.html`, formats, open and save, close guard, Save As (Q1), picker, shortcuts, theme, language, author; two windows; WebGPU and WebGL 2; Playwright; OS submodule on `craft-host/base` | work item 1, `craft-host/base` work items 2 and 3 merged |
| 3 | `spreadsheets/3-build-from-source` | `Kazo-ca/gridcraft` fork as a submodule, `tools/build-gridcraft.sh`, `build-gridcraft.yml` workflow, commit test | work item 1; in parallel with 2 |
| 4 | `spreadsheets/4-store` | icon with no ArtCraft logo, fr and en listing, README rebuilt on the listing, `THIRD-PARTY-NOTICES.md`, minimum OS version, install in a published Kazo OS, tag, catalog, `features.md` | work items 2 and 3, **release of the OS that contains the module** (number chosen by Benoit) |

**Done when**: Kazo Spreadsheets installs from the Store of a published Kazo OS, opens a workbook
by double-click, saves it, and two windows run together.

## Upstream track

Texts prepared by Claude in English, published by Benoit (no agent mention, `storytold`
repository rules):

- GridCraft: tell the host `saveAs` (like WordCraft's `host_save`), depending on Q1;
- `craft-libs`: join #611 (save confirmation) and #610 (`open-request`).

## Progress

- [x] Step 0: reading GridCraft 0.19.1 and the official zip.
- [x] Plan fr and en.
- [x] Benoit's answers to Q1 and Q2 (A and A).
- [ ] Work item 1 (`spreadsheets/1-solution`): solution renamed, manifest, 0.19.1 zip in `wwwroot/gridcraft/` (9.7 MB `.kazoapp` package, 26.5 MB extracted), OS submodule on 3.3.2; the window shows the GridCraft version until the module arrives.
- [ ] Work items 2 to 4.
