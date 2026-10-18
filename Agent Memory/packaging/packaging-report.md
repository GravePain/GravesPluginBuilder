# Installing the framework somewhere else

**Test 3 of 3.** `package.json` had existed since the first day and nobody had ever used it. This is
what happened when someone did.

**Result: it works.** A fresh Unity project, the core added as a UPM path dependency, three plugin
folders copied into `Assets`, and the whole chain runs — content authors, an entity spawns, takes
damage, saves, and comes back. Nine checks and one play mode test, all green from a clean state.

Everything below is what was wrong, missing, or quietly assumed the repository was there.

## What was done

A new project at `K:\Protected\PackageInstallTest\FreshGame`, created by
`Unity.exe -createProject` on 6000.3.9f1 — empty, no URP, no uGUI, no input system. Then:

```json
"com.graves.core": "file:../../../GravesPluginBuilder/GravesPluginBuilder/Assets/Graves/Core"
```

and `Assets/Graves/Plugins/{Stats, Vitals, Save}` copied in by hand. Nothing else.

The proof runs in two halves, both in batch mode, and the sources are beside this file:

| File | What it covers |
|---|---|
| `InstallProof.cs` | Package resolution, the editor half, authoring, the registry |
| `SpawnAndPersistTests.cs` | Spawning, damage, the save round trip |
| `InstallGaps.cs` | Measurements of the gaps, rather than assertions about them |

Both halves passed. `proof-report.txt` and `gaps-report.txt` are the raw output.

## Findings

### 1. The package lives inside another project's `Assets` folder

`resolvedPath` came back as
`K:\Protected\GravesPluginBuilder\GravesPluginBuilder\Assets\Graves\Core`.

It resolves, it compiles, `PackageInfo.FindForAssembly` reports `com.graves.core@0.1.0 source=Local`,
and both assemblies — runtime and editor — come across intact. Unity does not object.

**But a consuming project is pointing at a path inside a different project's `Assets` tree.** That
has consequences nobody has met yet:

- The two copies share `.meta` GUIDs, because they are literally the same files.
- The core compiles twice in the world: as ordinary project code in the source repository, and as a
  package everywhere else. A change made in the consumer's `Packages` view edits the source repo.
- There is no version to pin to. `file:` is whatever is on disk right now.

**The fix is a repository layout, not a code change.** The publishable core wants to live at
`Packages/com.graves.core/` in the repo, or in a repository of its own, with the game project
consuming it the same way any other project would. That is the arrangement that makes the source
repo eat its own dog food, and it is currently the only project in the world that does not.

### 2. The package ships no documentation of any kind

Measured, all false: `README.md`, `CHANGELOG.md`, `LICENSE.md`, `Documentation~`. And
`documentationUrl` is `NONE`.

The Package Manager window shows an empty details pane for it. Someone who installs this framework
is told nothing at all about what it is or how to start — while the repository contains a `Docs`
folder written specifically to tell them, and an authoring window with a Manual tab built to display
it.

### 3. The manual does not travel, and says nothing when it fails

`ManualPage.FindPages()` returned **0** in the fresh project. Both roots were absent:

```
root 'Docs' exists: False
root 'Agent Memory' exists: False
```

The roots resolve to `<projectRoot>/Docs` and `<projectRoot>/Agent Memory`, which is correct for the
repository and meaningless anywhere else. `FindPages` simply skips a root that does not exist, so
the Manual tab in a fresh install is **silently empty** — no message, no explanation, no hint that
the documents exist somewhere and were left behind.

This one is mine and it is the sharpest of the lot, because I built that tab two days ago and wrote
in its own comment that the documents are "things the next author needs". The next author is exactly
who does not get them.

Two things want fixing, and they are separable:

- The framework's own `Docs` should ship inside the package, where the packaged manual can find them
  without the repository. A game's own notes stay project-relative, which is right.
- An empty Manual tab should say why it is empty.

### 4. `DataRoot` is a compile-time constant inside the package

```
data root the package insists on: Assets/Graves/Data
```

`DefinitionCatalogue.DataRoot` is a `const string`. A consuming project cannot move its authored
content, and cannot change it without forking the package. It works — the folder was created from
nothing and everything landed in it — but a framework telling a game where its own content must live
is the framework overreaching.

Small, and the fix is small: a field on `CoreSettings`, which is already a per-project asset and is
already the right home for it.

### 5. The core has no supported way to create an entity from a definition

Every path into the runtime goes through a serialised `_definition` field that only an editor or a
prefab can fill in. The spawning plugin gets past this with reflection and says so plainly in its own
comment — *"it is still a reach past the core rather than through it, and should not spread. If a
second plugin needs this, the core should grow an honest way."*

Installing the framework elsewhere is the second caller. Anyone building content from script hits
this on their first afternoon and either finds the reflection trick or gives up.

### 6. The runtime cannot be smoke-tested from `-executeMethod`, and nothing says so

`GameEntity` does its entire job in `Awake`, and Unity does not call `Awake` in edit mode for an
ordinary component. So a batch-mode `-executeMethod` run can author content flawlessly and never
once produce a live thing — which is exactly what happened, twice, before I stopped guessing and
checked the log for the error message that would have been there if `Awake` had run at all. It was
not there.

This is not a defect. The core is right to keep its runtime out of the editor. But it means the only
way to test the runtime is a play mode test, and **a play mode test assembly must not be
Editor-only** — restricting it to the Editor platform produces EditMode tests, and the run reports
`testcasecount="0"` and `result="Passed"`, which is the worst possible way to be told nothing ran.

The working shape, for whoever does this next: author the content in an edit-mode pass, save a
prefab under `Resources`, and let an unrestricted play mode assembly load it the way a shipped game
would. That is what `SpawnAndPersistTests.cs` does.

### 7. Plugins are not packages at all

They were installed by copying folders. Each plugin is a clean assembly with name-based references
that bind to the packaged core without edits — that part worked perfectly, and it is the whole
plugin argument holding up under the only test that matters. But "install the plugins you need" is
currently a file manager operation.

Not urgent, and possibly never urgent for a single game. It becomes urgent the first time two
projects want different versions of the same plugin.

## One thing I got wrong

I predicted that a `GameEntity` added from script would get no persistent id, because the id is
minted in `OnValidate` and I believed `AddComponent` from code triggers neither `Reset` nor
`OnValidate` — and that a save would therefore never find such an entity again. The proof set the id
by hand to work around it.

Measured:

```
persistent id after AddComponent from script: e1d87f3f29a04c11a37363d8b90fad67
persistent id on the saved prefab:            e1d87f3f29a04c11a37363d8b90fad67
```

Wrong. Unity calls `OnValidate` on `AddComponent` in the editor, the id is minted, and it survives
onto the prefab. The workaround was deleted and the comment now records the mistake instead.

It is the same failure the journal already has an entry about: a claim that sounded like a defect,
believed because it flattered the checking, and not measured until something forced it. The only
difference is that this one was measured before it was written down.

## What did not need fixing

Worth saying, because a report of only problems is not an honest picture.

- Both core assemblies crossed the package boundary intact, editor half included.
- Three plugin assemblies compiled against a packaged core with **no changes of any kind** — the
  name-based references in their asmdefs simply bound.
- `DefinitionCatalogue.GetOrCreateSettings()` built the settings asset, the `Resources` folder and
  the registry in a project that had never seen any of them.
- The save round trip worked first time, in a project with a different
  `Application.persistentDataPath`, a different company name and a different product name.
- Not one line of the framework needed editing to make any of this happen.
