#!/usr/bin/env bash
# Puts a GridCraft web build in src/Kazo.Spreadsheets/wwwroot/gridcraft/ (decision 4 of
# doc/en/plan.md; the same method as Kazo Word's vendor-wordcraft.sh). That folder is never edited by hand: this script
# replaces it whole and writes gridcraft.json, its provenance (version, source, name, size and
# SHA-256 of each file). tests/Kazo.Spreadsheets.Tests checks the files still match it.
#
# Usage:
#   tools/vendor-gridcraft.sh                 download the pinned official zip and check it
#   tools/vendor-gridcraft.sh --zip <file>    use a zip already downloaded (still checked)
#   tools/vendor-gridcraft.sh --dir <folder>  use our own build of external/gridcraft (work item 3)
#
# --dir takes a folder laid out like the zip's gridcraft-web-<version>/, as GridCraft's
# packaging/web/package.sh makes it from the external/gridcraft submodule. The provenance then
# names the fork and the submodule's commit instead of the zip. tools/build-gridcraft.sh makes
# that folder and calls this script with it, adding --rustc and --trunk so the provenance also says
# what the build used (--craft-i18n and --craft-fonts stay available, but GridCraft's web release
# uses neither).
set -euo pipefail

# The pinned release. Moving to another version changes these three lines, in a PR.
version="0.19.1"
zip_url="https://github.com/storytold/gridcraft/releases/download/v${version}/gridcraft-web-${version}.zip"
zip_sha256="48c5d1b101befebb3c5ba3b7a14914f01c3207934813ed07dc4251b7ae116a78"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
target="$repo_root/src/Kazo.Spreadsheets/wwwroot/gridcraft"
submodule="$repo_root/external/gridcraft"

fail() { echo "$*" >&2; exit 1; }

zip_file=""
build_dir=""
i18n_rev=""
fonts_rev=""
rustc_version=""
trunk_version=""
while [[ $# -gt 0 ]]; do
  [[ $# -ge 2 || "$1" == -h || "$1" == --help ]] || fail "$1 needs a value"
  case "$1" in
    --zip) zip_file="$2"; shift 2 ;;
    --dir) build_dir="$2"; shift 2 ;;
    --craft-i18n) i18n_rev="$2"; shift 2 ;;
    --craft-fonts) fonts_rev="$2"; shift 2 ;;
    --rustc) rustc_version="$2"; shift 2 ;;
    --trunk) trunk_version="$2"; shift 2 ;;
    -h|--help) sed -n '2,17p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done
[[ -z "$zip_file" || -z "$build_dir" ]] || fail "--zip and --dir are two different sources; give one"

# Escapes a value for a JSON string (the rustc version is free text).
json_string() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  printf '"%s"' "$value"
}

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

