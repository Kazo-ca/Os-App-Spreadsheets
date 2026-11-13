# Kazo Spreadsheets

Un tableur pour Kazo OS, pour les classeurs `.xlsx`, `.ods` et `.csv`. Kazo Spreadsheets est basé
sur [GridCraft](https://github.com/storytold/gridcraft) de l'équipe ArtCraft, un tableur libre
écrit en Rust, qui tourne ici dans une fenêtre de Kazo OS.

## Calculer

- Feuilles, formules et les fonctions de feuille de calcul attendues (plus de 500), formats de
  nombre, mise en forme conditionnelle, tableaux, tri et filtres, graphiques, tableaux croisés
  dynamiques.
- Le ruban de GridCraft, sa barre de formule, sa recherche de commandes et ses raccourcis.
- Plusieurs fenêtres à la fois, chacune avec ses classeurs.

## Vos fichiers Kazo OS

- Double-cliquez sur un fichier `.xlsx`, `.xlsm`, `.xlsb`, `.xls`, `.ods`, `.slk`, `.csv` ou `.tsv`
  dans l'Explorateur de fichiers, ou utilisez « Ouvrir avec » sur un fichier `.txt` ou `.json` :
  le classeur s'ouvre dans Kazo Spreadsheets.
- Enregistrer écrit du `.xlsx`. Un `.xlsx` ouvert est réécrit en place ; un autre format n'est
  jamais écrasé par des données `.xlsx`, donc son premier enregistrement demande un nom dans le
  dialogue d'enregistrement de Kazo OS. Enregistrer sous (F12 ou Ctrl+Maj+S) et le premier
  enregistrement d'un nouveau classeur passent aussi par ce dialogue.
- Les exportations en PDF, CSV et HTML sont des copies : le classeur reste lié à son fichier.
- Fermer la fenêtre demande avant de perdre des modifications, pour chaque classeur ouvert.
- Fichier › Ouvrir et les images insérées viennent des fichiers Kazo OS, par le dialogue de
  Kazo OS.
- Kazo Spreadsheets suit le thème (clair ou sombre) et la langue de l'OS ; ses réglages sont gardés
  par utilisateur Kazo OS dans `~/.config/ca.kazo.spreadsheets/`.

## Limites connues

- **GridCraft est en alpha.** La plupart des classeurs s'ouvrent et s'enregistrent bien, mais
  gardez une copie des plus importants.
- Le bouton Enregistrer sous de GridCraft (page Fichier) enregistre sous le nom actuel : utilisez
  F12 ou Ctrl+Maj+S pour Enregistrer sous.
- **Écritures asiatiques** : la version web de GridCraft n'a pas de police chinoise, japonaise ou
  coréenne ; ce texte peut ne pas s'afficher, et ces langues d'interface passent en anglais.
- GridCraft dessine avec **WebGPU ou WebGL 2** : un navigateur ou un ordinateur sans l'un ni l'autre
  montre un écran d'erreur au lieu du classeur.
- Le téléchargement fait environ 10 Mo (27 Mo une fois installé), et chaque fenêtre charge sa propre
  copie de GridCraft.
- La fenêtre reste sur l'écran où elle s'est ouverte : la passer sur un autre écran rechargerait
  GridCraft et perdrait les classeurs ouverts.
- L'impression passe par le dialogue d'impression du navigateur.
- Demande Kazo OS 3.3.3 ou plus récent.

## Licence

Kazo Spreadsheets est un logiciel libre sous licence MIT. GridCraft est sous licence MIT ou
Apache-2.0 ; ses avis et les licences des polices qu'il contient sont livrés avec l'app (voir
`THIRD-PARTY-NOTICES-fr.md` dans le dépôt). Le nom et les logos ArtCraft sont des marques de
l'équipe ArtCraft et ne sont pas utilisés par Kazo Spreadsheets.
