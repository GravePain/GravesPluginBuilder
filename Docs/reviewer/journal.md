# Journal

What happened, in order. **Append only.** Never edit or delete an earlier entry — if a later pass
proves an entry wrong, add a correction below it and say so. A visibly corrected record is worth more
than a tidy one.

Every entry: the date, the commit range covered, and what was actually checked.

---

## Format

```
## YYYY-MM-DD — <commits abc1234..def5678>

**Checked:** what you actually read and compared.
**Happened:** what changed in the project, in plain terms.
**Clean:** claims that were checked and held up. Name them - a review that only lists
problems tells the operator nothing about coverage.
**Raised:** new entries added to findings.md or questions.md, by id.
```

---

## Entries

<!-- First entry goes below. Nothing here yet. -->

## 2026-09-05 — baseline, commits d2de433..2b93926 (all 17)

**Checked:** The charter. Every commit message and `--stat` from the initial commit to HEAD. Every
file under `Docs/`. All seven `.asmdef` files. Every `.cs` file under `Assets/Graves` (Core, Core/Editor,
Plugins/Animation, Plugins/Animation/Demo, Plugins/Save, TestBed). `Assets/Graves/Core/package.json`,
`Packages/manifest.json`, the three authored `.asset` files under `Assets/Graves/Data`, the
one-line scene diff in `f3578ca`, and `git status`. I did not read scene YAML, prefabs, `.meta` files,
or the Starter Assets binaries beyond counting them. I did not read the untracked Inventory files as a
review — only enough to record what they are (Q-005).

Evidence rule I applied: only what is in the repository. I have a memory directory from the building
session that includes a project brief; I did not use it, because the next agent will not have it and
the charter says the artifacts are the test. Where that left a gap, it is in `questions.md`.

**Happened:** In one evening (2026-09-04 23:13 to 2026-09-05 01:31) the project went from a Unity URP
template to: a core assembly (definitions with a permanent uid, a registry, a module extension point,
runtime instances with per-object state, an `ISaveable` contract, a settings asset, a version stamp);
an editor assembly (one authoring window, catalogue operations, a diagnostic report); an animation
plugin (named clips played through a Playables graph over a live Mecanim state, with a mask path) and a
separate demo-input assembly; a save plugin (JSON slots, per-state concrete-type serialisation, carry-
forward of unresolvable entity state); a test bed with two scenes and 11MB of borrowed Starter Assets;
and six documents. The project brief the charter refers to is not among them (Q-001).

**Clean:** these claims were checked against the code and held.

- Core asmdef references nothing (`Graves.Core.asmdef` `"references": []`). No asmdef references
  `Graves.TestBed`. Every plugin asmdef references `Graves.Core` only, plus `Unity.InputSystem` for the
  Demo. Boundary check 5 in the charter passes at the asmdef level. What the loaded domain says I
  cannot know (Q-006).
- `CoreVersion.Version` and `package.json` `version` are both `0.1.0`, as `CoreVersion.cs:11` requires.
- Commit `e5c697b`: "GameEntity now creates its instance before the body and before any module" —
  `GameEntity.cs:81-83` does that order. "The resolved definition is cached in a [NonSerialized]
  field" — `GameInstance.cs:53-54`.
- Commit `f3578ca`: persistent id is editor-only and never re-minted — `GameEntity.cs:155-174`, guarded
  by `#if UNITY_EDITOR` and an early return when set. Duplicates reported, not repaired —
  `SaveSystem.cs:117-124` logs an error and skips both. Type names are namespace, class, assembly
  and never version-qualified — `TypeNames.cs:25-29`. Newer-format saves refused —
  `SaveSystem.cs:171-177`. Entity state carried forward — `SaveSystem.cs:296-313` and `264-278`. (The
  same claim does NOT hold for blobs: F-001.)
- Commit `a19b69f`: Validate reports an unresolvable module by name and says not to re-save —
  `DefinitionRegistry.cs:117-126`.
- Commit `9bcc20b`: mask base created at captured time and speed, not zero —
  `AnimationRunner.cs:289-291`, using `_capturedTime`/`_capturedSpeed` from `CaptureAnimatorClip`.
- Commit `26475a4`: "TestBed/ThirdParty exactly nine files" — nine non-meta files present.
- Commit `619ba790`: report uses `currentRenderPipeline` first — `CoreDiagnostics.cs:89`. "core
  depends on a plugin" is its own line — `CoreDiagnostics.cs:133-138`.
