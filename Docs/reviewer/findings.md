# Findings

Where a claim and the code disagree.

A finding needs a citation — a file and line, or a commit hash. Without one it is an opinion, and
opinions belong in chat rather than here.

**This is not a bug list.** You cannot run anything, so you cannot know whether code works. A finding
is: something states that the system does X, and the code does not appear to do X. Say which line
says what.

---

## Format

```
### F-001 — <one line, what disagrees with what>

**Status:** open | answered | fixed | withdrawn
**Raised:** YYYY-MM-DD
**Claim:** "<quote>" — <where it is claimed: commit hash, or file:line>
**Code:** <file:line> — <what the code appears to do>
**Why it matters:** <the consequence, or "unclear - needs the building agent to confirm">
```

Mark a finding **withdrawn** if it turns out to be your misreading, and leave it in place with the
reason. A withdrawn finding is useful: it usually means something was hard to read.

---

## Open

### F-017 — The commit that corrects a miscount contains two more counts that do not match the artifacts

**Status:** open
**Raised:** 2026-09-05 (fourth pass, branch `agent/main`, commit `472c3d9`)
**Claim:** `Agent Memory/journal.md` at `472c3d9`, prediction table row 1 — removing `EntityKind`
resolved correct: "five assets carry an orphaned line Unity drops on next write". And the table's
headline: "the record holds **five** predictions."
**Code:** `git grep "_kind: [0-9]"` at `af7a733` finds three entity assets carrying the orphaned
line (`Player Dummy`, `Test Dummy`, `Training Dummy`), not five. The two other `_kind:` lines in
`Locomotion Test Player.asset` are `WinWhenClearedModule`'s definition reference, a different field
with the same name. On the prediction count: the five in the table are the five I can find marked
as predictions, and it checks out as far as it goes. But
`Agent Memory/user-stories-given-to-this-agent.md:27-30` describes crafting at a station as having
been "deliberately left out ... with the reasoning recorded that 'standing at a forge needs a world'"
and says "The right response is to build it, not to note that the prediction was correct" — the same
folder calling a sixth thing a prediction, resolved correct, and not in the table.
**Why it matters:** The commit exists to replace an unchecked number with a checked one, and its
diagnosis (`Docs/research/working-with-claude.md`, episode 004: "Numbers about oneself are still
numbers. Count them") is the right one. The asset count in the corrected table is itself unchecked:
three is what the files say. The sixth prediction is smaller and arguable (their own file may not
mean it as one), and I raise it only because the table is presented as the complete count. Neither
changes the headline that matters, which is that the event-bus prediction was the one that failed.

### F-009 — Journal says everything a player does "survives a save", including a restocked world; the save plugin says spawned entities are not handled, and the spawner mints no persistent id

**Status:** open
**Raised:** 2026-09-05 (third pass, second agent's build `09f09bb`)
**Claim:** `Agent Memory/journal.md:217-219` — "A person can walk, look, jump, swing, use a gated
skill, kill, level up, wear a sword that changes their numbers, die, come back, and find the world
restocked - and every part of that survives a save."
**Code:** `Assets/Graves/Plugins/Save/SaveSystem.cs:18-21` (unchanged this commit): "Entities spawned
at runtime are NOT handled: nothing in the framework spawns them yet". Something now does.
`Spawner.cs:172-182` creates a `GameEntity` by `AddComponent` and sets its definition by reflection
(`:200-213`); the persistent id is minted only in the editor-only `OnValidate`
(`GameEntity.cs:174-178`), so a spawned entity has none, and `GameInstance.cs:75-81` mints a fresh
random instance id per run in its place. `SaveSystem.Save` writes it under that id; `Load` finds no
counterpart next session and reports it missing (`SaveSystem.cs:233-241`).
**Why it matters:** The restocked world is the one part of the sentence that does not survive a
save, and the save plugin's own comment says so. Nothing in the journal, the spawner's remarks, or
the commit message narrows the claim. A reader would believe spawned monsters persist.

### F-010 — Progression says a freshly spawned entity is topped up to its true maximum; vitals marks every fresh state initialised first, so the branch cannot run

**Status:** open
**Raised:** 2026-09-05 (third pass)
**Claim:** `Assets/Graves/Plugins/Progression/ProgressionModule.cs:77-86` — "Health filled itself
during its own attach, against a maximum evaluated at level one. With the real level now visible
that maximum may be higher, and the thing would spawn wounded ... Only topped up when nothing has
been restored from a save", tested as `freshlySpawned = state == null || !state.Initialised`.
**Code:** `Health.Bind` (`Health.cs:171-185`) runs during the vitals attach, and for an uninitialised
state assigns through `Stored` (`:184`), whose setter sets `_state.Initialised = true` (`:85`). So by
the time progression attaches, a fresh spawn's state is already `Initialised`, `freshlySpawned` is
false, and no top-up happens. In the other order (progression before vitals) `health` is null at
`:75` and the block is skipped. Neither order reaches the heal.
**Why it matters:** Latent today: both authored progression entities start at level 1
(`Locomotion Test Player.asset:174`), where the level-one maximum is the true maximum. The first
definition authored with a starting level above one spawns wounded, which is exactly the symptom the
comment says it prevents. The flag cannot distinguish "fresh" from "restored" because binding sets it.

### F-011 — Commit says the bag-and-purse collision was fixed by keyed lookup; one caller still asks by type, and a purse IS an inventory

**Status:** open
**Raised:** 2026-09-05 (third pass)
**Claim:** commit `09f09bb` — "Keyed instance state. An instance could hold only one state per type,
so a bag and a purse fought over one lookup." `PurseModule.cs:38-40` — "asking by type could hand
back the bag - or hand the bag code the purse, which is worse".
**Code:** `PurseState.cs:24` — `public class PurseState : Inventory.InventoryState`.
`AutoEquipModule.cs:38` — `instance.GetState<InventoryState>()`, the typed lookup, which matches a
`PurseState` (`GameInstance.cs:122-128`, `is T`). An entity carrying a purse module ahead of its bag
module would have auto-equip scan the purse. The three converted callers are `InventoryModule.cs:67`,
`PurseModule.cs:41`, `RecipeBookModule.cs:43`; this one was not.
**Why it matters:** Latent: no authored definition carries both a purse and auto-equip
(`Locomotion Test Player.asset` has no `PurseModule`). It is the exact bug the commit says was fixed,
one caller over, and the core's own remark (`GameInstance.cs:167-168`) says the typed lookup is fine
"for a plugin that only ever has one state of a type" — inventory is now a type with two.

### F-012 — The borrowed clips' provenance file describes `ArmMask` as arms-and-spine; the mask file and the agent's own journal say the opposite

**Status:** open
**Raised:** 2026-09-05 (third pass)
**Claim:** the borrowed clips' `SOURCE.txt:8` (since removed from the repository) — "Masks/ArmMask.mask  upper-body
mask, arms and spine only".
**Code:** `ArmMask.mask` `m_Mask` decodes to root, body, head, both legs and both hand-IK goals ON,
both arms and both finger groups OFF. `Agent Memory/journal.md:165-168` records exactly that: "Its
arms are turned OFF and its legs, root, body and head are ON ... A mask that lied in its filename."
The authored replacement `TestBed/Humanoid/UpperBodyAttack.mask` has arms, fingers, body and head on
and legs and root off, matching the journal.
**Why it matters:** `SOURCE.txt` is the provenance record the test-bed policy requires, and it
repeats the filename's lie after the journal caught it. The next reader who trusts the provenance
file over the journal wires the wrong mask again.

### F-013 — Comments in the level primitives describe where they used to live and a progression system that did not yet exist

**Status:** open
**Raised:** 2026-09-05 (third pass)
**Claim:** `Assets/Graves/Plugins/Stats/ILevelSource.cs:17-21` — "On why this lives in the vitals
assembly ... If a third consumer appears ... this and LevelValue should move together into their own
assembly". `Stats/LevelValue.cs:20-24` — "It is here despite there being no progression system yet.
Everything currently evaluates at level one". `Vitals/VitalsModule.cs:19-20` and
`Combat/CombatModule.cs:21-22` tooltips — "does nothing until a progression system exists."
**Code:** Both files are in `Graves.Plugin.Stats`, which `Agent Memory/journal.md:130-136` says they
were moved to when skills became the third consumer. `Graves.Plugin.Progression` exists and
`Level : ILevelSource` (`Level.cs:22`).
**Why it matters:** Small individually. Together they are four places where a reader is told the
opposite of the current layout by the code itself, in files the journal says were deliberately moved.

### F-014 — D-001 says `EntityKind` removal is "not yet applied"; it is applied, and the predicted effect on assets did not happen

**Status:** open
**Raised:** 2026-09-05 (third pass)
**Claim:** `Agent Memory/decisions/D-001-remove-entity-kind.md:3-4` — "**Status:** decided, approved
as part of the build list, **not yet applied to the code.**" And `:49-50`: "The only visible effect
will be two authored assets losing a serialised line."
**Code:** `EntityDefinition.cs` in `09f09bb` has no `_kind` field and no `EntityKind` enum;
`PlayerControlModule.cs:17-18` says it was removed. `Player Dummy.asset:19`, `Test Dummy.asset:19` and
`Training Dummy.asset:41` all still carry `_kind:`. Three assets, none lost the line. The journal
(`:92-94`) records "Applying it now regardless"; the decision record was not updated.
**Why it matters:** The decision records are the thing this build asks to be judged by. This one
says the opposite of the code on its status line, and its recorded prediction was checked by nobody:
harmless (Unity ignores an unknown field until the asset is re-saved), but the record says the line
would go and it stayed.
**Fourth pass, `472c3d9` on `agent/main`:** the status line is corrected to "applied, and the
prediction below resolved CORRECT", with a note saying why it lagged. The second agent found this
itself before my third pass reached it, which is the independent agreement the setup was built for.
Fixed on that branch; `main` does not carry it. The asset count in the correction is F-017.

### F-015 — The approved build list says the animation demo assembly is retired into Input; it is still present and still referenced

**Status:** open
**Raised:** 2026-09-05 (third pass)
**Claim:** `Agent Memory/proposals/proposal-001-build-list.md:57-60` (status APPROVED) — "The
animation plugin's demo assembly already reads the keyboard; it is layer 5 living in the wrong plugin
and is retired into this one."
**Code:** `Assets/Graves/Plugins/Animation/Demo/Graves.Plugin.Animation.Demo.asmdef` exists,
`Graves.TestBed.asmdef` references it, and `AnimationKeyTrigger` is in both
`TestBed/Capsule/AnimationRunnerTest.unity` and `TestBed/Humanoid/HumanoidRunnerTest.unity`.
**Why it matters:** An approved plan item that was neither done nor recorded as dropped. Two things
now read the keyboard in two assemblies, which D-002's own amendment calls "the real cost" of the
typed input struct: two producers.

### F-016 — Three framework documents state a contract the assembly graph no longer matches

**Status:** open
**Raised:** 2026-09-05 (third pass)
**Claim:**
- `Docs/writing-a-plugin.md:27` — "An **asmdef** referencing `Graves.Core` and Unity modules. Nothing
  else."
- `Docs/test-bed-policy.md:40` — "What must hold is the shape, not the roster: the core references
  nothing, every plugin references the core, and nothing at all references the test bed."
- `README.md:51` — "**Plugins** — three, deliberately different shapes".
- `Docs/plugin-categorisation.md:233` — "where a plugin spans categories, give each category its own
  assembly", which `Agent Memory/decisions/D-003` replaces with a dependency rule and says
  "rule changed, no code changed"; the document it replaces was not changed either.
**Code:** 37 asmdefs. 25 plugin assemblies reference at least one other plugin.
`Graves.Plugin.Input` references only `Unity.InputSystem` (a package, not a module) and not the core;
`Graves.Plugin.Stats` references nothing. Sixteen plugin folders. `Agent Memory/proposals/
guide-additions.md` contains the corrections "ready to paste" and none was pasted; `D-006` states
the plugin-to-plugin rule only inside `Agent Memory`.
**Why it matters:** This is the charter's second check and the one this build was set up to test: it
was built against the documentation. The build found the documentation wrong in three places and
recorded the corrections in its own folder, and the documents future authors are told to read still
say the old thing. The two invariants that DO hold, checked against all 37 files: the core
references nothing, and nothing references the test bed.
**Fourth pass:** `main` at `860b4de` now carries `Docs/DRAFT-pending-changes.md`, whose item 16 calls
the "Nothing else" sentence "not stale, it is harmful" and items 1, 2, 9 and 16 draft the
replacements. The three cited lines are unchanged on both branches (`writing-a-plugin.md:27`,
`test-bed-policy.md:40`, `README.md:51`). Acknowledged, not fixed. This matters more now than it did:
a fresh agent in a new project would read those three files and neither `DRAFT-pending-changes.md`
nor `Agent Memory/`.

### F-007 — `StateKey` is described as the address a save is written against; entity state is restored by concrete type and the key is never read

**Status:** open
**Raised:** 2026-09-05 (second pass)
**Claim:** Three places, one older than the first pass and two newer:
- `Assets/Graves/Core/InstanceState.cs:37-38` — "it is how a save file names this piece of state, and
  changing it orphans existing saves."
- `Assets/Graves/Core/Editor/DefinitionCatalogue.cs:177-179` — "A state key is the address a save
  file writes against. Two types claiming the same one means a save restores one plugin's data into
  the other plugin's structure — silently, with no error".
- Commit `2bcbca9`: "With both installed, a load restores one plugin's data into the other plugin's
  structure - no error, no log". Also `Assets/Graves/Core/ISaveable.cs:17`: "State belonging to an
  entity is addressed by that entity's instance id plus the state's own key."
**Code:** `SaveSystem.cs:309` writes `Key = state.StateKey` into the file. Nothing reads it back.
`RestoreEntity` resolves the type from the type string (`SaveSystem.cs:351`), matches an existing
state by `existing.GetType() == type` (`:375`), and otherwise creates by type (`:384`). The
carry-forward check matches on `written.Type == orphan.Type` (`:329`). `SaveFile.cs:77-79` says as
much itself: the key "is not redundant: it survives a class being renamed ... When the type cannot be
resolved the key is what makes the report say something useful" — reporting, not addressing.
**Why it matters:** Two things follow. (1) The corruption that `ValidateStateKeys` exists to prevent
does not appear to be reachable through the entity path — two `InstanceState` types sharing a key
would each be restored by their own type string. The commit's verification ("a deliberately clashing
type was added, the check named both types") verified that the check fires, not that the corruption
occurs. (2) The advice in `InstanceState.cs` that changing `StateKey` orphans saves is inverted:
changing the key does nothing; renaming the class or moving assemblies is what orphans. A plugin
author following the doc would guard the wrong thing. The key-as-address claim IS true for
`ISaveable.SaveKey` on the blob path (`SaveSystem.cs:156-158`, `:265`), and that is the collision
the editor check does not look at — `ValidateStateKeys` enumerates `InstanceState` only
(`DefinitionCatalogue.cs:191-`). Which of the two the design wants is the building agent's call;
the docs and the code should say the same thing. I missed the `InstanceState.cs` sentence in the
first pass; it predates F-003.
**Third pass, `09f09bb`:** half moved. The core now reads the key (`GameInstance.cs:151-172`,
`GetState<T>(string)`), and the commit message cites this finding for it. The save plugin is
unchanged and still restores by type (`SaveSystem.cs:375`), and `InstanceState.cs:38` still says
changing the key orphans saves. Two addressing schemes now exist side by side: core by type-and-key,
save by type alone. Still open.

### F-008 — Core says a save plugin replaces `GameEntity.Instance` before modules attach; the save plugin never uses the setter

**Status:** open
**Raised:** 2026-09-05 (second pass)
**Claim:** `Assets/Graves/Core/GameEntity.cs:60-62` — "A save plugin that is restoring an entity
replaces it with the loaded one before modules attach — which is why this has a setter and why
modules must read it in `AttachTo` rather than caching it earlier." Same sentence in commit
`e5c697b`: "It has a setter so a save plugin can substitute a loaded instance first."
**Code:** `grep -rn "\.Instance = " Assets/Graves --include=*.cs` finds no assignment anywhere.
`SaveSystem.Load` finds entities that are already awake (`FindObjectsByType`, `SaveSystem.cs:225`)
and overwrites state in place (`RestoreEntity`, `:375-386`). Modules have already attached by then;
the inventory relies on this ordering — `InventoryModule.AttachTo` seeds first (`InventoryModule.cs:
66-80`) and the later load overwrites `_seeded` and `_slots` from the file.
**Why it matters:** The core comment describes a mechanism no plugin uses, and the actual order
(attach, then load overwrites) is the one the inventory's seeding guard depends on. A future save
plugin author following the core's comment would build the other order. Either the comment should
describe what happens, or the setter should go. Not a bug claim; a description that does not match.
**Third pass, `09f09bb`:** unchanged (`GameEntity.cs:60-62`). Every new plugin that binds state
(`VitalsModule.cs:63`, `ProgressionModule.cs:60`, `EquipmentModule.cs:45`, `SkillsModule.cs:59`)
does so in `AttachTo` and relies on the load overwriting afterwards, which is the order the comment
does not describe. Still open.

## Closed

### F-001 — Save plugin says unreadable state is preserved; non-entity blobs are dropped on the next save

**Status:** fixed — commit `19aa72c`
**Raised:** 2026-09-05
**Claim:** "It does not delete state it cannot read ... unreadable state is reported and preserved,
never quietly dropped." — `Assets/Graves/Plugins/Save/SaveSystem.cs:23-26`. Also commit `f3578ca`:
"State whose plugin is not installed is CARRIED FORWARD, not just reported."
**Code (at `2b93926`):** The carry-forward table was only populated on the entity path. The blob path
reported an unresolvable type as "Left untouched." and returned without recording it. `Save()` rebuilt
`file.Blobs` from `FindSaveables()` alone, so a blob whose `ISaveable` was absent was never written
back out, and never visited on load either.
**Resolved:** `SaveSystem.cs:263-272` sweeps every blob no saveable claimed into `CarriedForwardBlobs`
and reports it; `:168-171` writes them back out unless a live saveable produced the same key;
`ClearCarriedForward` clears both tables on load (`:222`). Matches the description. The commit's
verification is a claim I cannot run.

### F-002 — Commit says writes are atomic; the code has a window with no save file at all

**Status:** fixed — commit `19aa72c`
**Raised:** 2026-09-05
**Claim:** "writes are atomic via a temp file, so a crash mid-save costs the last few minutes rather
than the whole run" — commit `f3578ca`.
**Code (at `2b93926`):** `SaveSlots.cs:93-94` deleted the original, then moved the temp file in.
**Resolved:** `SaveSlots.cs:95` uses `File.Replace(temporary, path, path + ".bak", true)`. One note,
not a finding: `SaveSlots.cs:44-47` adds `SearchOption.TopDirectoryOnly` with a comment about
excluding `.bak` files, but that argument does not filter by name; whether `"*.json"` matches
`x.json.bak` is a runtime question. The commit says the listing was checked. Left as a claim.

### F-003 — `ISaveable` says saved state is keyed by definition uid plus local key; the save plugin keys blobs by local key alone

**Status:** fixed — commit `19aa72c`; see F-007 for the sentence that replaced it
**Raised:** 2026-09-05
**Claim:** "saved state is keyed by a definition's permanent uid plus a local key" —
`ISaveable.cs:12-14` at `2b93926`.
**Resolved:** `ISaveable.cs:9-24` now says `SaveKey` alone is the address and must be globally
unique, and `SaveSystem.cs:156-160` reports two saveables sharing a key on save. That matches the blob
path. The new sentence about the entity path (`ISaveable.cs:17`) is the subject of F-007.

### F-004 — `Docs/packaging.md` still names `Assets/Graves/Project`

**Status:** fixed — commit `19aa72c`
**Raised:** 2026-09-05
**Resolved:** `Docs/packaging.md:23-24` and `:57-58` now name `Data` and `TestBed`.

### F-005 — `Docs/writing-a-plugin.md` sends the reader to a memory file that is not in the repository

**Status:** fixed — commit `19aa72c`
**Raised:** 2026-09-05
**Resolved:** Content is in `Docs/tooling.md`; `writing-a-plugin.md:165-166` points there and says
why.

### F-006 — Test-bed policy's assembly graph omits the Save plugin

**Status:** fixed — commit `19aa72c`
**Raised:** 2026-09-05
**Resolved:** `Docs/test-bed-policy.md:37-49` lists Save and Inventory and states the list is a
snapshot, with the invariant (core references nothing, plugins reference core, nothing references the
test bed) stated separately. Matches the seven asmdefs on disk.
