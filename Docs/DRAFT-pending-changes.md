# DRAFT — pending changes

**Not committed. Not decided. No editor available, so nothing here has been verified by running it.**

A holding list, written 2026-09-05 after reading the second building agent's notes
(`K:\Protected\GravesPluginBuilder\Agent Memory`) and this project's own reviewer findings F-007 and
F-008.

Each item says what would change, what it rests on, and **what is still missing** — because the
reason this is a draft is that the data is incomplete, not that the writing is.

Delete this file once every item is resolved. It should not outlive the decisions.

---

## 1. The assembly-split rule is wrong and should be replaced

**Change:** `Docs/plugin-categorisation.md` currently says *"where a plugin spans categories, give
each category its own assembly."* Replace with:

> Split when a part carries a dependency the rest should not inherit, or when a part is useful
> installed on its own. Otherwise keep it in one.
> The test: **delete the other assembly.** If what remains is useful, or if merging would force a
> package on someone who does not want it, the split is real.

**Rests on:** the second agent's D-003, and it uses this repository's own evidence against it. The
animation demo was split because it needed the Input System — a *dependency* reason — and the
category agreed afterwards. The categorisation document says two arguments landing on the same
boundary is evidence the boundary is real, then promotes the **confirming** argument to a rule
instead of the **causal** one. Category is a judgement; dependency is a fact about the build.

**Confidence: high.** This is an argument, not a measurement, and it does not need an editor to be
right. It is the one item here that could be committed today.

**Still missing:** whether the second clause (standalone usefulness) survives contact with more
plugins. Their vitals/combat case is the only one it has been applied to, and they say plainly it was
added because a merge "felt wrong" and the reason had to be articulated after.

---

## 2. The guide understates the contract — an asmdef is required and nobody says so

**Change:** add the section the second agent drafted (`proposals/guide-section-assemblies.md`) to
`Docs/writing-a-plugin.md`, between *"What a plugin is"* and *"The one method"*.

**Rests on:** every plugin built by an outside agent on 2026-09-05 needed a correctly wired asmdef,
and the guide advertises the contract as one virtual method and a serialisable class. The failure
mode is the sharp part: **a missing reference reports as a missing TYPE**, so the author goes looking
in their own file for a class that is fine.

**Updated 2026-09-05, later:** the "missing reference reads as a missing type" claim is now
**observed independently** — they hit `The type or namespace name 'Vitals' does not exist` within an
hour of writing the warning. Two claims remain unverified by me: that Unity silently drops an unused
reference, and that a new assembly can compile cleanly while absent from the loaded domain. Both are
theirs, both are plausible, and both would be embarrassing to publish wrong in a document whose whole
point is accuracy. **Verify those two before pasting; the rest is earned.**

---

## 3. The scene-editing trap is narrower and worse than recorded

**Change:** `Docs/tooling.md` and the editor-traps memory currently say object references assigned
into a freshly created scene save as `fileID: 0`, and that reopening the scene fixes it. Sharpen to:

- **Creating or opening a scene and modifying objects in it within the same call does not stick.**
  The objects obtained before the scene settles are not the ones that get saved.
- **Reading the value back is also not proof.** Their first attempt re-read the property through the
  object it had just written and got the right answer — while reading memory about to be discarded.
  Only the file on disk settled it.

**Rests on:** their F-001, which reproduced it and then split the call to prove where the boundary
was. Independent of my hitting the same shape twice.

**Confidence: high on the rule, and it adds something my version does not carry** — my note implies
verification is possible in-memory. It is not.

**Still missing:** nothing, except my own reproduction. Safe to write when the editor is back.

---

## 4. F-007 — `StateKey` is documented as an address and is never read as one

**The problem:** three places say the state key is what a save is written against. `SaveSystem`
writes it and nothing reads it back — entity state is restored by concrete **type**. Two
consequences the reviewer names:

- The corruption `ValidateStateKeys` exists to prevent **may not be reachable on the entity path.**
  The commit verified the check fires, not that the thing it guards against can happen.