if [[ -n "$build_dir" ]]; then
  # Our build: the provenance names the fork and the submodule's commit, which must be clean so
  # the commit alone says what was compiled.
  [[ -d "$build_dir" ]] || fail "$build_dir is not a folder"
  source_dir="$(cd "$build_dir" && pwd)"
  [[ -f "$submodule/Cargo.toml" ]] || fail "external/gridcraft is empty. Run: git submodule update --init external/gridcraft"
  [[ -z "$(git -C "$submodule" status --porcelain)" ]] || fail "external/gridcraft has local changes; the provenance could not name what was built"
  commit="$(git -C "$submodule" rev-parse HEAD)"
  repository="$(git -C "$repo_root" config -f .gitmodules submodule.external/gridcraft.url)"
  version="$(awk '/^\[/ { in_pkg = ($0 == "[workspace.package]"); next }
                  in_pkg && $1 == "version" { gsub(/[" ]/, "", $3); print $3; exit }' "$submodule/Cargo.toml")"
  [[ -n "$version" ]] || fail "no [workspace.package] version in external/gridcraft/Cargo.toml"
else
  if [[ -z "$zip_file" ]]; then
    zip_file="$work/gridcraft-web-${version}.zip"
    echo "Downloading $zip_url"
    curl --fail --location --silent --show-error --retry 3 -o "$zip_file" "$zip_url"
  fi

  actual="$(sha256sum "$zip_file" | cut -d' ' -f1)"
  [[ "$actual" == "$zip_sha256" ]] || fail "SHA-256 mismatch for $zip_file: expected $zip_sha256, got $actual"

  unzip -q "$zip_file" -d "$work/zip"
  source_dir="$work/zip/gridcraft-web-${version}"
  [[ -d "$source_dir" ]] || fail "The zip has no gridcraft-web-${version}/ folder"
fi

# Exactly one glue module and one .wasm, named by Trunk with the same hash.
shopt -s nullglob
glues=("$source_dir"/gridcraft-web-*.js)
wasms=("$source_dir"/gridcraft-web-*_bg.wasm)
shopt -u nullglob
if [[ ${#glues[@]} -ne 1 || ${#wasms[@]} -ne 1 ]]; then
  fail "Expected one gridcraft-web-*.js and one gridcraft-web-*_bg.wasm in $source_dir"
fi
glue="$(basename "${glues[0]}")"
wasm="$(basename "${wasms[0]}")"
for needed in snippets LICENSE-MIT LICENSE-APACHE NOTICE ATTRIBUTION.md; do
  [[ -e "$source_dir/$needed" ]] || fail "$needed is missing from $source_dir (make it with packaging/web/package.sh)"
done

# What the app needs: the glue, the .wasm, the snippets the glue imports, and every licence and
# notice. Left out: GridCraft's own page (host/index.html replaces it), its icon (the ArtCraft
# brand is not ours to show), and the hosting notes.
rm -rf "$target"
mkdir -p "$target"
cp "$source_dir/$glue" "$source_dir/$wasm" "$target/"
cp -R "$source_dir/snippets" "$target/"
cp "$source_dir"/LICENSE-* "$source_dir/NOTICE" "$source_dir/ATTRIBUTION.md" "$target/"
shopt -s nullglob
font_licences=("$source_dir"/OFL-*.txt)
shopt -u nullglob
[[ ${#font_licences[@]} -eq 0 ]] || cp "${font_licences[@]}" "$target/"

# The provenance file, files sorted by path so a new version gives a readable diff.
{
  printf '{\n'
  printf '  "version": "%s",\n' "$version"
  printf '  "source": {\n'
  if [[ -n "$build_dir" ]]; then
    printf '    "kind": "source-build",\n'
    printf '    "repository": %s,\n' "$(json_string "$repository")"
    printf '    "commit": "%s"' "$commit"
    [[ -z "$i18n_rev" ]] || printf ',\n    "craftI18n": "%s"' "$i18n_rev"
    [[ -z "$fonts_rev" ]] || printf ',\n    "craftFonts": "%s"' "$fonts_rev"
    [[ -z "$rustc_version" ]] || printf ',\n    "rustc": %s' "$(json_string "$rustc_version")"
    [[ -z "$trunk_version" ]] || printf ',\n    "trunk": %s' "$(json_string "$trunk_version")"
    printf '\n'
  else
    printf '    "kind": "release-zip",\n'
    printf '    "url": "%s",\n' "$zip_url"
    printf '    "sha256": "%s"\n' "$zip_sha256"
  fi
  printf '  },\n'
  printf '  "glue": "%s",\n' "$glue"
  printf '  "wasm": "%s",\n' "$wasm"
  printf '  "files": [\n'
  first=1
  while IFS= read -r path; do
    size="$(stat -c %s "$target/$path")"
    sha="$(sha256sum "$target/$path" | cut -d' ' -f1)"
    [[ $first -eq 1 ]] || printf ',\n'
    first=0
    printf '    { "path": "%s", "size": %s, "sha256": "%s" }' "$path" "$size" "$sha"
  done < <(cd "$target" && find . -type f ! -path ./gridcraft.json | sed 's#^\./##' | LC_ALL=C sort)
  printf '\n  ]\n'
  printf '}\n'
} > "$target/gridcraft.json"

echo "GridCraft $version vendored in ${target#"$repo_root"/}: $glue, $wasm"
