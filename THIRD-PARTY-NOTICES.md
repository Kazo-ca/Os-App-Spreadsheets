# Third-party notices

> [Version française](THIRD-PARTY-NOTICES-fr.md)

Kazo Spreadsheets is under the MIT license ([LICENSE](LICENSE)). Its C# code, its iframe page
(`src/Kazo.Spreadsheets/wwwroot/host/`) and its icon (`src/Kazo.Spreadsheets/wwwroot/icons/app.svg`,
`store/icon.png`) are this repository's own work. The iframe proxy it uses
(`craft-proxy.js`) belongs to Kazo OS (`Kazo.Os.AppSdk.Craft`, MIT).

It bundles one third-party work, **GridCraft**, which brings its own third-party material.

## GridCraft

| | |
| --- | --- |
| Work | [GridCraft](https://github.com/storytold/gridcraft), a spreadsheet written in Rust, by the ArtCraft team and the GridCraft contributors |
| Version | 0.19.1, compiled from our fork [`Kazo-ca/gridcraft`](https://github.com/Kazo-ca/gridcraft) (branch `kazo/v0.19.1`, the upstream tag, unmodified) |
| License | MIT or Apache-2.0, at the user's option |
| Files | `src/Kazo.Spreadsheets/wwwroot/gridcraft/` (the web build: `.js`, `.wasm`, `snippets/`) |
| Provenance | `src/Kazo.Spreadsheets/wwwroot/gridcraft/gridcraft.json`: the fork's commit, the compilers, and the SHA-256 of every file |

Kazo Spreadsheets does not change GridCraft: it shows its web build in an iframe and talks to it
through the control commands and events GridCraft publishes for a host page.

GridCraft's own notices ship **unmodified** next to its build, and in the installed package
(`_content/Kazo.Spreadsheets/gridcraft/`):

| File | Content |
| --- | --- |
| `LICENSE-MIT`, `LICENSE-APACHE` | GridCraft's licenses |
| `NOTICE` | GridCraft's copyright, its trademark notice, and the third-party material it bundles: the egui fonts (Ubuntu Light under the Ubuntu Font Licence 1.0, Hack under the MIT and Bitstream Vera licenses, Noto Emoji under the SIL Open Font License 1.1, and the emoji-icon-font icons under the MIT license), each with its license text |
| `ATTRIBUTION.md` | GridCraft's per-file list of assets (author, source, license) |

GridCraft's web release embeds no craft-fonts font, so it ships no `OFL-*.txt` file.

### The ArtCraft brand

The ArtCraft name, wordmark and logos are trademarks of the ArtCraft team; GridCraft's open-source
license does not cover them. Kazo Spreadsheets uses none of them: its name, its icon and its Store
listing are this repository's own. "Based on GridCraft by the ArtCraft team" only says where the
software comes from.

## In the package

The Kazo OS package (`.kazoapp`) carries, under `wwwroot/`:

- `legal/LICENSE.txt`: Kazo Spreadsheets' MIT license (this repository's `LICENSE`);
- `legal/THIRD-PARTY-NOTICES.md` and `legal/THIRD-PARTY-NOTICES-fr.md`: this file and its French
  version;
- `gridcraft/LICENSE-MIT`, `LICENSE-APACHE`, `NOTICE`, `ATTRIBUTION.md`: GridCraft's notices, as
  above.

A C# test (`LegalFilesTests`) checks that the project puts these files in the package.

## Test data

The end-to-end tests make their own workbooks with Kazo Spreadsheets; they ship no third-party
file.
