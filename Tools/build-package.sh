#!/usr/bin/env bash
#
# Assembles the distributable package from this repository.
#
# The package is BUILT rather than kept, because a hand-maintained copy drifts from the source
# within a day and the drift is invisible until someone installs it. Run this, take the output
# folder, drop it in another project's Packages/ directory.
#
#   Tools/build-package.sh [output-directory]
#
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${1:-$(dirname "$REPO")/_Package}/com.graves.core"
CORE="$REPO/Assets/Graves/Core"
PLUGINS="$REPO/Assets/Graves/Plugins"

# The exemplars. A plugin earns a place here by being a shape worth copying AND by having nothing
# outstanding against it - no open reviewer finding, no decision whose outcome is still unknown. A
# cost that was stated and accepted is not a disqualification; that is the framework working.
#
# The set is dependency-closed on purpose: importing it gives something that compiles, rather than a
# folder of red errors and a reader who assumes they did it wrong.
EXEMPLARS=(
  Vitals          # data and behaviour, with state as the storage
  Locomotion      # behaviour only; here because Death needs it
  Death           # a bridge: joins two plugins so neither depends on the other
  Inventory       # a collection, and definitions referenced rather than embedded
  Quests          # cross-plugin, with optional halves that install separately
  Animation       # a runtime built from a technique rather than a copied asset
)

# Two former exemplars are no longer plugins at all. Stats moved into the core because removing it
# killed seven plugin roots - a standard library mislabelled as optional - and Save moved into the
# core so that the core does something on its own. Both ship with the package now, not beside it.
#
# Deliberately absent, and why. Kept here rather than in a document because the next person to add a
# plugin to the list will read this file, not that document.
#
#   Progression   the shape Vitals already demonstrates, and one per shape is the point
#   Input         D-002: scope deliberately cut, outcome not yet known
#   Combat        D-004: a migration cost that has not been paid yet
#   Animation.Demo  F-015 open
#   Crafting, Currency, Equipment, Enhancement, Skills, AI, Camera, UI
#                 useful, but each repeats a shape already shown above

say() { printf '  %s\n' "$*"; }

rm -rf "$OUT"
mkdir -p "$OUT"

say "package root: $OUT"

# --- the package proper -------------------------------------------------------------------------
for entry in package.json CHANGELOG.md LICENSE.md; do
  cp "$CORE/$entry" "$OUT/"
done

# Runtime and editor code, with their .meta files. The metas travel so that GUIDs stay stable: drop
# a second copy of this package into a project that already had one and nothing re-links.
mkdir -p "$OUT/Runtime" "$OUT/Editor"
find "$CORE" -maxdepth 1 -name "*.cs" -o -maxdepth 1 -name "*.cs.meta" -o -maxdepth 1 -name "*.asmdef" -o -maxdepth 1 -name "*.asmdef.meta" | while read -r f; do
  cp "$f" "$OUT/Runtime/"
done
cp -r "$CORE/Editor/." "$OUT/Editor/"
cp -r "$CORE/Documentation~" "$OUT/Documentation~"

say "core: $(find "$OUT/Runtime" "$OUT/Editor" -name '*.cs' | wc -l) scripts, $(find "$OUT/Documentation~" -name '*.md' | wc -l) documentation pages"

# --- samples ------------------------------------------------------------------------------------
# Samples~ has a tilde, so Unity's asset pipeline never sees it. Nothing here is compiled until
# somebody chooses to import it, which is what lets a sample reference a tool this package does not
# ship.
mkdir -p "$OUT/Samples~/Plugins"
for plugin in "${EXEMPLARS[@]}"; do
  if [ ! -d "$PLUGINS/$plugin" ]; then
    echo "MISSING exemplar: $plugin" >&2
    exit 1
  fi
  cp -r "$PLUGINS/$plugin" "$OUT/Samples~/Plugins/$plugin"
done