- `Docs/writing-a-plugin.md` "four things, only the first required": `DefinitionModule.cs:25` is a
  virtual no-op; the save plugin has no module and no attribute, as the doc says.
- Placeholder capsule strips components without naming `Collider` — `GameEntity.cs:141-147` keeps
  `Transform`, `MeshFilter`, `Renderer` by type test and destroys the rest.

Not checkable by me and recorded as claims only: every "verified in play mode" statement; the plugin
removal test in `writing-a-plugin.md:96-114`; the 0.03 → 1.33 capsule measurement; anything about
what a blend looks like.

**Raised:** F-001 through F-006 in `findings.md`. Q-001 through Q-006 in `questions.md`. Of the
findings, F-001 is the one I would put in front of Graves first: it is the charter's own example bug
on the adjacent code path, and the commit's verification did not reach it.

**Note on timing:** the working tree moved while this pass ran. At the final `git status`,
`InstanceState.cs`, `SaveFile.cs`, `SaveSystem.cs` and the solution file showed uncommitted
modifications, and an `ItemDefinition/` data folder had appeared alongside the Inventory files. Every
citation above is against HEAD `2b93926`, not the working tree. If those edits touch F-001 to F-003,
the next pass should check them against the findings rather than assume either way.

## 2026-09-05 — second pass, commits 2b93926..6755ba0 (10 commits)

**Checked:** The charter again, in full; `git log` shows it unchanged since `2b93926`. All ten
commit messages and the full diff of `Assets/Graves/Core` and `Assets/Graves/Plugins/Save`. Every
file in `Assets/Graves/Plugins/Inventory` in full. The two new item definition assets and the registry
diff. Diffs of `packaging.md`, `test-bed-policy.md`, `writing-a-plugin.md`, `README.md`; the new
`user-stories.md`, `tooling.md`, both extraction notes on the reference framework and `research/working-with-claude.md`
in full. Working tree is clean at HEAD. Also checked whether the building agent altered my three
files when it committed them in `88e0d5b`: the diff is exactly what I wrote, nothing added or
changed.

**Happened:** Six findings acted on in one commit (`19aa72c`). The inventory plugin landed
(`88e0d5b`), then had its slot model replaced (`e97320c`). A state-key clash check was added to the
window and the report (`2bcbca9`). The core lost an editor reference the report had exposed
(`eaeb4b0`). The README became an entry point. The user stories are now in the repository. Five
commits of documentation about a third-party addon ecosystem, the last four of which the building
agent itself records as a spiral that Graves stopped (`6755ba0`).

**Findings from the first pass:**
- F-001, F-002, F-004, F-005, F-006: fixed, and the fixes match their descriptions. Moved to Closed
  with the resolving lines cited.
- F-003: fixed for the blob path. The replacement sentence about the entity path is F-007.

**Clean:** claims checked this pass that held.
- `19aa72c` blob carry-forward: unclaimed blobs swept and reported (`SaveSystem.cs:263-272`),
  written back out (`:168-171`), both tables cleared per load (`:222`). Duplicate `SaveKey`
  reported on save (`:156-160`).
- `19aa72c` `File.Replace` with a `.bak` (`SaveSlots.cs:95`).
- `88e0d5b` correction applied consistently: the "JsonUtility cannot" sentence is gone from
  `InstanceState.cs`, `SaveFile.cs` and `SaveSystem.cs`, and the narrower reason (unresolvable types
  are dropped by the built-in path) is stated in each. The nested-state gap is recorded rather than
  hidden (`SaveFile.cs:69-72`).
- `88e0d5b` seeding guard: `_seeded` is a serialized field (`InventoryState.cs:35-36`) and
  `AttachTo` returns early on it (`InventoryModule.cs:66-67`). Starting items reference
  `ItemDefinition`, instances are minted at attach (`InventoryModule.cs:73`).
- `e97320c` slots keep positions: `Remove` clears in place (`InventorySlot.cs:99`), `EnsureSize`
  trims only trailing empty slots (`InventoryState.cs:359-364`), over-capacity is reported not
  repaired (`:303-307`). `MoveSlot` is merge, move, else swap (`:195-229`).
- `2bcbca9` `ValidateStateKeys` is wired into both the Validate button (`GravesWindow.cs:96-98`)
  and the report (`CoreDiagnostics.cs:164-168`). That it is wired is clean; what it guards is F-007.
- `eaeb4b0`: the `SetDirty` call is gone (`GameEntity.cs:174-178`).
- `4ff140f` README: its description of the three plugins matches what is on disk, including "atomic
  writes" now that F-002 is fixed.
