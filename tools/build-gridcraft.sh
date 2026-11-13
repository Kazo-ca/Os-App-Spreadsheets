#!/usr/bin/env bash
# Compiles GridCraft from the external/gridcraft submodule (our fork Kazo-ca/gridcraft, work item 3
# of doc/en/plan.md) the way its own release workflow builds the web zip, then vendors the result
# in src/Kazo.Spreadsheets/wwwroot/gridcraft/ with tools/vendor-gridcraft.sh --dir. The
# .github/workflows/build-gridcraft.yml workflow runs it; it also runs on a developer machine.
#
# Usage:
#   tools/build-gridcraft.sh
#
# Needs: Rust stable (1.95 or later) with the wasm32-unknown-unknown target, Trunk at the version
# below, zip and unzip. GridCraft's web release is built without craft-fonts and craft-i18n
# (the web job of storytold/craft-common-actions only adds craft-fonts when an app asks for it, and
# GridCraft does not), so neither is needed here: our build matches the official zip of the same
# commit.
set -euo pipefail

# The Trunk version of GridCraft's release workflow (craft-common-actions, install-trunk).
trunk_version="0.21.14"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
gridcraft="$repo_root/external/gridcraft"

case "${1:-}" in
  "") ;;
  -h|--help) sed -n '2,14p' "$0"; exit 0 ;;
  *) echo "Unknown argument: $1" >&2; exit 2 ;;
esac

fail() { echo "error: $*" >&2; exit 1; }

[[ -f "$gridcraft/Cargo.toml" ]] || fail "external/gridcraft is empty. Run: git submodule update --init external/gridcraft"
# A local change of GridCraft must go through a commit of the fork, so the provenance names it.
[[ -z "$(git -C "$gridcraft" status --porcelain)" ]] || fail "external/gridcraft has local changes; commit them in the fork first"

command -v trunk >/dev/null || fail "trunk not found; install Trunk $trunk_version"
actual_trunk="$(trunk --version | awk '{print $2}')"
[[ "$actual_trunk" == "$trunk_version" ]] || fail "trunk $actual_trunk found; GridCraft's release uses $trunk_version"
rustup target list --installed 2>/dev/null | grep -qx wasm32-unknown-unknown \
  || fail "the wasm32-unknown-unknown target is missing (rustup target add wasm32-unknown-unknown)"
command -v zip >/dev/null && command -v unzip >/dev/null || fail "zip and unzip are needed"

commit="$(git -C "$gridcraft" rev-parse HEAD)"
rustc_version="$(rustc --version)"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

# GridCraft's own packaging script: trunk build --release, then the licences, NOTICE and
# ATTRIBUTION.md next to the site. The build date is the commit's (UTC), so building the same
# commit twice bakes the same date into the About window. CRAFT_FONTS_DIR is cleared so a value
# left in the developer's shell does not embed fonts the official web zip does not have.
build_date="$(TZ=UTC git -C "$gridcraft" log -1 --format=%cd --date=format-local:%Y-%m-%d)"
echo "Building GridCraft $commit (rustc: $rustc_version, trunk $actual_trunk)"
(
  unset CRAFT_FONTS_DIR CRAFT_FONTS_REQUIRED CRAFT_I18N_DIR CRAFT_I18N_REQUIRED
  export GRIDCRAFT_BUILD_SHA="$commit" GRIDCRAFT_BUILD_DATE="$build_date"
  export DIST="$work/dist"
  export CARGO_TARGET_DIR="${CARGO_TARGET_DIR:-$gridcraft/target}"
  cd "$gridcraft"
  packaging/web/package.sh
)

shopt -s nullglob
zips=("$work"/dist/gridcraft-web-*.zip)
shopt -u nullglob
[[ ${#zips[@]} -eq 1 ]] || fail "packaging/web/package.sh made no single gridcraft-web-*.zip"
unzip -q "${zips[0]}" -d "$work/site"
site=("$work"/site/gridcraft-web-*)
[[ ${#site[@]} -eq 1 && -d "${site[0]}" ]] || fail "the zip has no single gridcraft-web-*/ folder"

"$repo_root/tools/vendor-gridcraft.sh" --dir "${site[0]}" \
  --rustc "$rustc_version" --trunk "$actual_trunk"