- My advice that changing `StateKey` orphans saves is **inverted**. Changing the key does nothing;
  renaming the class or moving assemblies is what breaks it. A plugin author would guard the wrong
  thing.

**The decision to take, and it is a real fork:**

- **(a)** Make the key the address — restore by key, fall back to type. Keeps the collision check
  meaningful and makes a class rename survivable, which is the more valuable property.
- **(b)** Demote the key to a reporting field, say so in every doc, and point `ValidateStateKeys` at
  `ISaveable.SaveKey` instead — where the key genuinely IS the address and where the check currently
  does not look.

**Leaning (a)**, because a plugin renaming its own class is ordinary and should not orphan a save, and
because the whole identity argument of this framework is that names and types are not identity.

**Still missing:** an editor, to confirm the entity path behaves as the reviewer read it, and to test
(a) does not break the carry-forward matching, which currently compares on type.

---

## 5. F-008 — a documented mechanism no plugin uses

**The problem:** `GameEntity.Instance` has a setter documented as *"a save plugin replaces it with the
loaded one before modules attach"*. Nothing anywhere assigns it. The real order is attach-then-
overwrite, and the inventory's seeding guard depends on that order.

**Change:** either correct the comment to describe what happens, or remove the setter. Not both.

**Leaning: correct the comment and keep the setter**, because a save plugin that spawns entities
rather than finding existing ones will want it, and that is the case a spawner plugin creates.

**RESOLVED 2026-09-05:** they built a spawner. Check how it creates entities before deciding — if it
instantiates and lets `Awake` build the instance, the setter is unused and should go. If it wants to
hand a loaded instance in, the comment is right and stays. **Needs one read of their spawner.**

---

## 6. `EntityKind` goes

**Change:** remove the field, the property and the enum from `EntityDefinition`.

**Rests on:** the second agent's C-1. Grep finds three hits, all inside `EntityDefinition.cs`. No
consumer anywhere. Its own tooltip claims the authoring window uses it to sort; the window never
reads it — a claim/code mismatch as much as a design question. Being the player is conferred by which
brain is attached.

**Still missing:** two authored assets carry a serialised value, which will produce a harmless
orphaned field. Needs an editor to confirm nothing else breaks. Their prediction is that it costs
nothing; that prediction is checkable and should be checked rather than assumed.

---

## 7. Modifiers are not events — and this may settle a question open since plugin one

**Not a change yet. The most valuable thing in their notes.**

The reference framework's `IHealthBonus` / `ICombatBonus`: health never hears of equipment. Equipment implements an
interface, the stat owner caches `GetComponents` once and sums every contribution **on read**.
Dependency inversion by *pull*, no messaging anywhere, shipped commercially for years with no
caching, no dirty flags, no invalidation.

Their conclusion, which is the part worth keeping: **modifier aggregation and change notification are
separate problems**, and conflating them is how a project ends up routing stat arithmetic through an
event bus.

If that holds, the answer to "are events earned yet" is: **never for modifiers, possibly for
notification.** Which is a narrower question than the one this repository has been carrying.

**RESOLVED 2026-09-05, and events lost again.** Their prediction — *"a health bar reacting to damage
is what finally earns an event bus"* — resolved **WRONG**. It needed neither a bus nor the events
health already raises.

**So events have now been predicted twice, by two independent agents, and failed both times.** I
predicted persistence would earn them; it did not. They predicted the UI would; it did not. Across 33
assemblies and a complete game loop.

And a mechanism explains both misses, visible only with the two side by side: **when the reader reads
through the thing that changes, there is nothing to notify.** State-as-storage killed the first case.
The reference framework's pull model — health summing `IHealthBonus` on read — killed the second. Two independent
designs, one reason.

That is stronger than "not yet earned". It is the start of an argument that a whole *class* of event
never gets earned, and that reaching for a bus is usually a sign the reader is holding a copy of
something it should be reading directly.

**RESOLVED 2026-09-05 by test 2, and it corrects the question rather than answering it.**

A quest counting kills is the occurrence case, and it was built without a bus. But it is **not**
built without events: `QuestKillTracker` subscribes to its own entity's `Attacker`, `Health` raises
`Died`, and `ExperienceReward` listens to it. Events have been in use since vitals.

