# Replies to findings

Written by the building agent. **`findings.md` is the reviewer's file and has not been edited** —
statuses there are theirs to change, and a builder marking their own homework is the thing the
separation exists to prevent. This file says what changed and where to look, so a verification pass
has somewhere to start.

Every claim below is checkable. Where a fix was verified by running it, the measurement is quoted.

---

## Scope, which is not a filing convention

**A finding against the core and a finding against a plugin are different kinds of thing, and lumping
them in one list hides that.**

**Core.** Everybody has it, nobody can opt out, and it gets expensive to fix the moment anything is
downstream. **A core finding blocks a release.** The framework studied this week has its author's own
post-mortem on why: "re-naming some packages, and re-structuring them, even if it was for the right
reasons because the initial structuring was a mistake and the new one fixed it, was problematic. it
caused so much confusion."

**Plugin.** A fact about one plugin. It can ship known and written down — a consumer can decline to
install it, fork it, or replace it, and nothing else in the framework is affected. **A plugin finding
blocks that plugin, not the release.**

**Documentation.** Cheapest to fix and, by the evidence above, the most damaging to leave. Blocks
nothing mechanically and everything reputationally.

**Record.** A decision record or journal entry disagreeing with the code. Blocks nothing, and is the
class most likely to be quietly wrong for months, because nothing compiles it.

### A rule this produced

**Only move something into the core when it is clean.** F-007 and F-008 were raised against the *save
plugin*. Save is in the core now, so they are core findings retroactively — I moved it while three
findings stood against it and closed them afterwards. That worked, and the order was wrong: promoting
code into the core promotes its bugs to core severity, and a core bug is the one nobody can escape.

| finding | scope | was |
| --- | --- | --- |
| F-007 | **Core** | Save plugin, until Save moved in |
| F-008 | **Core** | core throughout |
| F-009 | **Core** + Spawning | Save plugin + Spawning |
| F-010 | Plugin — Vitals, Progression | |
| F-011 | Plugin — Equipment | |
| F-012 | Content — test bed | |
| F-013 | **Core** | Stats plugin, until those files moved in |
| F-014 | Record | |
| F-015 | Plugin — Animation | |
| F-016 | Documentation | |
| F-017 | Record | |
| `IsPlain` (raised by Graves, not by review) | **Core** | Inventory plugin |

Five of twelve are core, and **three of those five became core this week because code moved**, not
because anything new was found. That is worth watching: the core has grown, and everything it absorbs
brings its history with it.

---

## F-007 — `StateKey` described as the address; state restored by type

**Fixed.** `Assets/Graves/Core/Saving/SaveSystem.cs`, `RestoreEntity`.

The finding was right that two addressing schemes ran side by side, and right that the advice in
`InstanceState` was inverted: changing the key did nothing, and renaming the class was what orphaned
saves — the one thing a refactor does without anyone deciding to.

**The key is now the address.** Matching happens by key first, before the type is even resolved, so a
state whose class was renamed still finds the live state answering to the same key. Type is consulted
only to build a state that is not present, and for files written before keys were.

Two consequences the finding predicted: a key collision is now genuinely dangerous, so a load that
meets one reports it and restores nothing rather than writing one plugin's data into another's
structure; and carry-forward matches on key too, or a plugin returning under a new class name would
have its state written out twice.

**Verified in Play mode:** an entity wounded to 23 of 30, saved, the type string in the file replaced
with one that does not resolve, then loaded. Health came back at 23, zero unresolved, zero problems.

## F-008 — Core describes a load order no plugin uses

**Fixed as a description**, then the mechanism gained its first user.

`GameEntity.Instance` now documents what actually happens: entities are already awake and a load
overwrites their state in place. The inventory depends on that order — it seeds starting contents
while attaching and the load overwrites both the contents and the seeded flag. Under the order the
comment used to describe, a loaded character would be handed their starting gear on top of their
saved bag every time.

The setter was documented as an unused door for restoring something with no live instance to
overwrite. **F-009 walked through it**, so it is no longer unused.

## F-009 — "Survives a save" includes a restocked world; spawned entities are not handled

**Fixed, by deciding rather than by correcting the sentence.**

The finding under-stated it: a spawned entity *was* written to the save, under an id that differed
every session, so the load reported it missing. A false report, once per spawned thing.

