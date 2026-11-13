> [English version](../en/plan.md)

# Plan : l'app Kazo Spreadsheets pour Kazo OS

Étape 0 du [guide de portage des apps Craft](https://github.com/Kazo-ca/Os/blob/main/doc/fr/wasm/dev/craft-app-port-guide.md)
de Kazo OS, écrite le 2026-11-13 (issue `TODO` [#1](https://github.com/Kazo-ca/Os-App-Spreadsheets/issues/1)).
Ce document fixe le but, ce que la lecture de GridCraft a montré, les décisions et l'ordre des
chantiers. La méthode vient de Kazo Word ([`Kazo-ca/Os-App-Word`](https://github.com/Kazo-ca/Os-App-Word),
[plan d'intégration](https://github.com/Kazo-ca/Os-App-Word/blob/main/doc/fr/integration-plan.md)).

## But

Kazo Spreadsheets est le **tableur de Kazo OS**. Il emballe le build web de
[GridCraft](https://github.com/storytold/gridcraft) (équipe ArtCraft, `MIT OR Apache-2.0`), un
tableur écrit en Rust (egui, wgpu : WebGPU avec repli WebGL 2), en alpha, et branche son mode
hébergé (`craft-web-host`) sur les fichiers, les dialogues et les associations de Kazo OS. Rien de
GridCraft n'est réécrit.

Différence avec Kazo Word : le code Kazo commun aux apps Craft (iframe, proxy, sélecteur de
fichiers, raccourcis, réglages, `DocumentSession`, règle d'enregistrement) n'est **pas copié** ici.
Il vient du module `Kazo.Os.AppSdk.Craft` livré par Kazo OS
([ADR 0247](https://github.com/Kazo-ca/Os/blob/craft-host/base/doc/fr/wasm/dev/adr/0247-shared-craft-app-host-module.md),
série `craft-host/base` de `Kazo-ca/Os`). Kazo Spreadsheets garde seulement sa fenêtre, sa liste
de formats, sa page d'iframe, le build de GridCraft et son manifeste.

## Lecture de GridCraft (étape 0, 2026-11-13)

Lue sur le fork [`Kazo-ca/gridcraft`](https://github.com/Kazo-ca/gridcraft) (`main` au commit
`c0ee120`, 2026-11-12, version 0.19.1) et sur le zip officiel `gridcraft-web-0.19.1.zip`.

| Point | Constat |
| --- | --- |
| Build web | `apps/gridcraft-web` (Trunk, `public_url = "./"`), dépend de `craft-web-host` et `craft-web-api` à la révision `aa84415` de `craft-libs` (le reste de l'app : `bb57d42`). Rust 1.95 au minimum. |
| Zip officiel | `https://github.com/storytold/gridcraft/releases/download/v0.19.1/gridcraft-web-0.19.1.zip`, SHA-256 `48c5d1b101befebb3c5ba3b7a14914f01c3207934813ed07dc4251b7ae116a78`, 9,7 Mo. Contenu : `index.html`, colle `gridcraft-web-<hash>.js` (174 Ko), `gridcraft-web-<hash>_bg.wasm` (**26,3 Mo**), `snippets/`, icône, licences, `HOSTING.md`. |
| Taille | Bien sous les limites de Kazo OS (128 Mio par fichier, paquet de 64 Mio compressé). Moins de la moitié de WordCraft (60 Mo). |
| Canevas | `gridcraft_canvas`, écrit en dur (une instance par document HTML : une iframe par fenêtre, comme Kazo Word). Écran de chargement `gridcraft_loading`. |
| Mode hébergé | `data-host="page"` sur le canevas : `CustomEvent` `gridcraft:*`. `ready` (version de l'app, `file: false` : GridCraft lit des octets), `open { name, bytes }` (refusé au-delà de 512 Mio ; sans nom : `Book.xlsx`), `save { name, saveAs, bytes }`, `dirty`, `failed`. `?author=` signe les notes et commentaires. En mode hébergé : classeur vide au départ, pas d'exemple. |
| `save` | Envoyé pour Enregistrer, Enregistrer sous (`.xlsx`) et les exports PDF, CSV et HTML. **`saveAs` est toujours `false`** : GridCraft ne dit pas à l'hôte qu'un enregistrement est un « Enregistrer sous » (WordCraft le dit, par son service `host_save`). Pas d'`id`, pas de confirmation (craft-libs #611, comme WordCraft). |
| `dirty` | Vrai si **un** des classeurs ouverts dans la page a des modifications (WordCraft : seulement le document affiché). |
| Nom envoyé par `save` | Le titre du classeur avec l'extension du format. Un classeur ouvert en `.ods`, `.xls` ou `.csv` est enregistré en `Budget.xlsx` : le nom ne correspond plus au fichier lié, la règle de Kazo Word demande alors un chemin par le dialogue de l'OS (comportement voulu : on ne réécrit pas un `.ods` avec des octets `.xlsx`). |
| Commandes (`window.gridcraft`) | `request`, `execute`, `commands`, `docInspect`, `uiInspect`, `screenshot` (`craft-web-api`). `ui.inspect` donne `documents[{title, dirty}]` et `activeDocument`. **Pas de `ui.theme` ni de `ui.language`** : le thème passe par `ui.set { dark }` (ou `view.darkMode`), la langue par `app.language.set { language }`. `file.new`, `file.save`, `file.saveAs`, `file.saveBytes { format }`, `file.close`, `ui.backstage { page }` existent ; pas de `file.rename`. |
| Sélecteur du navigateur (`rfd`) | Deux usages : Fichier › Ouvrir (`.xlsx .xlsm .xls .xlsb .ods .csv .tsv .txt .slk .json`) et Insérer une image (`.png .jpg .jpeg`). Le même `input type=file` que WordCraft : le sélecteur du module le remplace. |
| Formats | Ouverts : la liste ci-dessus. Classeur enregistré : `.xlsx` seulement. Copies (la liaison ne change pas) : PDF, CSV, HTML. |
| Réglages | `localStorage` `gridcraft.ui` et `gridcraft.prefs` (le module les garde par utilisateur Kazo). |
| Cadre de l'app | `?chrome=none|notitlebar|nofiletab` retire la barre de titre et/ou l'onglet Fichier de GridCraft. Par défaut, tout est dessiné. |
| Impression | Dialogue d'impression du navigateur sur un PDF (iframe cachée), comme Kazo Word. |
| Langue | GridCraft suit la langue du navigateur sauf chinois, japonais et coréen ; le français existe. |
| Marque | Nom et logo ArtCraft : marques. Un build **modifié** les retire ; la fiche du Store dit « basé sur GridCraft de l'équipe ArtCraft », sans logo. |

## Décisions

| # | Sujet | Décision |
| --- | --- | --- |
| 1 | Nom et identité | « **Kazo Spreadsheets** » (Benoit, 2026-11-13 ; remplace « Kazo Sheet » du plan de Kazo Word). Id `ca.kazo.spreadsheets`, éditeur `pub.kazo`, assembly `Kazo.Spreadsheets`, catégorie `productivity`, plusieurs fenêtres. |
| 2 | Code commun | Module `Kazo.Os.AppSdk.Craft` de Kazo OS (ADR 0247, Benoit, 2026-11-13). Kazo Spreadsheets compile contre le sous-module de l'OS sur `craft-host/base` tant que l'OS qui contient le module n'est pas sorti ; sa publication au Store attend cette sortie. |
| 3 | Intégration | Une iframe par fenêtre, page `wwwroot/host/index.html` avec le canevas `gridcraft_canvas` `data-host="page"` `data-craft-app="gridcraft"` (ADR 0247 §3). |
| 4 | Provenance du build | Comme Kazo Word : d'abord le **zip officiel 0.19.1** épinglé par SHA-256, dans le dépôt sous `wwwroot/gridcraft/` avec `gridcraft.json` ; ensuite compilé depuis le fork `Kazo-ca/gridcraft` (chantier 3). |
| 5 | Enregistrer sous | **Question Q1 à Benoit** (voir plus bas). |
| 6 | Formats associés | **Question Q2 à Benoit** (voir plus bas). |
| 7 | Cadre de GridCraft | On garde sa barre de titre et son onglet Fichier (Enregistrer, Annuler, Rechercher, thème y sont) ; `?chrome` reste un réglage possible plus tard. |

## Questions ouvertes

**Q1. Enregistrer sous.** GridCraft envoie toujours `saveAs: false`. Si un classeur est lié à
`~/Documents/Budget.xlsx`, Enregistrer sous (F12, Ctrl+Maj+S, Fichier › Enregistrer sous) réécrit
`Budget.xlsx` au lieu de demander un nouveau nom.

- **A (recommandée)** : contourner dans Kazo et corriger en amont. La fenêtre a son propre
  « Enregistrer sous… » (menu de la fenêtre) qui demande `file.saveBytes` à GridCraft puis ouvre
  le dialogue de l'OS ; le proxy du module renvoie F12 et Ctrl+Maj+S à cette commande. Le bouton
  Enregistrer sous de la page Fichier de GridCraft reste un Enregistrer (limite notée dans la
  fiche). En parallèle, Claude prépare l'issue et la PR amont (reprendre `host_save` de WordCraft),
  que Benoit publie.
- B : corriger seulement dans notre fork (chantier 3), donc un build modifié dès la v1 (retrait
  des marques ArtCraft), et proposer la PR en amont.
- C : accepter la limite pour la v1, sans contournement.

**Q2. Formats associés à Kazo Spreadsheets.**

- **A (recommandée)** : app par défaut pour `.xlsx .xlsm .xlsb .xls .ods .slk .csv .tsv` ;
  « Ouvrir avec » seulement pour `.txt` et `.json` (l'éditeur de l'OS reste par défaut).
- B : app par défaut seulement pour les classeurs (`.xlsx .xlsm .xlsb .xls .ods .slk`) ; `.csv` et
  `.tsv` en « Ouvrir avec ».

## Ce que le module doit savoir de GridCraft

Transmis à la série `craft-host/base` (chantiers 2 et 3), car GridCraft diffère de WordCraft :

- thème et langue par d'autres commandes (`ui.set { dark }`, `app.language.set { language }`) :
  le module doit les prendre de l'app (par exemple dans `CraftFrameOptions`) ;
- `saveAs` toujours `false` et nom de `save` avec l'extension du format (Q1) ;
- `dirty` vaut pour tous les classeurs de la page, pas seulement l'affiché ;
- formats ouvrables plus nombreux que les formats enregistrés (`CraftDocumentFormat` : ouvrable,
  par défaut, copie).

## Chantiers

Série `spreadsheets/base`, une branche `spreadsheets/<n>-<sujet>` et une PR vers
`spreadsheets/base` par chantier, un fil par chantier. Benoit fusionne `spreadsheets/base` dans
`main`. Le module fait une grande part de ce que les chantiers 2 et 3 de Kazo Word ont fait : ils
sont réunis ici.

| N° | Branche | Contenu | Dépend de |
| --- | --- | --- | --- |
| 0 | `spreadsheets/0-plan` | ce plan (fr et en) | — |
| 1 | `spreadsheets/1-solution` | renommer la solution du modèle (`Kazo.Spreadsheets`, `.Os`, `.Tests`), manifeste, `tools/vendor-gridcraft.sh` (zip 0.19.1 épinglé) et `gridcraft.json`, tests du manifeste et de la provenance, sous-module de l'OS sur 3.3.2 | plan fusionné ; peut commencer tout de suite |
| 2 | `spreadsheets/2-window` | fenêtre sur le module : `host/index.html`, formats, ouvrir et enregistrer, garde de fermeture, Enregistrer sous (Q1), sélecteur, raccourcis, thème, langue, auteur ; deux fenêtres ; WebGPU et WebGL 2 ; Playwright ; sous-module de l'OS sur `craft-host/base` | chantier 1, chantiers 2 et 3 de `craft-host/base` fusionnés |
| 3 | `spreadsheets/3-build-from-source` | fork `Kazo-ca/gridcraft` en sous-module, `tools/build-gridcraft.sh`, workflow `build-gridcraft.yml`, test du commit | chantier 1 ; en parallèle du 2 |
| 4 | `spreadsheets/4-store` | icône sans logo ArtCraft, fiche fr et en, README refait sur la fiche, `THIRD-PARTY-NOTICES.md`, version minimale de l'OS, installation dans un Kazo OS publié, tag, catalogue, `features.md` | chantiers 2 et 3, **sortie de l'OS qui contient le module** (numéro choisi par Benoit) |

**Terminé quand** : Kazo Spreadsheets s'installe depuis le Store d'un Kazo OS publié et ouvre un
classeur par double-clic, l'enregistre, et deux fenêtres tournent ensemble.

## Piste en amont

Textes préparés par Claude en anglais, publiés par Benoit (pas de mention d'agent, règles des
dépôts `storytold`) :

- GridCraft : dire `saveAs` à l'hôte (comme `host_save` de WordCraft), selon Q1 ;
- `craft-libs` : se joindre à #611 (confirmation d'enregistrement) et #610 (`open-request`).

## Avancement

- [x] Étape 0 : lecture de GridCraft 0.19.1 et du zip officiel.
- [x] Plan fr et en.
- [ ] Réponses de Benoit à Q1 et Q2.
- [ ] Chantiers 1 à 4.