- Boundaries: eight asmdefs now; `Graves.Plugin.Inventory` references `Graves.Core` only; core
  still references nothing; nothing references the test bed.
- Scope, now checkable: the five stories cover definitions, editor, no-presentation core, one-way
  plugins, everything-else-native. Instances, the persistent id, the save plugin and the inventory
  are plugins or plugin-enabling core by the test at `user-stories.md:177-185`. The diagnostic
  report, the packaging manifest and the state-key check trace to emergent stories E-4 and E-1, which
  are marked pending confirmation — the document says so itself, which is the right status.

**Not checkable by me:** every "verified" in the ten messages, including the blob round trip, the
backup file listing, the `references.RefIds` behaviour, the swap-by-identity test, and the loaded-
domain graph after `eaeb4b0`. Every count and quotation about the reference framework (477 files, 246 partial, 38
addon files on `Player`, 40 of 84 names) — the sources are outside the repository by policy, so a
reader here has to take them on trust; the documents do say which claims Graves corrected and which
were checked. Every quotation attributed to Graves in `user-stories.md` and
`research/working-with-claude.md`.

**Raised:** F-007 (state key is described as the address; entity restoration is by type and never
reads the key; the editor check guards a collision the code does not appear to have, while the real
key collision is on the blob path it does not check). F-008 (core comment says a save plugin swaps
`GameEntity.Instance` before modules attach; no code uses the setter, and the inventory depends on
the opposite order). Q-001 and Q-005 answered and moved. Q-002, Q-003, Q-004, Q-006 re-checked and
still open, each with a note on what changed. No new questions: the artifacts this pass were more
explicit about their own uncertainty than the last set, which is what the questions file is for.

**One observation for Graves, not a finding:** of the ten commits, five change code and five are
documents about a third-party product and about the agent. The building agent's own last commit
names this. The charter's third and fourth checks are about the framework, and I have nothing to cite
against those documents; I note only that `Docs/` grew by six files this pass and the code by one
plugin.

## 2026-09-05 — addendum to the second pass, no new commits

**Source statement from Graves, in chat, after reading the second-pass entry:** the quotations
attributed to him in `Docs/user-stories.md` and `Docs/research/working-with-claude.md` are accurate;
he said them. This resolves the "every quotation attributed to Graves" item under "Not checkable by
me" above, for the quotations as they stand at `6755ba0`.

What it does not resolve: the eight emergent stories E-1 to E-8 in `user-stories.md` are the building
agent's interpretations of those quotes and remain marked pending. Confirming the words is not
confirming the reading of them. Recorded here so the next reader does not have to ask about the
quotes again, and does not mistake this for confirmation of the stories.

## 2026-09-05 — Graves on the emergent stories E-1 to E-4, in chat, no new commits

Recorded from his statements as he went through `Docs/user-stories.md`. E-5 to E-8 not yet reviewed
by him at the time of writing. These are his decisions and they belong in `user-stories.md`; this
entry exists so they are not lost before the building agent moves them there.

- **E-1 (competing plugins for one category):** confirmed.
- **E-2 (a plugin may be more than one thing):** confirmed.
- **E-3 (installed by choice, not by default):** the requirement as stated is confirmed. The quoted
  words, though, were Graves reiterating the building agent's proposal, not his own origin. His own
  idea was to put it into a Unity package. He agreed with what is stated.
- **E-4 (consumable from a separate project):** NOT confirmed as written. The quoted words were an
  assumption he was stating, not a requirement. The requirement he would draw from it is that the
  agent must not pollute the core project with unnecessary test data, which is close to E-5. He is
  not satisfied with that phrasing and wants to restate it himself. I have deliberately not written a
  cleaner version for him; the interpretation stays pending until he gives the wording.
- **E-5 (the framework repository stays clean):** confirmed. Graves says E-5 is basically the same
  requirement as E-4, stated more clearly. So the requirement behind E-4 is already captured by E-5,
  and E-4 as written (consumable from a separate project) is the part that stays unconfirmed.
- **E-6 (windows that behave):** confirmed in substance, with Graves' own restatement, given in
  chat and transcribed as closely as I could: the user interface is compliant with modern user
  interface techniques, such as drag and drop and the memory of window positions. Still a future UI
  plugin requirement, not built. The quote already in `user-stories.md` stands; this is the
  requirement he draws from it.
