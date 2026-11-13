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
| 5 | Enregistrer sous | **Q1 = A** (Benoit, 2026-11-13) : « Enregistrer sous… » de la fenêtre (`file.saveBytes` puis dialogue de l'OS), F12 et Ctrl+Maj+S renvoyés à cette commande par le proxy ; le bouton Enregistrer sous de la page Fichier de GridCraft reste un Enregistrer (limite de la fiche) ; issue et PR amont préparées par Claude, publiées par Benoit. |
| 6 | Formats associés | **Q2 = A** (Benoit, 2026-11-13) : app par défaut pour `.xlsx .xlsm .xlsb .xls .ods .slk .csv .tsv` ; « Ouvrir avec » seulement pour `.txt` et `.json`. |
| 7 | Cadre de GridCraft | On garde sa barre de titre et son onglet Fichier (Enregistrer, Annuler, Rechercher, thème y sont) ; `?chrome` reste un réglage possible plus tard. |

## Questions posées à Benoit

Réponse le 2026-11-13 : « je suis d'accord avec tes recommandations » (A et A).

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

## Chantier 2 : la fenêtre (2026-11-13)

La fenêtre tourne sur le module `Kazo.Os.AppSdk.Craft` de la série `craft-host/base` (sous-module
de l'OS sur `940ccd9`, puis `3788bfb`). Ce que Kazo Spreadsheets garde à lui :

| Fichier | Rôle |
| --- | --- |
| `wwwroot/host/index.html` | page de l'iframe : canevas `gridcraft_canvas` `data-craft-app="gridcraft"` `data-craft-build="../gridcraft/gridcraft.json"`, `craft-proxy.js` et `craft-page.css` du module |
| `Documents/SpreadsheetFormats.cs` | formats de la décision 6 (Q2 = A), copies PDF et HTML, nom d'enregistrement (`SaveNameOf`), « tout ce qui n'est pas `.xlsx` est une copie » (`IsCopyName`) |
| `Hosting/GridCraftLanguage.cs` | langues de GridCraft (deux lettres), choisies par `CraftLanguage.Pick` du module ; chinois, japonais et coréen restent en anglais (pas de police CJK dans la version web) |
| `Hosting/SpreadsheetKeys.cs` | raccourcis réservés : ceux de l'OS, Ctrl+N (nouvelle fenêtre), F12 et Ctrl+Shift+S (Enregistrer sous) |
| `SpreadsheetsWindow.*` | démarrage, réessai et délai, ouvrir, enregistrer, Enregistrer sous, fermeture, sélecteur de fichiers, réglages, thème, langue, auteur |

Ce qui a été décidé en écrivant le code :

- **Enregistrer sous (Q1 = A).** F12 et Ctrl+Shift+S arrivent à la fenêtre, qui arme un
  « Enregistrer sous » puis lance `file.saveAs` de GridCraft ; le `save` qui suit est décidé avec
  `saveAs: true` (dialogue de l'OS, puis liaison au nouveau fichier). Passer par la sauvegarde de
  GridCraft plutôt que par `file.saveBytes` garde juste son état « modifié ». Le SDK n'a pas de menu
  de fenêtre : les raccourcis sont l'entrée de la commande. Le bouton Enregistrer sous de la page
  Fichier de GridCraft reste un Enregistrer (limite de la fiche du Store).
- **Liaison sous le nom du fichier.** Un fichier ouvert est lié sous son propre nom
  (`Budget.csv`). GridCraft l'enregistre sous `Budget.xlsx` : autre nom, donc le dialogue de l'OS ;
  un `.csv`, `.ods` ou `.xlsm` n'est jamais réécrit avec des octets `.xlsx`. Un `.xlsx` est réécrit
  en place.
- **Copies.** Tout enregistrement qui n'est pas `.xlsx` est une copie (PDF, CSV et HTML exportés),
  même si `.csv` s'ouvre dans Kazo Spreadsheets.
- **Pas de renommage.** GridCraft n'a pas de `file.rename` : après un Enregistrer sous, son titre
  garde l'ancien nom ; la fenêtre de l'OS montre le fichier, et l'enregistrement suivant va au bon
  fichier (la liaison).
- **Garde de fermeture.** « Oui » montre chaque classeur modifié (`window.activate`) puis
  l'enregistre (`file.save`) : GridCraft compte un enregistrement remis à la page comme celui du
  classeur de sa fenêtre active. La fermeture attend d'abord la fin d'une écriture en cours :
  GridCraft compte le classeur comme enregistré dès qu'il a remis les octets, et fermer pendant
  l'écriture perdait le fichier (trouvé par un parcours Playwright).
- **Changement d'écran** (issue #4) : `canChangeScreen: false` attend que l'ADR 0263 de l'OS
  (Os#484, vers `main`) arrive dans la version de l'OS visée ; le chantier 4 le déclare.
- **Sélecteur.** Fichier › Ouvrir de GridCraft montre le dialogue de l'OS ; un classeur choisi
  s'ouvre dans la fenêtre si elle n'a qu'un classeur vierge intact, sinon dans une nouvelle
  fenêtre. Une image (Insertion › Image) est lue et remise à GridCraft.
- **Thème et langue.** `ui.set { dark }` à chaque changement du thème de l'OS,
  `app.language.set { language }` au démarrage.
- **CI.** Le module n'est pas encore sur nuget.org : la CI vérifie que le sous-module est sur
  `craft-host/base` (`KAZOOS_BRANCH`) et construit le profil Release avec les sources
  (`KazoOsUseSource=true`). Le chantier 4 remet le sous-module sur l'étiquette de la version de l'OS
  et retire ces deux variables. Rien n'est publié d'ici là.
- **Retour au module** (envoyé à la série `craft-host`) : `OpenDocument.ListFrom` lit la réponse de
  WordCraft (`documents[].id`, `path`) et rend une liste vide pour GridCraft ; `CraftLanguage.Pick`
  donne les codes de WordCraft (`zh-hans`, `pt-br`). Kazo Spreadsheets a les siens.
- **Suite (chantier 2b, `spreadsheets/2b-module-helpers`).** Le module a pris ce retour (Os#489,
  `craft-host/base` `3788bfb`) : la garde de fermeture lit `ui.inspect` avec
  `OpenDocument.ListFrom(inspect, SpreadsheetFormats.SaveNameOf)` et `GridCraftWorkbooks.cs` est
  retiré ; `GridCraftLanguage.Pick` passe la liste de GridCraft à `CraftLanguage.Pick`, qui se
  replie sur les deux lettres (`pt`, `sr`). Même comportement, testé (81 tests xUnit, 8 parcours
  Playwright).

Tests : 76 tests xUnit (manifeste, formats, `ui.inspect`, langue, raccourcis, page de l'iframe,
textes fr et en) et 8 parcours Playwright dans l'OS (`npm run test:e2e` après
`dotnet build -c Debug`) : WebGPU, WebGL 2 (`--webgl`), clavier, thème, auteur, deux fenêtres,
Ctrl+N, enregistrer puis réenregistrer en place, réouverture par `xdg-open`, F12 et Ctrl+Shift+S,
copie PDF, `.xlsm` non réécrit, garde de fermeture (Annuler, Oui puis Annuler, Oui), Fichier ›
Ouvrir par le dialogue de l'OS.

## Chantier 3 : compiler GridCraft depuis le fork (2026-11-13)

Même méthode que Kazo Word (`build-wordcraft.yml`) :

| Élément | Rôle |
| --- | --- |
| branche `kazo/v0.19.1` du fork `Kazo-ca/gridcraft` | le commit `476448d` de l'étiquette `v0.19.1` d'amont, qui n'est pas sur `main` du fork ; la branche le garde vivant |
| sous-module `external/gridcraft` | ce commit, `shallow`, branche `kazo/v0.19.1` ; seuls `build-gridcraft.yml` et un poste qui compile le récupèrent |
| `tools/build-gridcraft.sh` | vérifie Trunk 0.21.14, la cible `wasm32-unknown-unknown` et un sous-module propre, lance `packaging/web/package.sh` de GridCraft (date du commit, `GRIDCRAFT_BUILD_SHA`), puis `tools/vendor-gridcraft.sh --dir --rustc --trunk` |
| `.github/workflows/build-gridcraft.yml` | à la main : compile, valide le build sur `gridcraft/build-<version>-<commit>`, y lance `ci.yml` (appelable par `workflow_call`) et ouvre la PR ; sur une PR qui touche le build : compile seulement |
| `GridCraftBuildTests` | une provenance `source-build` doit nommer le fork et le commit du sous-module dans l'index de git |

Ce qui a été décidé en écrivant le code :

- **Ni craft-fonts ni craft-i18n.** Le job web de `craft-common-actions` n'ajoute craft-fonts que si
  l'app le demande, et GridCraft ne le demande pas : le zip officiel n'a pas de `OFL-*.txt`. Le
  script vide `CRAFT_FONTS_DIR` et les autres variables pour donner le même contenu.
- **Les workflows ne récupèrent que le sous-module de l'OS** (`ci.yml`, `publish-packages.yml`,
  `publish-to-kazoos-store.yml`) : l'app utilise le build versionné, pas les sources.
- **Rust stable courant.** Un premier build avec le Rust 1.97 du conteneur donnait une colle JS de
  4 Ko de plus ; avec 1.100 (stable du 2026-11-09, ce que la CI installe) la colle a la taille du
  zip officiel et ne diffère que par les noms de symboles. Le wasm fait 26,4 Mo (9,6 Mo en gzip -9),
  0,45 % de plus que l'officiel : le build n'est pas reproductible octet par octet d'une machine à
  l'autre, comme pour Kazo Word.
- **Le dépôt garde le build compilé depuis le fork** (provenance `source-build`, commit `476448d`),
  au lieu du zip officiel ; `tools/vendor-gridcraft.sh` sans argument le reprend au besoin.

Tests : 76 tests xUnit et les 8 parcours Playwright avec le build compilé.

## Avancement

- [x] Étape 0 : lecture de GridCraft 0.19.1 et du zip officiel.
- [x] Plan fr et en.
- [x] Réponses de Benoit à Q1 et Q2 (A et A).
- [x] Chantier 1 (`spreadsheets/1-solution`) : solution renommée, manifeste, zip 0.19.1 dans `wwwroot/gridcraft/` (paquet `.kazoapp` de 9,7 Mo, 26,5 Mo décompressé), sous-module de l'OS sur 3.3.2 ; la fenêtre montre la version de GridCraft en attendant le module.
- [x] Chantier 2 (`spreadsheets/2-window`) : fenêtre sur le module `Kazo.Os.AppSdk.Craft`, formats, ouvrir, enregistrer, Enregistrer sous (F12, Ctrl+Shift+S), garde de fermeture, sélecteur, thème, langue ; CI sur `craft-host/base`.
- [x] Chantier 3 (`spreadsheets/3-build-from-source`) : fork `Kazo-ca/gridcraft` (branche `kazo/v0.19.1`) en sous-module, `tools/build-gridcraft.sh`, workflow `build-gridcraft.yml`, build compilé depuis le fork dans `wwwroot/gridcraft/`.
- [ ] Chantier 4.