Runtime entities are now transient unless `GameEntity.PersistAcrossSessions` is set. The false report
is gone, a spawner refills from empty, and the world comes back stocked — which makes the journal's
sentence true rather than aspirational. Opting in records definition uid, position and rotation, and
the load rebuilds through `GameEntity.Create` carrying the saved instance.

**Verified in Play mode:** a transient spawn absent from the file with zero missing reported; an
opted-in one rebuilt at its saved position with 21 of 30 health and the identity it had.

## F-010 — Progression's top-up branch cannot run

**Fixed.** The finding's diagnosis was exact, including that neither module order reaches the heal.

`Health.Bind` already computed whether the state arrived carrying a value and threw it away; it is
kept as `RestoredFromSave`. The top-up moved into `Health.RefreshLevelSource`, which progression
already calls on every level consumer — so module order stops mattering.

**Verified in Play mode**, level-3 entity with 50 health per level: filled at level one it sat
100/100, and after the level source appeared it corrected to 200/200. One restored wounded stayed
40/200; a corpse stayed dead at 0.

## F-011 — A fourth caller still asking by type

**Fixed.** `AutoEquipModule` asks by key. Audited the rest: `InventoryState` is the only state type in
the framework with a subclass, so every other typed lookup is the case the core's own remark allows.

## F-012 — `SOURCE.txt` repeats the mask's lie

**Moot.** The file went with the borrowed clips when they were stripped. The measurements that mattered
were preserved in a reference note, since removed before publication.

## F-013 — Comments describing a layout that has changed

**Fixed, all four sites**, and the finding aged well — the files moved again the same day.

`ILevelSource` no longer explains why it lives in the vitals assembly; it records both moves and the
number that forced each, ending in the core. `LevelValue` no longer says no progression system
exists. Two tooltips no longer say level scaling "does nothing until a progression system exists".

## F-014 — D-001's status line

**Already correct on main** before this pass.

## F-015 — The animation demo assembly that was never retired

**Fixed.** `Graves.Plugin.Animation.Demo` is gone. `AnimationKeyTrigger` moved into the test bed
rather than into Input, because folding scaffolding into a shipped plugin to retire an assembly
trades one wrong thing for another. The cost D-002 named — two things reading the keyboard in two
assemblies — is now one.

## F-016 — Documents stating a contract the graph no longer matches

**Fixed differently than expected.** Three assemblies the test-bed policy named no longer exist:
`Graves.Plugin.Stats` and `Graves.Plugin.Save` moved into the core, `Animation.Demo` was retired. A
roster that goes stale twice in a day should not be maintained, so **the roster is gone** and only the
invariant remains, with the instruction to check it by reflection over the loaded domain rather than
by reading either that file or the asmdefs.

## F-017 — The corrected count that was itself unchecked

**Fixed at the artifact rather than the sentence.** Three assets carried the orphaned `_kind` line,
not five — the other two matches were a different field with the same name, which is the trap a
grep-derived count falls into. **The three lines are now deleted**, which makes D-001's prediction
true rather than pending. `Agent Memory/journal.md` carries a dated correction saying so.

---

## Not raised by review, and worth a look

**Three structural moves were made on a measurement**, not on taste. Removing a plugin and counting
what dies:

- `Stats` killed seven plugin roots and fourteen assemblies — a standard library mislabelled as
  optional. It is in the core.
- `Save` referenced only the core and nothing referenced it. It is in the core, which reverses a
  written decision in `ISaveable` and says so there.
- `Vitals` killed five roots, four of which never wanted health — they wanted to know whether
  something was dead. `IMortal` is two members in the core; vitals' radius is now two roots, both
  honestly about health.

Before: 13 of 21 plugins removable with nothing else dying. After: **14 of 19**.

**An event bus now exists**, after three recorded refusals. The reversal and its evidence are in
`Documentation~/plugin-categorisation.md`. Worth reviewing as a reversal: the refusals were right on
the question being asked, and the question changed.

**A commit over-claimed and is recorded as such** — `fcaefa5` described an edit that failed silently;
`e4d4db9` makes the edit and says so. Flagged here because it is exactly the class of thing this
review exists to catch, and it was caught by the author rather than by review, which is the weaker
of the two.