- **E-7 (extract technique, never transplant code):** confirmed, with the intent stated by Graves
  in chat, recorded as closely as I could: it concerns how a third-party asset package is reviewed
  and interpreted. Code is not copied and blindly trusted from the package. Dependencies are not
  bought into this project, so it does not become dependent on a third-party asset. What is taken
  is the logic and the behaviour of the asset, not necessarily its code. The asset is used as an
  example, not as a copy-paste foundation to start something from. This matches the "Not a line
  goes to your clipboard" quote and the `_Reference` folder policy in `writing-a-plugin.md`.
- **E-8 (saves must survive the game changing under them):** confirmed, and Graves gave the story
  behind it in chat, recorded as closely as I could: it is a story about developing content for
  games. The content that gets developed is fluid, and that requires some guards within the
  framework. Recently a lot of time was spent chasing bugs where state had been serialized against
  an asset that was later changed to reflect current gameplay, but the serialized version was never
  updated. That caused chasing bugs that did not exist. The requirement is the guard, not the
  symptom: the framework must make a mismatch between serialized state and its changed source
  visible rather than let it present as a gameplay bug.

All eight now reviewed by Graves. Summary for the building agent: E-1, E-2, E-5, E-6, E-7, E-8
confirmed, some with his own wording above. E-3 confirmed with a correction about whose idea the
quote was. E-4 not confirmed as written; its requirement is E-5. These belong in `user-stories.md`,
which I cannot edit.

**Correction from Graves on the sentence above.** "The changes to user-stories.md are the building
agent's to make" simplified it too much. His rule, as he stated it: the agent has considerable
influence over the user stories while they are being created, but is not free to change them. The
user stories are what he said, and editing them would be changing his words without telling him. The
agent can make changes to `user-stories.md` only with the involvement of the project owner, Graves.
That is the prequalifying condition on the sentence above, and it applies to recording the E-series
decisions in this entry as well: the building agent records them with him, not for him.

**On the transcriptions.** Graves' statements reached me through speech-to-text with filler and some
garbled phrases, so E-6, E-7 and E-8 above are my readings, not verbatim. I read each back to him in
chat. He confirmed that what I read back was what he said into the microphone, so the readings above
are confirmed as his meaning. The exact spoken words are not in the record; the confirmed readings are.

Graves then corrected that last sentence: as far as he is concerned, what I wrote for E-6, E-7 and
E-8 IS exact. He has chosen not to correct the transcript, and treats the recorded readings as his
statement. Standing: the E-6, E-7 and E-8 entries above are his words, not approximations of them.

## 2026-09-05 — third pass, commit 6755ba0..09f09bb (one commit, a second building agent, branch agent-build)

**Checked:** The charter, unchanged since `2b93926`. The commit message. Every file under
`Agent Memory/` (README, journal, the first agent's retro, six decision records, three findings, the
extraction note, three proposals, the stories file). The full diff of the five core files and the
inventory plugin, `ManualPage.cs` in full. Every `.cs` file in the sixteen new plugin folders,
8,307 lines, read in full. All 37 asmdefs, parsed and graphed. `SOURCE.txt` and both new masks
decoded. The new data assets by module list. The three reviewer files, to see what the commit did to
them: it committed my uncommitted second-pass edits and the E-series addendum unaltered; the only
removed lines in the diff are the ones my own edits removed. I did not read the scene YAML, the
`.anim` files, or anything under `_Reference`.

Weighting, as the second agent's journal asks: its contact point is at `journal.md:62`. Everything I
cite from D-001, D-002, D-003 (original text), the stage-1 entry and F-001 is pre-contact. D-004's
amendment, D-005, D-006, F-003, stage 2 onward and the guide-additions are post-contact. I found no
place where I needed the distinction to decide a finding; it matters for Graves reading agreement
between the two agents, not for claims-versus-code.

**Happened:** One commit, 117,137 insertions, of which ~114,000 are three animation clips. The real
surface: sixteen new plugin folders in 29 new assemblies (37 total), a keyed state lookup and
`EntityKind` removal in the core, an authoring-window rebuild with tabs, module reordering and a
Manual tab that renders Markdown from `Docs` and `Agent Memory`, ten decision records, three
recorded findings from running, the Unity template folder deleted, and my three files committed.

**Clean:** claims checked against the code that held.
- Boundaries: `Graves.Core` references nothing; nothing references `Graves.TestBed`; no runtime
  folder contains `using UnityEditor`; every asmdef name matches its file. Checked over all 37.