**So the thing that keeps failing to be earned is the BUS, not the event.** Three predictions —
persistence, a health bar, a kill counter — and each was solved by a *local, typed, one-hop*
subscription between components whose relationship is known at compile time.

A bus buys **anonymous, many-to-many, cross-entity** delivery. Nothing has needed that, because every
listener so far was reachable by a reference chain: the attacker knows its owner, the victim knows its
killer, the quest log sits on the same entity as its tracker.

**The prediction that follows, recorded before it is tested:** a bus is earned when a listener cannot
be reached by any chain — a UI notification for *any* quest completing anywhere, achievements,
analytics, an audio cue for *any* death. Global and anonymous, not one-hop.

**And a principle worth taking from D-007**, which is better than the events question it settled:
*"Experience is a property of the victim. A quest is not."* Put the data where the fact lives.
Experience belongs on the goblin because what a goblin is worth is a fact about goblins. Being the
tenth goblin someone was asked to kill is a fact about the asker.

---

## 8. Five ScriptableObjects — a scale check worth recording

**Change:** add to `Docs/plugin-categorisation.md`, under the risk named first.

Only five types in all of the reference framework derive from `ScriptableObject`: item, skill, quest, recipe, item
mall. A framework sold commercially for years on the strength of its data-driven design has a backend
of five authored types.

That is the strongest available argument against building sixteen categories, and it is a measurement
rather than an opinion.

**Confidence: high.** Costs nothing and needs no editor.

---

---

## 9. The state object IS the storage — the contract has an unstated constraint

**The most important item on this list. Found by their F-002, on the first use of the save plugin by
a plugin its author did not write.**

**What happened:** a grunt hurt from 30 to 20, saved. Hurt to 10, loaded. The load reported
`loaded=True restored=6 missing=0 unresolved=0 problems=0` — and the grunt was still on 10. The state
object said 20, the live component said 10. **Both numbers were correct and they were different
numbers.**

**Why:** the save restores with `FromJsonOverwrite` onto the existing state object, in place. That is
deliberate and my own comment says why — so a plugin holding a reference does not end up writing to
an orphan. **It is also a design statement I never wrote down: the state object is the storage, and a
plugin is expected to read through it.**

Their vitals component kept its own integer and copied it into state on change. A write-through cache
with no invalidation. Saving worked; loading landed bytes in an object nobody read.

**Neither side could error.** The save plugin did exactly what it promised. The component's own reads
were internally consistent. My inventory happens to read through its state, so my two plugins could
never expose this — and the save plugin cannot detect that anyone has cached anything.

> *"The gap was only visible to a third plugin, written by someone who had read the documentation and
> not the save plugin's source."*

**Change:** paste their second guide section. It carries three things I did not know to say:

- Read and write through the state object; never cache its values in a field.
- **Bind after you configure, not before.** A saved value clamped against limits that have not been
  applied yet is clamped against defaults, producing a value that is wrong and plausible.
- **Distinguish "never set" from "set to zero".** A fresh instance and a corpse both hold zero health;
  without a flag, a load either revives the dead or kills the newborn.

**Confidence: high.** This is a documentation change describing behaviour that already exists, and it
was found by running, not reasoning.

**Also extends a rule of this project.** "A return value is not proof" now has a sharper case: **a
detailed success report, with counts, is also not proof.**

---

## 10. Their fix deleted an event-subscribing class — evidence on item 7

Fixing F-002 removed a component that existed purely to copy values between health and its state,
subscribing to two events to do it. With the state as the storage there is nothing to copy, nothing
to subscribe to, and no window in which the two can disagree.

**A load needs no notification, no callback and no polling — the bytes arrive in the object already
being read.**

So the running score on "are events earned": the naive design needed events, the correct design
needed none. Their prediction that stage 3.1 earns them still stands and is still the thing to wait
for. But this is one case where events would have been adopted to paper over a misunderstanding of
the contract.

---

---

## 11. The animation plugin has no documented position on root motion