# Sub-assemblies that leave the set. A plugin's own folder can contain assemblies that reach
# outside it - the optional halves that make the plugin-to-plugin shape worth showing - and some of
# those reach at something not shipped here. Dropped by name, with the reason, rather than by a rule
# that would also drop the halves this package exists to demonstrate.
DROP=(
  Quests/Combat        # needs Graves.Plugin.Combat, which D-004 leaves a migration cost against
  Locomotion/Player    # needs Graves.Plugin.Input, which D-002 deliberately left half-built
  Animation/Demo       # F-015 open
)
for path in "${DROP[@]}"; do
  rm -rf "$OUT/Samples~/Plugins/$path" "$OUT/Samples~/Plugins/$path.meta"
done

# --- the closure check ----------------------------------------------------------------------
# Enforced rather than promised, the same way the core's independence from plugins is. A sample set
# that does not compile teaches the reader that they installed it wrong.
python - "$OUT/Samples~/Plugins" <<'CHECK'
import json, io, os, sys

root = sys.argv[1]
shipped, refs = set(), {}
for base, _dirs, files in os.walk(root):
    for name in files:
        if not name.endswith(".asmdef"):
            continue
        data = json.load(io.open(os.path.join(base, name), encoding="utf-8-sig"))
        shipped.add(data["name"])
        refs[data["name"]] = data.get("references", [])

# Provided by the package itself, or by Unity.
external = {"Graves.Core"}

missing = []
for name, needed in sorted(refs.items()):
    for r in needed:
        if r in shipped or r in external or r.startswith("Unity."):
            continue
        missing.append(name + " needs " + r)

if missing:
    print("SAMPLES DO NOT CLOSE:", file=sys.stderr)
    for m in missing:
        print("  " + m, file=sys.stderr)
    sys.exit(1)

print("  closure: %d assemblies, every reference resolves" % len(shipped))
CHECK

say "samples: $(ls "$OUT/Samples~/Plugins" | wc -l) exemplar plugins"

# The bridge sample is committed rather than copied from Plugins - it cannot compile in this
# repository, because the tool it adopts belongs to another project.
if [ -d "$CORE/Samples~/CombatBridge" ]; then
  cp -r "$CORE/Samples~/CombatBridge" "$OUT/Samples~/CombatBridge"
  say "samples: + CombatBridge (does not compile until pointed at your own tool)"
fi

# --- the project scaffold -----------------------------------------------------------------------
# Not part of the Unity package - it goes in the consuming project's ROOT, beside Assets. Kept in
# this repository and copied out by this script for the same reason the package is built rather than
# maintained: a second copy drifts, and the drift is invisible.
SETUP="$(dirname "$OUT")/ProjectSetup"
rm -rf "$SETUP"
cp -r "$REPO/ProjectSetup" "$SETUP"
say "project scaffold: $SETUP  ($(find "$SETUP" -name '*.md' | wc -l) files)"

say "done"

cat <<INSTALL

  To use it in another project:

    1. copy  $OUT
       into  <that project>/Packages/com.graves.core

    2. copy the CONTENTS of  $SETUP
       into  <that project>/          (CLAUDE.md, .claude/, Docs/ sit beside Assets)

    3. in Unity: Window > Package Manager > Graves Core > Samples > Import
       Take "Exemplar plugins" to start. Do not take the Combat Bridge until you have a
       tool to point it at - it does not compile on its own, deliberately.

INSTALL

# --- verifying an install, when it matters ------------------------------------------------------
#
# The closure check above is static. It proves every reference resolves; it does not prove Unity
# accepts the package. To prove that, install it somewhere clean:
#
#   Unity.exe -batchmode -quit -nographics -createProject <fresh project> -logFile <log>
#   cp -r <this package> <fresh project>/Packages/com.graves.core
#   cp -r <package>/Samples~/Plugins/. "<fresh project>/Assets/Samples/Graves Core/0.1.0/Exemplar plugins/"
#   Unity.exe -batchmode -quit -nographics -projectPath <fresh project> -logFile <log>
#   grep -i "error CS" <log>
#
# Done on 2026-09-05: eleven assemblies, no errors, the manual found its documentation inside the
# installed package, nine module types discovered by reflection, and the core referenced no plugin.