- Commit: "EntityKind removed. Nothing read it" — `EntityDefinition.cs` diff, and my own grep from
  the first pass agreed nothing read it. "Keyed instance state ... Uses the key the save file already
  wrote and nothing had ever read" — `GameInstance.cs:151-172` reads it; matches F-007's account.
  "Authoring window: category tabs, module ordering with the reason order matters, owning assembly
  per module, a Manual tab" — `GravesWindow.cs` diff, `DefinitionCatalogue.MoveModule`, the
  per-module assembly label, `ManualPage.cs`. "Thirty-seven assemblies" — 37.
- Second agent's F-002 (state is the storage): `Health.cs:77-92` reads and writes through the bound
  state with a loose fallback; `Level.cs`, `Equipment.cs`, `Skills.cs`, `PurseModule`,
  `RecipeBookModule` all bind or read state directly. The fix described is the fix present.
- Second agent's F-003 (cached lookup): `ILevelConsumer` exists in Stats, `Health` and `Attacker`
  implement it, `ProgressionModule.cs:69-73` refreshes every consumer after installing a level.
- Journal claim the first agent found a case bug in the clip drawer and it was fixed:
  `ClipNameDrawer.cs:115-125` compares with `OrdinalIgnoreCase`, matching `AnimationModule.Find`.
- D-003: "`Graves.Plugin.Input` references nothing from the framework" — asmdef references only
  `Unity.InputSystem`. Stats "references nothing at all - not even the framework core" — true.
- Journal: "15 definitions across six kinds" — registry has 15 entries; six `DefinitionCategory`
  types (Entities, Items, Equipment Slots, Skills, Recipes, Crafting Stations), four from plugins.
- Journal: "no state key collisions" — seven `InstanceState` subclasses, seven distinct literal
  keys, all overridden as literals as `InstanceState.cs` asks.
- Journal, mask episode: `ArmMask.mask` decodes exactly as described, `UpperBodyAttack.mask` matches
  the authored replacement. (The provenance file disagrees: F-012.)
- Death moved out of AI into its own assembly, as the retro asked and the journal says accepted:
  `Graves.Plugin.Death` references Vitals and Locomotion; `Graves.Plugin.AI` references neither
  Death nor Combat.