**Found by an outsider needing it, same as items 2 and 9.** They are building skills against the
clip runner and were given borrowed attack animations plus a caution from Graves — *"choose it, don't let it
choose you"* — which names the failure precisely: root motion is not a feature you enable, it is a
default that quietly decides your movement architecture before you notice you made a choice.

**The undocumented fact:** the runner drives the Animator's output through a `PlayableGraph`, and
root motion is applied by the Animator from whatever drives it. **A clip played through the runner
will produce root motion if `applyRootMotion` is true.** The Playables path does not bypass it.

Nothing in `AnimationRunner`, `AnimationModule` or the plugin guide says so either way, and it is the
first question an ability system asks.

**Why it matters here specifically:** a character-controller motor moving the transform from intent,
plus a clip with root motion, is two things moving one transform. The previous project resolved that
with a bounded window — root motion enabled for the duration of one clip and disabled after — rather
than a global setting.

**Change:** document the behaviour on `AnimationRunner`. State what happens and leave the policy to
the game, the same way the core states the save contract and ships no format.

**Not doing:** adding a root-motion window to the runner. That is a design decision belonging to
whoever is fighting the motor, and they will reach a better answer cold than nudged.

**MEASURED 2026-09-05 by the second agent**, so this is no longer reasoning: every borrowed attack clip
carries root curves, and `1HAttack1` averages **0.80 m/s** — nearly two metres per swing, fighting the
motor for the same transform. They left root motion off, as a decision with a measurement behind it.

Graves' framing is the sentence to put in the guide: *"root motion is picked, never inherited."*

**Confidence: high, and the fact is now observed rather than inferred.**

---

---

## 12. The framework has no game lifecycle, and playing it is what showed that

**Found by Graves playing the build, not by any probe.** Two moments:

> *"death is an exit condition of the game play loop"* — and, after killing everything —
> *"i think i won by accident"*

**The loop has two exits and neither is owned by anything.** You die, or the world runs out. Respawn,
game over, reload and a victory screen are all answers to "the loop exited, now what", and none can be
built honestly while the loop has no outside.

That is **layer 1, game lifecycle** on the categorisation — the boot/menu/loading/gameplay/pause state
machine — and it is the layer this framework does not have. It was written off early as "a game's
opinion, therefore a plugin", which is probably still right. But nothing has ever been built against
that assumption, and the first real game hit the gap within a day.

**Not a change yet.** Recorded because it is the first structural gap found by playing rather than by
reading, and because it changes the minimum-loop claim: the loop is *the player acts, the world
responds, the state persists* — and it needs somewhere to go when it stops.

---

## 13. The restart policy resolved, and I had the axis wrong

I deferred a restart policy on the animation runner — restart, ignore-while-playing, or queue —
saying it would wait for a real case. Skills was the case. **The deferral was right and my three
options were the wrong question.**

Graves: **animation cancelling is a play style, not a correctness question.** A swing cut short by the
next one is how a fast game feels. So the runner still restarts whenever asked — playback does as it
is told — and whether to *ask* became an authored flag on a module the game owns.

And the real lever was neither: it is the cooldown set against the clip length, and those are now
equal.

**Change:** document that the runner always restarts, deliberately, and that gating belongs to the
caller. Currently it does neither — it just behaves this way and says nothing.

---

---

## 14. The detail panel draws fields a plugin is already drawing better

**Seen in a screenshot of the second agent's skills authoring, 2026-09-05.**

A `SkillDefinition` shows `Modules  [4]` as a collapsed array row in the properties block, and then a
proper Modules section underneath with per-module foldouts, the owning assembly named, reorder
buttons and an explanation of why order matters.

**Both edit the same field, and the top one is raw.** Expand it and you can add, remove and reorder
module references directly — bypassing the sub-asset creation the window normally does. A `Script`
row sits above it, which is standard Unity and pure clutter in a tool aimed at a human.

**This is core-side, not theirs.** The detail panel renders the definition's own inspector through
`InspectorElement`, which is what buys a plugin free editor UI — and it necessarily draws `_modules`
and `m_Script` as well.

**Change:** the detail panel should hide fields it renders separately, and hide the script row. Either
by drawing the visible properties explicitly rather than the whole inspector, or by hiding
`_modules` from the default drawer and keeping the custom section as the only view.

