# Avis de tiers

> [English version](THIRD-PARTY-NOTICES.md)

Kazo Spreadsheets est sous licence MIT ([LICENSE](LICENSE)). Son code C#, sa page d'iframe
(`src/Kazo.Spreadsheets/wwwroot/host/`) et son icône (`src/Kazo.Spreadsheets/wwwroot/icons/app.svg`,
`store/icon.png`) sont l'œuvre de ce dépôt. Le proxy d'iframe qu'il utilise (`craft-proxy.js`)
appartient à Kazo OS (`Kazo.Os.AppSdk.Craft`, MIT).

Il embarque une seule œuvre tierce, **GridCraft**, qui apporte ses propres éléments tiers.

## GridCraft

| | |
| --- | --- |
| Œuvre | [GridCraft](https://github.com/storytold/gridcraft), un tableur écrit en Rust, de l'équipe ArtCraft et des contributeurs de GridCraft |
| Version | 0.19.1, compilée depuis notre fork [`Kazo-ca/gridcraft`](https://github.com/Kazo-ca/gridcraft) (branche `kazo/v0.19.1`, l'étiquette amont, sans modification) |
| Licence | MIT ou Apache-2.0, au choix de l'utilisateur |
| Fichiers | `src/Kazo.Spreadsheets/wwwroot/gridcraft/` (la version web : `.js`, `.wasm`, `snippets/`) |
| Provenance | `src/Kazo.Spreadsheets/wwwroot/gridcraft/gridcraft.json` : le commit du fork, les compilateurs et le SHA-256 de chaque fichier |

Kazo Spreadsheets ne modifie pas GridCraft : il montre sa version web dans une iframe et lui parle
par les commandes de contrôle et les événements que GridCraft publie pour une page hôte.

Les avis de GridCraft sont livrés **sans modification** à côté de sa version web, et dans le
paquet installé (`_content/Kazo.Spreadsheets/gridcraft/`) :

| Fichier | Contenu |
| --- | --- |
| `LICENSE-MIT`, `LICENSE-APACHE` | les licences de GridCraft |
| `NOTICE` | le copyright de GridCraft, son avis de marque, et les éléments tiers qu'il embarque : les polices d'egui (Ubuntu Light sous Ubuntu Font Licence 1.0, Hack sous les licences MIT et Bitstream Vera, Noto Emoji sous SIL Open Font License 1.1, et les icônes d'emoji-icon-font sous licence MIT), chacune avec le texte de sa licence |
| `ATTRIBUTION.md` | la liste, fichier par fichier, des ressources de GridCraft (auteur, source, licence) |

La version web de GridCraft n'embarque aucune police de craft-fonts, donc aucun fichier
`OFL-*.txt`.

### La marque ArtCraft

Le nom, le logotype et les logos ArtCraft sont des marques de l'équipe ArtCraft ; la licence libre
de GridCraft ne les couvre pas. Kazo Spreadsheets n'en utilise aucun : son nom, son icône et sa
fiche du Store sont propres à ce dépôt. « Basé sur GridCraft de l'équipe ArtCraft » dit seulement
d'où vient le logiciel.

## Dans le paquet

Le paquet Kazo OS (`.kazoapp`) contient, sous `wwwroot/` :

- `legal/LICENSE.txt` : la licence MIT de Kazo Spreadsheets (le `LICENSE` de ce dépôt) ;
- `legal/THIRD-PARTY-NOTICES.md` et `legal/THIRD-PARTY-NOTICES-fr.md` : ce fichier et sa version
  anglaise ;
- `gridcraft/LICENSE-MIT`, `LICENSE-APACHE`, `NOTICE`, `ATTRIBUTION.md` : les avis de GridCraft,
  comme ci-dessus.

Un test C# (`LegalFilesTests`) vérifie que le projet met ces fichiers dans le paquet.

## Données de test

Les tests de bout en bout créent leurs propres classeurs avec Kazo Spreadsheets ; ils ne livrent
aucun fichier tiers.