- The three failures in the summary Graves relayed in chat ("the load that reported six entities
  restored and changed nothing, the mask I trusted by its filename, the health bar that was supposed
  to earn an event bus and didn't") are all in the record at equal weight: `F-002-state-is-the-
  storage.md`, `journal.md:165-172`, `journal.md:336-347`. The claim that failures were recorded is
  itself checkable, and it holds.

**Scope, against `Docs/user-stories.md` and the approved proposal:** every plugin traces to
proposal-001 as approved (stage 1 as corrected by Graves, stage 2, stage 3.1), to the direct story
G-1 (currency, crafting, stations), to D-005 answering a question Graves asked (enhancement), or to a
failure the journal records from Graves playing (death, respawn, spawning). By the test at
`user-stories.md:177-185` all are plugins, and none is core. I have no citable scope finding. What
I cannot verify is the brief and the approval themselves (Q-009). Scale is worth one sentence
without an adjective: sixteen plugins and 29 assemblies arrived in one commit against a document
that says the framework it reacts against "failed by having too many features rather than too few";
every one of them is a plugin, and the number of assemblies a reader must understand is now 37.

**Accumulation:** `Agent Memory/` as a second documentation root, now named by the core's editor
code (Q-007). The Unity template deleted without a record, the sample scene kept (Q-004). A first
agent's data asset edited without a record (Q-008). `Spawner.cs:200-213` reaches a private core field
by reflection; it says so and says it should not spread. `Graves.Plugin.UI` takes on the Input
System for its event module (`UiRoot.cs:106`); not recorded as a decision, and this project is set
to the new Input System only, so it is the only module that would work.

**Not checkable by me:** every "verified by running" in ten decision records and the journal; the
loaded-domain claim "the core still references `UnityEngine.CoreModule` only" (Q-006 stands, and this
is the third commit to assert it without the report's output in the repo); every quotation attributed
to Graves in `Agent Memory`; the root-motion measurements in `SOURCE.txt`; the first agent's retro as
a record of what it said. One arithmetic note, not a finding: `journal.md:9-10` lists ten stage-1
assemblies and says "Eight new assemblies, taking the project from seven to fifteen"; from one commit
I cannot tell which two arrived later.

**Raised:** F-009 to F-016. Of these, F-009 (spawned entities do not survive a save, against a
sentence saying everything does) and F-010 (the level top-up branch cannot run) are claims against
code that the build's own tests would not have reached: F-009 needs a save after a spawn, F-010
needs a starting level above one, and neither is in the authored data. F-011 is the commit's
headline fix with one caller missed. F-016 is the finding this build was set up to produce: it was
built against the documentation, found it wrong in three places, and wrote the corrections into a
folder the documentation does not point at. F-007 and F-008 re-checked, still open, notes added.
Q-007, Q-008, Q-009 raised; Q-004 narrowed.

**One note on the graph, for Graves rather than a finding.** The first agent's rule that a plugin
references "Graves.Core and Unity modules. Nothing else" is now false 25 times, and D-006 says that
is right and normal. I have no basis to say which is correct. I can say that the compiler enforces
one direction only, that the deepest chain is now five assemblies long
(`Skills.Combat -> Skills -> Stats`, `Skills.Combat -> Combat -> Vitals -> Stats`), and that the
document a plugin author is told to read still promises the shorter contract.

## 2026-09-05 — addendum to the third pass: a claim I did not check

Commit `09f09bb` says "Four predictions recorded before building, three of them wrong." I read that
sentence and did not count. A message Graves pasted into chat by mistake, and asked me to disregard,
happened to state that the count was wrong and had been corrected in a commit that is not in this
repository. I am not acting on that message and cannot verify it here. What I can record is my own
miss: a numeric claim in the commit under review, about the record's own honesty, and I let it
through because it read as self-critical. That is the same mechanism the charter warns about from the
other side. The next pass on this branch should count the predictions in `Agent Memory/` against
that sentence, whichever version of it is then in the log.

## 2026-09-05 — fourth pass, catch-up: main 6755ba0..860b4de and agent/main 09f09bb..af7a733

**Checked:** Four commits on two branches. On `main`: `6c6b565` and `860b4de` — their messages and
stats, the new `Docs/DRAFT-pending-changes.md` and research episode 004 in full, and the diff of my
three files (nothing removed; the additions are my own third pass, committed unaltered). On
`agent/main`: `472c3d9` in full, and `af7a733` in full — six new source files, two asmdefs, the
player asset change. The `_kind` residue and the prediction mentions counted across `Agent Memory`
at `af7a733`. `Test Potion.asset` compared across branches.

**Happened:** The branches diverged. `main` took documents: a 482-line holding list of sixteen
pending changes, most of them drawn from the second agent's notes and from my F-007 and F-008, and a
research episode about the second agent's miscounted commit message. `agent/main` took code: the
correction commit, and a lifecycle plugin in two assemblies giving the loop a won, lost or running
state with two authored endings. The player asset lost `RespawnModule` and gained the win and lose
modules, which is what the contradiction check in that commit requires.

**Clean:**
- `af7a733`: "the lifecycle assembly never learns what health or an enemy is" —
  `Graves.Plugin.Lifecycle.asmdef` references only the core; `Session.cs` names neither.
  `Lifecycle.Vitals` references Vitals and Death, as the contradiction check needs. "Ends once" —
  `Session.End` returns when not running. "Detects a contradiction ... respawn and end on death" —
  `LoseOnDeathModule.cs` reports a `Respawn` component. The two endings are asked, not told, as the
  remark says; `EndWhenNoneRemain` waits for at least one to have existed.
- `472c3d9`: D-001's status line now matches the code (F-014 note). The five predictions in its table
  are the five I can find marked as such. The event-bus prediction is the one that failed, on both
  branches' accounts.
- `860b4de`: the research episode quotes the second agent's journal accurately; the "five, one
  wrong" numbers match the correction commit.
- Boundaries at `af7a733`: 39 asmdefs; core references nothing; nothing references the test bed.

**Raised:** F-017 (the correction commit's own counts: three assets carry the orphan, not five; and a
sixth thing the same folder calls a prediction is not in the table). F-014 and F-016 updated: one
fixed on one branch, one acknowledged on the other and fixed on neither. Q-008 updated: both agents
carry the potion change, neither knows why. Q-010 raised: two diverged trees and no packaging shape,
against a plan to install the framework in a fresh project.

**Cross-reference for the first agent's holding list, since it asks:** its item 5 says "check how
their spawner creates entities before deciding" about the `GameEntity.Instance` setter. F-009 has the
answer: `Spawner.cs:172-182` adds the component and lets `Awake` build the instance; nothing hands a
loaded instance in; the setter is unused on both branches.

**Not checkable by me:** the lifecycle commit's "Verified: five grunts alive ... reports Won ...
reports Lost"; the correction's claim that the removal "compiled clean".