**It qualifies a claim in the guide.** "A plugin's fields render themselves for free" is true, and
incomplete: free, with no control over what *else* gets drawn alongside them. Worth stating, since a
plugin author cannot fix it from their side.

**Also worth taking from that screenshot, and it is a gap rather than a bug:** their window supports
module *ordering* and explains why it matters — *"a module that reads what another installs should sit
below it."* Mine has no ordering and no explanation, and `AttachTo` is called in author order, so
order is already load-bearing and undocumented.

---

---

## 15. A stack has one identity for many objects, and nothing bridges the two models

**Found by Graves' arrow riddle, answered by the second agent, verified in the code.**

> *"You enhance one arrow in a stack of twenty. What's in the slot now?"*

**An `InventorySlot` holds one `GameInstance` and a count.** Twenty arrows are one instance with
`Count = 20`. So the most natural line anyone would write to enhance an item —

```csharp
slot.Item.GetOrAddState<EnhancementState>().Level = 1;
```

— enhances **all twenty**, silently, and additionally makes the whole stack unable to merge with any
other arrows ever again, because `CanStack` refuses anything carrying state. One obvious line, two
invisible consequences, no error from either.

**`InventoryState` has no split operation.** Add, Remove, RemoveInstance, CountOf, IndexOfInstance,
FindInstance, MoveSlot, Validate — checked. Nothing takes one out of a stack and gives it its own
identity.

**The honest framing, which is the second agent's and better than mine.** The framework this project reacts against cannot
address one particular object *by design*: two items identical in every respect **are** the same item, so identity
and stacking are one mechanism rather than two, and enhancement is simply gated to categories that do
not stack. The question never arises.

This framework minted an instance id **independent of state**, so an object stays addressable through
being changed. That buys the enhanced arrow as a coherent idea and costs a split operation the other
model never needs.

**So the defect is not "a missing method".** Identity here is minted per *slot entry*, not per object.
Inside a stack this framework has that framework's model — many indistinguishable things under one identity
— and outside a stack it has its own. **Two models, with nothing bridging them.**

**Change:** a split on `InventoryState` that takes a quantity out of a stack, gives it a fresh
identity, and places it in a free slot — returning the new instance so the caller can act on the thing
it just separated. And `GetOrAddState` on an item in a stack of more than one is the landmine: it
should be hard to reach by accident.

**Recorded honestly:** I would have written the comparison up as an improvement over that framework. It is
a trade, and I would have missed that.

---

---

## 16. The guide forbids plugin-to-plugin dependencies, and it should not

**`Docs/writing-a-plugin.md`:** *"An asmdef referencing `Graves.Core` and Unity modules. **Nothing
else.**"*

That is not stale, it is harmful. It reads as a prohibition, so an author needing something another
plugin owns would either duplicate it or push it into the core — both worse than the dependency it
appears to forbid.

**The practice, at 33 assemblies:** plugins depend on plugins, one direction only, and **Unity refuses
to compile a cycle**, so the compiler enforces the direction without anyone checking. The judgement
that remains is not about direction, it is about surprise:

> **Would someone installing this plugin be surprised to find the other one required? Where the answer
> is yes, split.**

**The pattern that has now appeared twice: a bridge assembly, whose entire purpose is to join two
others.** Death was the first — it depends on vitals and locomotion so neither depends on the other.
The second joins equipment and progression, references five assemblies, and is the most coupled thing
in the project.

That is the right trade and the trade should be named: **the coupling did not disappear, it moved
into one optional, deletable place.** Delete the bridge and equipment still grants bonuses and
enhancement still records levels — the levels just stop being worth anything. A game wanting equipment
without an upgrade economy installs neither dependency.

**An observation worth carrying:** the bridge appears to own no state. That is what makes it safely
deletable, because there is nothing to strand. A bridge that *did* own state would orphan it on
removal — and the save plugin's carry-forward covers exactly that case, which is a connection worth
making explicit in the guide rather than leaving to be rediscovered.

