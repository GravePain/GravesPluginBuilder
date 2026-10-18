# F-004 — The save writes entities it can never restore

**Status:** found, not fixed. Belongs to the save plugin and to spawning.
**Date:** 2026-09-05
**Found by:** the quest save round trip, test 2 of 3.

## What happened

Saving mid-run and loading it back worked — the quest came back at ten of ten, claimed, with the
hundred gold still in the purse. The load also reported five problems:

```
Saved entity 'd57c8840627045e4bbfeca50abbcd9a1' has no counterpart in the scene.
The scene has changed since this save was written.
```

Five of them, one per grunt. The scene had not changed. The grunts were spawned.

## Why

`SaveSystem`'s own doc comment says it: *"Entities spawned at runtime are NOT handled: nothing in
the framework spawns them yet, and inventing a scheme for a case that does not exist is how the
wrong scheme gets locked in."*

**That comment is now stale, and the code never enforced what it claimed.** Something does spawn
entities now — the spawning plugin, which I built. And the save path has no exclusion matching the
comment. It skips an entity whose instance id is *empty*, and a spawned entity's id is not empty:
`Spawner` makes a bare `GameObject`, so `GameEntity.Awake` mints a fresh GUID for it. A fresh GUID
every session.

So the save writes five entities out, and the load cannot match any of them, every time.

## Why this matters more than the noise

The warnings are cosmetic. The reasoning behind them is not.

That message exists to tell a designer the scene has changed under a save — a genuinely useful
signal, and the exact situation where a player would otherwise experience things quietly missing.
Five false ones per load train whoever reads the console to ignore it. **A diagnostic that cries
wolf is worse than no diagnostic**, because the real one now arrives as the sixth line in a list of
five that never mattered.

## The two honest answers, and why I did not pick one

1. **Exclude runtime-spawned entities from the save.** Correct today and a lie tomorrow. The moment
   a spawned thing carries something worth keeping — a named enemy, a boss at half health, a chest
   somebody has already opened — it silently stops persisting.
2. **Restore them by respawning them.** This is the real feature: a save records that a spawner had
   five grunts and what state each was in, and a load recreates them. It needs the spawner and the
   save system to agree on who owns a spawned thing's identity, which is a design conversation, not
   a guard clause.

Neither is a quest plugin's business, and doing the cheap one would foreclose the right one. Written
down instead.

## What would settle it

Whether a spawned entity's identity is minted by the spawner from something stable (the spawner's
own persistent id plus an index), or whether the save file records the spawner's population and the
spawner rebuilds it on load. The second is more honest about what a spawner is for. Related:
[F-002](F-002-state-is-the-storage.md) — the state is the storage, and a spawned entity's state
currently has nowhere durable to be stored *to*.
