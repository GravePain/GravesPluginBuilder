# Changelog

All notable changes to this package.

## [0.1.0] — 2026-09-05

First version. Built in roughly twelve hours by two agents working independently from one
repository, with a third reviewing read-only.

### Core

- **Definitions** as ScriptableObjects, with a `Uid` minted once and never reassigned. Nothing is
  keyed by name, index or object reference.
- **Modules** — the extension point. A plugin declares a `DefinitionModule` subclass and the core
  stores it without knowing the type.
- **Instances** — a particular thing rather than a kind of thing, with its own identity and its own
  `InstanceState`.
- **`GameEntity`** — spawns a body, attaches modules, then stops. No update loop, by design.
- **`GameEntity.Create`** — the supported way to make an entity from script. Starts from something not yet awake, so the definition can be set before `Awake` runs.
- **`DefinitionRegistry`** — resolves a uid, and reports integrity problems rather than repairing
  them: missing assets, duplicate uids, unresolvable modules.
- **`ISaveable`** — the persistence contract. No format; that is a plugin's decision.
- **`CoreSettings`** — the registry, a default representation slot and a base entity prefab slot, loaded from `Resources`. Every slot may be left empty, and the core says what it fell back to.

### Editor

- **One authoring window.** Every definition kind in tabs, with search, create, duplicate, delete,
  and module ordering. A plugin's fields render themselves, and a plugin may supply a property
  drawer that will be picked up.
- **A Manual tab** rendering this package's documentation and the project's own.
- **Validate** — integrity checks including two plugins claiming one state key.
- **Report** — a diagnostic describing the installation, for bug reports from projects nobody else
  can run.

### Known limits

Written down rather than discovered. See `Documentation~/overview.md`.

- Packaging is designed and only partly built; plugins install by copying folders.
- Entities spawned at runtime do not survive a save.
- A stack has one identity for many objects, and there is no split operation.
- The authoring window draws a raw module list beside the good one.