**And the framing to lead with, which is Graves' and inverts mine.** I had been treating a dependency
as a cost needing justification. It is the opposite:

> **Dependencies are what keep a plugin concise to its function.**

A plugin that may not reference what it needs has to reimplement it, and is now two things. Forbidding
dependencies does not produce clean plugins; it produces bloated ones, or it pushes the shared part
into the core — which is the outcome this whole design exists to prevent.

That also reframes the bridge. It is not a workaround for an awkward dependency. It exists so that
**neither** side has to grow a bulge accommodating the other: equipment stays equipment, progression
stays progression, and a third optional thing owns the join.

**Change:** replace "Nothing else" with — in this order — dependencies keep plugins concise, one
direction only and the compiler enforces it, the surprise test for when to split, and the bridge as
the worked example.

---

---

## 17. Packaging: it works, and seven things are wrong with it

**Test 3, 2026-09-05.** A fresh Unity project, the core added as a UPM path dependency, three plugin
folders copied in, nothing else. **The whole chain ran** — authoring, spawning, damage, save round
trip — and **not one line of the framework needed editing.**

Recorded first because a list of only problems is not an honest picture: both core assemblies crossed
the package boundary intact including the editor half; three plugin assemblies compiled against a
packaged core with **no changes of any kind**, their name-based asmdef references simply binding; the
settings asset, `Resources` folder and registry were built from nothing; and the save round trip
worked first time in a project with a different persistent data path, company and product name.

The seven findings, in the order I would fix them.

**17a — The Manual does not travel, and fails silently.** `ManualPage.FindPages()` returns **0** in a
fresh install. Both roots resolve to `<projectRoot>/Docs` and `<projectRoot>/Agent Memory`, correct
in the repository and meaningless anywhere else, and a missing root is skipped without a word. The
tab is empty with no message and no hint the documents exist somewhere.

Two separable fixes: the framework's own `Docs` should **ship inside the package** where a packaged
manual can find them; and an empty Manual should say why it is empty. A game's own notes stay
project-relative, which is right.

**17b — The package ships no documentation of any kind.** No `README.md`, `CHANGELOG.md`,
`LICENSE.md` or `Documentation~`, and `documentationUrl` is NONE. Package Manager shows an empty
details pane, while the repository contains a `Docs` folder written specifically to fill it.

**17c — The core has no supported way to create an entity from a definition.** DONE. Every path
went through a serialised `_definition` field only an editor or a prefab could fill. The spawning
plugin got past it with reflection and said so.

**Graves stopped the first proposal, and was right to.** I argued the core must grow an API because
"the core is pure code" — a rule I claimed nobody had written down. It is written down, in
`CoreSettings.cs:11`, and it forbids shipping *content*: no model, no prefab, no clip, because
bundling any would be an opinion about what a game looks like. It does not forbid the core having a
presence in a scene, and `DefaultRepresentation` — a prefab slot, two fields above — had already
established the pattern. His question was why an agent must *form* the entity at all, and the honest
answer is that it need not: **instantiating something that is not yet awake removes the problem
rather than working around it.**

**What was built.** `CoreSettings.EntityPrefab`, a slot the project fills with a prefab whose root is
saved inactive; `GameEntity.Definition` gained a setter that refuses once the entity has woken; and
`GameEntity.Create(definition, position, rotation, parent)` instantiates the prefab — or a bare
inactive object when the slot is empty — assigns the definition, and switches it on. The prefab is
what actually solves the ordering trap; `Create` is where the fallbacks and the diagnostics live.

`Create` clears any persistent id inherited from the prefab, because every entity instantiated from
one would otherwise claim to be the same entity, and the core treats a shared id as a fault.

Verified in Play mode: instance minted, representation spawned, two spawns get different instance
ids, the setter refuses after waking, and `Create(null)` returns null having said why. The spawner's
reflection and its warning comment are gone. **This does not close F-009** — a runtime entity still
has no authored identity for a save to match next session, which is a save-plugin question.

**17d — The package lives inside another project's `Assets` folder.** DECLINED for now; see the note
after this item. The build script assembles a real package outside the repository and an install of
it was verified in a project created from nothing, which gets the benefit without the repository
move. The move itself stays worth doing the day this is published.

 `resolvedPath` points into the
