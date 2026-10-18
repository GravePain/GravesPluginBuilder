# Questions

What could not be determined from the artifacts alone.

**Your confusion is the instrument.** You are reading exactly what every future agent will read,
including the building agent after its own context is lost. If you had to infer something, so will
they — and that is a documentation defect worth fixing while the person who knows the answer is still
here.

Do not work it out and move on. Write it down.

---

## Format

```
### Q-001 — <the question, in one line>

**Raised:** YYYY-MM-DD
**Looked in:** <the files you checked before giving up>
**Why it matters:** <what you could not do, or might get wrong, without knowing>
**Answer:** <filled in later, with where it was recorded so the next reader does not ask again>
```

An answered question should end by pointing at where the answer now lives in the repository. If the
answer only ever exists in this file, it will be lost — the point is to get it into the documentation.

---

## Open

### Q-002 — Is `EntityDefinition.Kind` staying or going, and who decides?

**Raised:** 2026-09-05
**Looked in:** `Assets/Graves/Core/EntityDefinition.cs:18-29, 35-47`;
`Docs/plugin-categorisation.md:143-144` ("I have flagged `Kind` as removable twice; this is the
strongest argument yet for removing it"); commit `c28653e`.
**Why it matters:** The doc records the building agent's argument for removal three times and no
decision either way. Nothing in the repository reads `Kind` — `grep` over `Assets/Graves` finds no
consumer outside its own file. A future agent will either delete it on the strength of the doc or
build on it on the strength of the code, and both are defensible from the artifacts.
**Second pass, 2026-09-05:** still open. Nothing in `2b93926..6755ba0` touches it. One new input:
`Docs/user-stories.md:22-23` has story 1 name "my player, the NPCs, the enemies" as the things to
define, which is the same three-way split `EntityKind` encodes. That is an argument in the other
direction from `plugin-categorisation.md`, and neither document mentions the other.
**Answer:**

### Q-003 — Which entries in `Packages/manifest.json` are tooling for this repo, and which would a game project inherit?

**Raised:** 2026-09-05
**Looked in:** `Packages/manifest.json`, `Docs/packaging.md`, `Docs/test-bed-policy.md`,
`Assets/Graves/Core/package.json` (declares no dependencies). Commits `8cc49ab` ("Tools Installed
and Setup", no body) and `1d105b2` (`com.unity.pipeline`).
**Why it matters:** The manifest carries `com.besty.unity-skills` (a git URL), `com.unity.ai.assistant`,
`com.unity.visualscripting`, `com.unity.multiplayer.center`, `com.unity.timeline`, and
`com.unity.inputsystem`. The Demo asmdef needs Input System; the rest have no recorded reason. The
charter's fourth check is "accumulation nobody decided"; `8cc49ab` is a commit with no message body
that added packages. I cannot tell decided from default.
**Second pass, 2026-09-05:** still open. `Docs/tooling.md:27-28` now records why `com.unity.pipeline`
is there and that it is per project. The others remain unexplained.
**Answer:**

### Q-004 — Is the Unity template residue (`Assets/TutorialInfo`, `Assets/Readme.asset`, `Assets/Scenes/SampleScene.unity`) intended to stay?

**Raised:** 2026-09-05
**Looked in:** `git log --stat` for `d2de433` (initial commit), `Docs/test-bed-policy.md`.
**Why it matters:** `TutorialInfo/Editor/ReadmeEditor.cs` compiles into `Assembly-CSharp-Editor` and
the test-bed policy says every scene "earns its place by covering something no other scene covers".
`SampleScene` covers nothing recorded. Not a finding because nothing claims otherwise; a question
because a future agent cannot tell whether deleting these is tidying or vandalism.
**Second pass, 2026-09-05:** still open, unchanged.
**Third pass, `09f09bb`:** half answered by action. `Assets/TutorialInfo` and `Assets/Readme.asset`
are deleted in this commit; `Assets/Scenes/SampleScene.unity` is not. Nothing in the commit message,
`Agent Memory/journal.md` or any decision record mentions the deletion, so a reader cannot tell
whether the scene was kept on purpose. The question narrows to the scene.
**Answer:**

### Q-007 — Is `Agent Memory/` a permanent part of the repository, and who owns the two user-story files?

**Raised:** 2026-09-05 (third pass)
**Looked in:** `Agent Memory/README.md:1-5` ("Everything this agent writes lives here, so a human
can collect it in one grab"); `Assets/Graves/Core/Editor/ManualPage.cs:45` — the core's editor
assembly hard-codes `{ "Docs", "Agent Memory" }` as the two roots the Manual tab renders;
`Agent Memory/user-stories-given-to-this-agent.md` (story G-1, "the game has gold and you craft at a
station"); `Docs/user-stories.md`, unchanged this commit.
**Why it matters:** Three things pull different ways. The folder's README describes a working area
to be collected; the core now names it by path, which makes it framework-adjacent; and a user story
Graves gave directly sits in it rather than in the file the README calls "the foundation. Every
scope decision is checked against this document." Graves' rule (journal, second pass) is that
`user-stories.md` changes only with his involvement, which explains why the second agent kept its
file separate, and leaves two story files with no statement of which one a scope check reads.
**Answer:**

### Q-008 — Why did `Test Potion` lose its `StackableModule`?

**Raised:** 2026-09-05 (third pass)
**Looked in:** the diff of `Assets/Graves/Data/Definitions/ItemDefinition/Test Potion.asset` in
`09f09bb` (the sub-asset and the `_modules` entry removed, `_modules: []` left); commit message;
`Agent Memory/journal.md`; every decision record. No mention.
**Why it matters:** It is the first agent's authored data, changed by the second agent, with no
record. Harmless if deliberate (the potion is not used by any recipe or starting list I can find),
and the kind of silent edit to another author's content that the charter's fourth check exists for.
**Fourth pass:** the first agent committed the same change on `main` in `6c6b565` ("a Test Potion
test fixture lost its stackable sub-asset at some point after e97320c. Test-bed data, noted rather
than chased"), and the second agent's `860b4de` commit message names this question as "aimed
squarely at me". The file is byte-identical on both branches. So both agents carry the change and
neither knows its origin. Still open; the likeliest reader-with-no-context explanation is a Unity
re-save while the module type was unresolvable, which is the exact loss `DefinitionRegistry.Validate`
warns about, and if so it is the first time that warning's scenario has happened for real.
**Answer:**

### Q-010 — Which branch does a new project consume, and how?

**Raised:** 2026-09-05 (fourth pass)
**Looked in:** `git log --graph --all`: `main` (`860b4de`, the first agent's documents and my files)
and `agent/main` (`af7a733`, the second agent's 37 assemblies plus lifecycle) diverge at `6755ba0`
and are not merged; `git diff --stat` between them is 376 files. `Docs/packaging.md` ("Not yet
implemented, deliberately"); `Assets/Graves/Core/package.json` declares no `samples`;
`Docs/user-stories.md` E-4 ("consumable from a separate project", still pending).
**Why it matters:** Graves intends to install the framework in a fresh project with a fresh agent
and have plugins built from assets. That agent will build against whichever tree it is pointed at,
and the two trees disagree about what a plugin is (F-016), how many exist, and whether the core has a
keyed state lookup. Neither tree has the packaging shape `packaging.md` describes, so "install" today
means copying or path-referencing a folder. Which one, and whether the branches merge first, decides
what the fresh agent's documentation says.
**Answer:**

### Q-009 — Is the second agent's brief ("a single-player game with MMORPG-shaped systems") Graves' words, and where should it live?

**Raised:** 2026-09-05 (third pass)
**Looked in:** `Agent Memory/README.md:10` ("Set up by Graves") and `:40-46` ("The target"); `Agent Memory/
proposals/proposal-001-build-list.md:12-13` and the APPROVED block at `:147-151`, which quotes Graves;
`Docs/user-stories.md`, which has no such target statement.
**Why it matters:** Every scope judgement I can make on this build rests on that brief and on the
approval quote, and both are recorded only by the agent whose scope they authorise. The same class
as the emergent stories E-1 to E-8, which Graves reviewed in chat and confirmed; these have not been.
Until they are, "was this asked for" for sixteen plugins rests on the builder's transcript of the
asker.
**Answer:**

### Q-006 — What would settle the assembly-boundary claims that only the loaded domain can answer?

**Raised:** 2026-09-05
**Looked in:** all seven asmdefs; commit `32e23b5` ("Graves.Core -> netstandard,
UnityEngine.CoreModule"); `Docs/writing-a-plugin.md:126-135`.
**Why it matters:** The asmdefs say the right thing (see journal, Clean). But the docs are explicit
that asmdefs are not the truth and reflection is — and I cannot run reflection. The
`CoreDiagnostics` report (`Assets/Graves/Core/Editor/CoreDiagnostics.cs:104-141`) prints exactly this
graph. If its output were committed as a text file in `Docs/` on each core change, the reviewer and
every future agent could check the real graph without an editor. Not a demand; a suggestion for
where the evidence could live.
**Second pass, 2026-09-05:** still open, and commit `eaeb4b0` is the case for it: the loaded domain
showed `Graves.Core -> UnityEditor.CoreModule` while every asmdef and every doc said otherwise, and
only running the report found it. The report's output is still not in the repository, so the
corrected graph ("`Graves.Core -> UnityEngine.CoreModule`, and nothing else") is a commit-message
claim I cannot check.
**Answer:**

## Answered

### Q-001 — Where are the five user stories?

**Raised:** 2026-09-05
**Looked in:** every file under `Docs/`, `README.md`, all commit messages. The charter
(`Docs/reviewer/CHARTER.md:62`) says "The five user stories are in `Docs/` and in the project brief."
`grep -rni "user stor" Docs README.md` matches only that charter line. No file under `Docs/` is a
project brief.
**Why it matters:** Scope creep is the third check in the charter and I cannot run it. I cannot say
whether `ItemDefinition`, `EntityKind`, the diagnostic report, or the Inventory plugin now appearing
on disk were asked for. Neither can the next agent. Until the stories are in the repository, every
"was this asked for" judgement is made from memory or from nothing.
**Answer:** `Docs/user-stories.md`, commit `19aa72c`. The five stories are there verbatim, with
derived decisions kept separate and eight emergent stories marked pending. Answered 2026-09-05.

### Q-005 — An Inventory plugin is being written on disk with no commit and no recorded decision. Was it asked for?

**Raised:** 2026-09-05
**Looked in:** `git status` at two points during the first pass. `Assets/Graves/Plugins/Inventory/`
contained one untracked file at the first check and four at the second, all timestamped 01:34, three
minutes after HEAD. Nothing in `Docs/` named an inventory plugin as next.
**Why it matters:** Work in progress, not reviewed in the first pass. Recorded because it was the
first plugin arriving without a doc or commit saying why, and because it put `[SerializeReference]`
on a `GameInstance` inside an `InstanceState`, which `InstanceState.cs` at the time said `JsonUtility`
could not handle.
**Answer:** Committed as `88e0d5b`, with the serialisation concern answered in the same commit:
the "JsonUtility cannot" claim was wrong and is corrected in `InstanceState.cs`, `SaveFile.cs` and
`SaveSystem.cs`, and the remaining gap (nested state from an uninstalled plugin is not carried
forward) is recorded at `SaveFile.cs:69-72`. On "was it asked for": none of the five stories names an
inventory. `Docs/user-stories.md:177-185` gives the test — a plugin is in bounds if it is a decision a
particular game would make — and the inventory is a plugin. `README.md:5` says the framework is for
"entities, items, inventories", which is broader than the stories. Answered 2026-09-05.
