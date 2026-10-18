# Packaging

The agreed distribution shape, and the reasoning, so it is not re-litigated later.

**Decided 2026-09-05. Not yet implemented, deliberately — see the last section.**

## One package, plugins as samples

A single UPM package containing the core, with each plugin shipped in `Samples~` and declared in the
`samples` array of `package.json`. Nothing installs itself. The Package Manager shows an Import
button per plugin, and importing **copies it into `Assets/`**.

```
com.graves.core/
  package.json          <- declares each plugin under "samples"
  Runtime/              <- Assets/Graves/Core
  Editor/               <- Assets/Graves/Core/Editor
  Samples~/
    Animation/          <- Assets/Graves/Plugins/Animation
    ...
```

The project's own data never ships. `Assets/Graves/Data` stays in the project, and
`Assets/Graves/TestBed` never leaves this repository at all.

## Why samples rather than separate packages

**Because a package is read-only and a plugin should not be.** A UPM package lives in
`Library/PackageCache` and cannot be edited. Plugins are where game-specific decisions live, and
Graves works by modifying things in place — *"I smash two things in editor together and work it
through until it's right."* A sample lands in `Assets/` as ordinary hackable source. The core stays
immutable where immutability is a virtue; plugins stay editable where editability is the point.

**Because independent versioning solves a problem that does not exist here.** Separate packages per
plugin would give proper version ranges, at the cost of a repository per plugin and a dependency
graph to maintain. Graves is solo and does not take updates — an asset is a block of code he wants,
not an ongoing vendor relationship. The maintenance is real and the benefit would never be collected.

If one plugin ever grows its own life, split that one out then. Splitting later is easy; merging back
is not.

## The core does not install anything

No package manager inside the framework. That would re-implement UPM and put network and
manifest-editing code into a core that currently references `netstandard` and
`UnityEngine.CoreModule` and nothing else.

It does not need to *find* plugins either. Module types are discovered by reflection the moment they
compile, which is how the authoring window picked up `AnimationModule` with no core edit.

## The folder split already matches

| Now | Ships as |
|---|---|
| `Assets/Graves/Core` | package `Runtime` + `Editor` |
| `Assets/Graves/Plugins/*` | `Samples~/*`, one per plugin |
| `Assets/Graves/Data` | nothing — stays in the project |
| `Assets/Graves/TestBed` | nothing — never leaves this repository |

That split came from keeping framework and project data apart, and it happens to be exactly the
packaging boundary. Nothing needs restructuring when the time comes.

**One thing to protect:** `GravesCoreSettings` lives in an `Assets` Resources folder and must stay
there. It is project configuration, and inside the package it would be read-only.

## Why this is not built yet

Packaging turns every core change into a version bump and a reinstall, and the core is still changing
daily. Do it when the core stops surprising us — realistically after two or three more plugins, once
the parts of the contract that are wrong have shown themselves.

The decision has no deadline. Making it early costs velocity and buys nothing.