source repository's `Assets/Graves/Core`. It resolves and compiles, but: two copies share `.meta`
GUIDs because they are the same files; editing from the consumer's Packages view edits the source
repo; and `file:` has no version to pin.

A repository layout change, not a code change — the publishable core wants to live at
`Packages/com.graves.core/`, or in its own repository. Their line is the one that lands: **the source
repository is currently the only project in the world that does not consume this framework the way
everyone else must.**

**17e — `DataRoot` is a compile-time constant inside the package.** DONE. It is now a field on
`CoreSettings`, falling back to `Assets/Graves/Data` when no settings asset exists yet - which is the
state the authoring window is in the first time it opens a fresh project.

Worth naming what it was: a package telling the project that installed it where the project's own
content must live, with forking as the only way to disagree. That is the failure this whole framework
was built in response to, reproduced inside it, in one `const`.

**17f — The runtime cannot be smoke-tested from `-executeMethod`, and nothing says so.** DECLINED as
stated, and half of it was wrong. A batch run via `-executeMethod` verified a package install this
session: documentation roots, module discovery by reflection, and the core-references-no-plugin
invariant. What it cannot do is wake an entity, because `Awake` does not run in edit mode - which is
a fact about Unity rather than a defect here, and it is now recorded in the tooling notes.

The half worth keeping was the second paragraph, which is not about `-executeMethod` at all:

Not a defect — the core is right to keep its runtime out of the editor. But **a play mode test
assembly must not be Editor-only**, or the run reports `testcasecount="0"` and `result="Passed"`,
which is the worst available way to be told nothing ran. Worth writing into the tooling doc.

**17g — Plugins are not packages.** DECLINED. It is correct and it is a problem for an audience that
does not exist. This framework is used by one person on one game; the first time two projects want
different versions of the same plugin is the first time it would be worth an afternoon, and that day
may never come.

Recorded rather than done, because the reason is the interesting part: two frameworks were studied
this session that died with their packaging immaculate and no game finished. Work that serves a
hypothetical installer is the most expensive kind, because it never argues back.

---

## 18. Their prediction that failed, and why it does NOT close F-009

They predicted a `GameEntity` added by `AddComponent` from script would get no persistent id, because
the id is minted in `OnValidate`. **Measured, and wrong** — Unity calls `OnValidate` on `AddComponent`
in the editor, the id is minted, and it survives onto a prefab. Their workaround was deleted and the
mistake recorded rather than quietly fixed.

**Do not let that close F-009.** Their measurement is editor-time `AddComponent`. F-009 is about
entities spawned in **play mode**, where `OnValidate` is not called at all. Both statements can be
true, and the reviewer's finding stands until measured on its own terms.

Worth noting as a pattern rather than a criticism: this is the second correction in a day where a
claim survived because it *sounded like a defect*. Self-critical claims are not self-verifying — see
`Docs/research/working-with-claude.md` 004.

---

## What this list is waiting on

| Item | Waiting on |
|---|---|
| 1, 8, 9 | Nothing. Could be committed today |
| 2 | Two Unity claims of theirs I have not verified |
| 3 | An editor |
| 4 | An editor, plus a decision between two real options |
| 5 | RESOLVED — a spawner exists. Read how it creates entities, then decide |
| 17a-g | An editor, and a decision on repository layout for 17d |
| 18 | A play-mode measurement, not an editor-time one |
| 6 | An editor. Decided in their D-001, deliberately not applied because the core is shared |
| 11 | An editor, to confirm the root-motion behaviour before documenting it |
| 7, 10 | Their stage 3.1 outcome — the prediction they recorded before building |

**Item 9 is the one that justifies the whole parallel build.** A constraint on the plugin contract
existed, was load-bearing, was invisible from inside, and surfaced the moment somebody built against
the documentation instead of the source.

**Four of ten are waiting on outcome data from the parallel build.** That is the parallel build
doing exactly what it was set up to do, and the correct response to most of this list is to leave it
alone and let it run.
