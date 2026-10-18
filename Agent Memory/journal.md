# Journal

Append only. A later entry corrects an earlier one; nothing is edited away.

---

## 2026-09-05 — stage 1, the loop closes

**Built:** input, locomotion in two assemblies, a follow camera, vitals, combat in three assemblies,
AI in two. Eight new assemblies, taking the project from seven to fifteen.

**Verified by running, not by reasoning:** a person can move, the camera follows and orbits, an
attack lands and takes health off, two grunts walk in from eleven metres, hit the player for five
each on a 1.2 second cooldown, and die in three swings, after which the body stops, unblocks the
path, sinks and is removed.

**Not verified:** that any of it survives a save. Health lives in an instance state and the save
plugin should carry it, and nobody has pressed the button. Stated because it is exactly the kind of
thing that reads as done.

### What running it found that reading it did not

**1. Two monsters killed each other on the way to the player.** Both spawned symmetrically, both
converged on the same spot, and a swing finds everything with health inside its arc. They were dead
before they landed a blow on anyone who mattered. The diagnostic output showed both grunts at zero
health with nothing having attacked them, which took a moment to believe.

Fixed with a damage filter rather than a faction system: an interface answering one question about
one victim, and a hunter that may only hurt things carrying the AI target marker. Crude - every
hunter is now harmless to every other hunter - and it defers factions honestly rather than
pretending they exist. When factions are built, that class is what they replace.

**2. A dead brain still reported it was chasing.** Death disables the brain component, and a disabled
component's update never runs to clear what it was pursuing. Harmless in itself, actively misleading
to anyone reading state, and on this project reading state is how most real bugs have been found.

**3. The guide gap demonstrated itself within the hour.** A compile error read
`The type or namespace name 'Vitals' does not exist` - which is a missing assembly reference wearing
the costume of a missing type, and is precisely what the drop-in guide section written earlier that
day warns about. Pleasing and slightly embarrassing.

### Two things left undone, named rather than hidden

**Player death does nothing.** The player reached zero health during testing and simply stood there.
The only death behaviour that exists is attached by the AI brain module, so it applies to monsters
and not to the person playing. A general death system is the honest fix and it is not built.

**Death handling sits in the AI plugin, which is the wrong home.** Death is not an AI concept. It is
there because the alternative was giving vitals a dependency on locomotion and physics in order to
stop a body and stop it blocking the path. Recorded as a compromise, not as a design.

### The claim this stage was built to test

Single player deleted the networking argument for splitting a brain from a motor, leaving one
justification: that a machine and a person could drive the same legs. **The chase brain produces the
same intent struct the player brain produces and hands it to the same motor, and needed nothing the
player brain did not.** If it had, the split would have been wrong. It did not, so the split holds -
on the narrower ground, which is the only ground left.

---

## 2026-09-05 — CONTACT POINT: retrospective feedback from the lead agent

Everything above this line was decided blind. Everything below was decided after reading
`Lead Agent Feedback mid build retro.md`, in which the first build agent said where it would have
landed on decisions already taken. It deliberately said nothing about work not yet done.

**A future reader should discount agreement below this line accordingly.** Convergence above it is
independent; convergence below it is not.

### What was accepted, and what changed as a result

**A correction to this agent's framing, accepted in full.** This agent said agreement from a
reviewer that cannot compile is the weakest evidence available. That conflated two things. The
reviewer is a *weak witness on behaviour* and a *strong witness on claims* — it found two live
data-loss bugs and four false claims by comparing documents against code, which needs no compiler.
The charter warns against agreement, not against the method.

**Four corrections accepted and acted on:**

1. D-003's second clause rests on a single case and is now marked provisional.
2. D-003 demotes the categorisation document from a rule-giver to a vocabulary, and this agent
   failed to say so. Now stated.
3. Death handling has a third home that this agent's own rule prescribes and did not apply to
   itself — its own assembly. Being built.
4. The damage filter's risk was mis-stated: it is a migration cost in content, not a class to
   replace.

**One correction accepted about the typed input struct:** the escape hatch is the real cost, because
a second source fragments the producer and forces every consumer to choose or reconcile.

**One decision defended rather than conceded:** `EntityKind` decided-and-not-applied is a declared
divergence with a stated reason, which is a different thing from the undeclared mismatch it was
criticised for. Applying it now regardless, since it is unblocked.

**One place this agent's call beat the lead agent's:** treating the camera as an instrument rather
than a system, on the grounds that locomotion is judged by eye and cannot be judged without one. The
lead agent would have deferred it. That distinction generalises past cameras.

---

## 2026-09-05 — stage 2: the backend the reference was chosen for

**Built:** progression, equipment in two assemblies, skills in three, and a small shared assembly
for the level primitives. Twenty-two Graves assemblies now.

**Verified by running:**

- Level one to level two on two kills, with experience carried over and the next level costing more.
- A sword equipped moves out of the inventory, raises maximum health by 25 and damage by 5, comes
  off again, and survives a save with the same instance id it had before.
- A skill refused while unarmed with the words "Requires something in the Main Hand", allowed once
  the sword is on, hitting for double the user's damage *including* the sword bonus, then refused
  again with "Ready in 2.4s".
- Known skills persist. The save holds a skill uid and a level and nothing else.

### The plugin contract took its real test

Four kinds of definition now exist and two were added by plugins - equipment slots and skills - each
appearing in the authoring window with no core change. That claim has been in the documentation
since before any of this was built and nothing had ever exercised it.

**Two of the three interfaces this framework leans on were proved by things written after them.**
Health and the attacker were written before equipment existed and never mention it; wearing armour
changes both. Nothing is told, nothing recalculates, and there is no cache to invalidate because the
component is always present and only its answer changes.

### The move that was predicted and then required

The level-scaled value struct and the two level interfaces lived in vitals because that is where the
first consumer was. `ILevelSource` said in writing that if a third consumer appeared wanting levels
without health, they should move to their own assembly.

Skills was that consumer. They moved. The new assembly references nothing at all - not even the
framework core - which is the second time an assembly here has turned out to need less than its
author assumed.

### What was refused, and why

**Skills carry no behaviour.** The reference's skill asset holds virtual methods for checking and
applying; ours holds modules that implement interfaces. The cost is one indirection. The gain was
visible immediately: the melee effect, the timing and the equipment requirement live in three
different assemblies and none of them knows the others exist.

**The strike is not duplicated.** A skill asks the attacker to sweep with different numbers rather
than doing its own overlap. There is one owner of what a sweep in front of an entity hits, and a
skill quietly bypassing the filter that stops monsters killing each other would have been a very
confusing bug.

**No global cooldown.** A shared floor across all skills is a real tool and it is invisible until it
blocks fast chaining, at which point it reads as dropped input. If one is wanted it should be an
explicit setting rather than an unstated default.

**No animation.** There is no attack clip in the project - the borrowed set is jump, locomotion and
idle. Wiring a walk cycle to a sword swing would look like progress and be a lie. The clip runner is
ready and the skill has nowhere to point.

---

## 2026-09-05 — the session where a person played it

Everything up to here was verified by measurement. This entry is different: Graves played it, and
four things came out of that which no probe would have found.

**1. A mask that lied in its filename.** The borrowed attack clips came with a mask called `ArmMask`. Its arms are turned OFF
and its legs, root, body and head are ON. Wired to a basic attack, it drove the legs and torso into
an uppercut while the arms carried on walking. Graves saw it as a pop; no measurement of weights,
blend times or graph structure would have shown anything wrong, because nothing was.

The skill guidance names this exactly - **names are not identity** - and it was read that morning and
still not applied to a filename. A correct upper-body mask was authored instead: torso, head, arms
and fingers on, legs off, root off.

**2. Death was indistinguishable from a bug.** A killed player has its motor and collider switched
off and simply stops answering. Reported as "I jump and Q and I am stuck in place", then, a minute
later, "oh I am dead". The state probe said `hp=0 dead=True` and everything else was working
perfectly.

The framework did precisely what it was told. **Nothing about the experience said so**, and the
earlier note calling player death "cosmetic" was wrong. Respawn was built in response - not as the
right answer, but as the cheapest legible one.

**3. The dead could still cast.** Found by a player pressing keys on a corpse and noticing the only
thing that still worked was the skill. The attacker had a dead check and skills had none, because
the rule was written in one place and not the other.

The fix is a condition on the *user*, not a requirement on each skill - otherwise every skill ever
written carries a module saying it does not work while dead, and the one that forgets is castable by
a corpse. The same seam now serves stunned, silenced and frozen.

**4. The loop has two exits, and both are unowned.** Graves: *"death is an exit condition of the
game play loop"*. Then, after killing everything: *"i think i won by accident"*.

That is the pair. You die, or the world runs out. Neither is anybody's job. Respawn, game over,
reload and a victory screen are all answers to "the loop exited, now what", and none of them can be
built honestly while the loop has no outside. **That belongs to game lifecycle - layer one on the
categorisation, and the layer this framework still does not have.**

The nearer half was buildable and was built: a spawner, so the world stops running out.

### Two things he decided that this agent had wrong

**Animation cancelling is a play style, not a correctness question.** A swing being cut short by the
next one is how a fast game feels and is not a defect. So the runner still restarts whenever asked -
playback doing as it is told - and whether to ask became an authored flag on a module a game owns.
The real lever turned out to be neither: it is the cooldown set against the clip length, and those
are now equal.

**Root motion is picked, never inherited.** *"one trap is root motion you pick to use it dont let it
be chance"*. Every borrowed attack clip carries root curves and the prefab had root motion off by
default. Off is now a decision with a measurement behind it: `1HAttack1` averages 0.80 m/s and would
have carried the character nearly two metres per swing while the motor fought it for the same
transform.

### Where it stands

29 assemblies. 10 definitions across four kinds, two of those kinds added by plugins. A person can
walk, look, jump, swing, use a gated skill, kill, level up, wear a sword that changes their numbers,
die, come back, and find the world restocked - and every part of that survives a save.

---

## 2026-09-05 — the lead agent reviewed a drawer and found a bug I could not see

**The bug, in his words and it is exact.** The clip-name drawer gathered its vocabulary into a
`SortedSet` with `OrdinalIgnoreCase`, then tested membership with `List.Contains`, which is
case-sensitive. `AnimationModule.Find` matches case-insensitively.

So a skill naming "attack" against a module authoring "Attack" **works perfectly at runtime and the
drawer flags it as unknown.** A warning that fires on something that works is worse than no warning,
because the next real one gets ignored.

**Why I could not have found it.** Every clip name in this project happens to be authored in the
same case as it is referenced. The bug is invisible until somebody types a name in a different case,
which is exactly the thing the drawer exists to protect against and exactly the thing I would never
do while testing my own drawer. He found it by reading the two comparers next to each other.

Fixed by matching the way the runner matches. The index lookup one line above had the same defect and
would have shown "(none)" while the asset held a working value - a display lie rather than a false
warning, and worse for being quieter.

Two other things he named, both now recorded in the file rather than fixed: the collapse of
case-variant names into one entry is deliberate rather than a side effect, and "(none)" as a magic
string is a real edge case that was seen and left.

### What he checked that I would not have thought to state

> "I checked the part that matters rather than trusting the description: the only write is inside
> RegisterValueChangedCallback, so drawing never mutates data."

That is the review this project's charter asks for, applied to a drawer: not "does the description
sound right" but "where does this write". A drawer that fixes values on inspection is the usual way
this goes wrong, and the check is one grep.

### A documented claim that turns out to be true and incomplete

The plugin guide promises that a plugin's fields render themselves for free. The lead agent's
qualification: **free, with no control over what else gets drawn next to them.** The core detail
panel renders the definition's whole inspector, which draws the raw modules array and the script row
whether anyone wants them or not - so the modules list appears twice, once as a bare array that
bypasses sub-asset creation and once as the proper section.

That is his to fix core-side and he has asked me not to build around it. Recorded because the guide
sentence should carry the qualification.

### Three documented claims now exercised

Two of them by me, one by breaking it:

1. **A plugin can add a definition kind with no core change.** Done twice - equipment slots and
   skills - and both appear in the authoring window as tabs.
2. **A plugin can add a property drawer and it will be picked up.** Done, editor-only assembly, never
   reaches a build.
3. **Instance state is the storage, not a copy of it.** Found by violating it and watching a load
   report six entities restored while changing nothing.

### Two things he says I have that he does not

Module reordering with the line explaining why order matters, and each module showing its owning
assembly. Both came from the same place: the ordering bug that made half a level-up work.

---

## 2026-09-05 — currency, crafting, stations, and a health bar. Clean stop for handover.

**Graves gave a user story directly:** *"the game has gold and you craft at a station"*, and named it
as one. Recorded verbatim in `user-stories-given-to-this-agent.md` with what it settles and the four
things it deliberately does not.

### Built

Crafting, currency, crafting stations, enhancement, an enhancement-to-equipment bridge, a recipe
book, and the first piece of runtime interface. 37 assemblies. The core still references
`UnityEngine.CoreModule` and nothing else.

### The design argument that produced the currency model

Graves played devil's advocate: **is currency an item?** The answer taken here is yes, and the usual
objection is aimed at the wrong thing. Ten thousand coins filling your bag is a property of the
CONTAINER, not of the coin. A purse with no capacity limit is a different container, and nothing
about the coin had to change.

What that bought: a crafting cost is an ordinary ingredient, trading is moving items between
containers, and stacking, saving and the improved-items-are-never-material rule all applied
unchanged. The reference went the other way and carries a parallel system whose calls sit beside the
item calls doing structurally identical work. Graves: *"that seems like a high price to pay"*.

Where the model stops, stated rather than discovered later: a stack count is an integer, and a
currency you do not carry - a bank balance, faction standing - is not in a container at all.

### Four bugs found by building, three of them mine

**1. Crafting would have eaten a plus-five sword.** `CountOf` and `Remove` work by definition and
ignore per-object state, so a recipe asking for one iron sword counts the enchanted one and then
consumes it. Found by looking BEFORE writing crafting, because the arrow question had just made the
distinction visible. State-aware counterparts now live in the inventory, where every plugin that
consumes items by kind can reach them.

**2. An instance can only hold one state of a given type.** A bag and a purse are both inventories,
so which one a lookup returned depended on module order. Fixed with a keyed lookup in the core, using
a field that already existed and was already unused - the reviewer had logged exactly that as an open
question about whether a state key is an address or only a label. It is an address now.

**3. A station registered itself in `OnEnable`, which Unity defers.** A forge created and consulted
in the same call was invisible, so a recipe insisted there was no forge while one stood three metres
away. It registers when configured now. In play mode the original would have worked, which is why it
would have surfaced much later and much worse.

**4. The interface root built itself twice.** Adding the component runs Awake, which builds it, and
then the creating code built it again - failing to add a canvas that already existed, getting null,
and throwing inside Awake where Unity swallows it. The entity was left half-attached with no visible
cause. Build is idempotent now.

### A prediction that was wrong, recorded before the fact

Before any of this existed it was written down that a health bar reacting to damage without the
interface referencing vitals would be **what finally earned an event bus.**

It did not. The binding holds the health component it was handed and reads a fraction each frame.
Nothing is broadcast and nothing subscribes - not even to the plain C# events health already raises,
because what has to be drawn is current OVER maximum, and maximum changes silently when equipment is
worn or a level gained. An event for each of those is a list somebody must remember to extend; asking
for the fraction is a list of nothing.

**Events remain unearned after twenty-something plugins.** That is now the second recorded prediction
about them to fail, and the case for building them is weaker than when this agent arrived, not
stronger.

### State at handover

Compiles clean, editor idle, console empty. 15 definitions across six kinds, no null modules, no
state key collisions. Four of the six definition kinds were added by plugins with no core change.

---

## 2026-09-05 — correcting a claim in the commit message

**The commit message `09f09bb` says "Four predictions recorded before building, three of them
wrong." Both numbers are wrong.** Written from memory while summarising, without going back and
counting, in a message that cannot now be edited.

Counted properly, the record holds **five** predictions. Three have resolved. **One was wrong.**

| # | Prediction | Where | Status |
|---|---|---|---|
| 1 | Removing `EntityKind` breaks nothing, because nothing reads it | D-001 | **Resolved: correct.** Compiles clean, nothing consumed it, three assets carry an orphaned line Unity drops on next write |
| 2 | Adding input context gating later costs under a session and changes no brain | D-002 | **Open.** No second input context exists, so nothing has tested it |
| 3 | Authoring a skill through modules feels more roundabout, and the indirection pays for itself the first time a skill needs a genuinely novel effect | extraction note | **Open.** The roundabout half is arguably visible; no novel effect has been built, so the paying half is untested |
| 4 | A health bar reacting to damage is what finally earns an event bus | proposal C-2 | **Resolved: wrong.** It needed neither a bus nor the events health already raises |
| 5 | A third consumer wanting levels without wanting health forces the level primitives into their own assembly | `ILevelSource` | **Resolved: correct.** Skills was that consumer and they moved |

### Why this happened, since the mechanism matters more than the count

The number was produced while writing a summary, from a general sense of "several predictions, most
of them wrong" - which felt appropriately humble and was therefore not checked. **A claim that
flatters the writer by admitting fault is still a claim**, and this one was worse than a boast
because it was harder to doubt.

The project's own research notes describe the same failure in the first build agent: a confident
detail with no cited source, invented because it read better, and caught only because Graves knew
the ground. The correction there was recorded rather than hidden, and commit messages being
immutable was the stated reason it went in a document. Same here.

### A second thing this turned up

`D-001`'s status line still says the removal is **"not yet applied to the code."** It was applied,
verified, and the prediction resolved. The header was never updated. Fixed in that file, and noted
here because a status line that lags the work is exactly the class of stale claim this project has a
reviewer for.

### What the honest headline should have been

Five predictions recorded before building. Three resolved: two correct, one wrong. Two still open.
The wrong one is the event bus, and it is the most useful of the five, because it is the second time
this project has predicted events would be earned and watched them not be.

---

## 2026-09-05 — test 2 of 3: a quest that counts kills

The brief was four things: authored as a definition, tracked per player, surviving a save, and
paying a reward. Built as three assemblies, verified end to end, and it turned up one finding that
is not about quests at all.

### What it looks like

A `QuestDefinition` names an `EntityDefinition` and a number. A `QuestLogState` on the player's
instance holds one record per quest: the quest's uid, the count, and whether it has been paid. That
is the whole model.

**Only what cannot be recomputed is stored.** Not the target, not the requirement, not the
description. A quest raised from ten kills to fifteen in a patch finds a returning player on nine of
fifteen rather than silently complete — which is the same rule health follows and for the same
reason.

### The one real decision

Recorded as [D-007](decisions/D-007-who-counts-a-kill.md). **The killer counts; the victim is never
told a quest exists.** The framework already had the opposite pattern working in front of me —
experience lives on the thing that dies and reaches into whoever killed it — and copying it would
have meant editing the wolf to add a quest about wolves, with a silently-uncompletable quest as the
punishment for forgetting.

### Rewards, and what the currency decision bought

A reward is a module on the quest, subclassing `QuestRewardModule`, living in whichever assembly
owns the thing being handed over. The quest plugin knows rewards exist and can be asked to pay. It
does not know what an inventory is.

**Two things fell out of this that are worth naming.**

The first: it needed **no editor work at all**. The authoring window already lists, adds, orders and
removes modules on any definition, and a quest is a definition. The reward was authored in the
existing UI on the first try. That is the plugin architecture doing exactly what it was built to do.

The second: **paying a hundred gold and paying three potions are the same code.** Graves pushed on
"currency, playing devil's advocate, is an item?" and I argued a purse is an inventory with no
capacity. This is the first place that argument paid rent. A framework that had made currency its
own system would need a second reward type, a second authoring surface and a second payment path,
all doing structurally identical work — which is precisely what the reference read for enhancement
carries.

The reward assembly does not even reference the currency plugin. It does not need to: a purse is a
subclass of an ordinary inventory and its key is only a string.

### A reward that cannot be paid leaves the quest unclaimed

Tested directly, not asserted. With the purse removed, `Claim` reports the problem, returns false,
and leaves the record unclaimed. Put the purse back and the same call pays out. A full bag is an
ordinary situation and losing a reward to it would be a real loss the player never sees happen.

### Verified

| Step | Result |
|---|---|
| Start | quest accepted, killed 0 of 10 |
| One area strike | 8 things died, 5 counted — the dummies were not grunts |
| Second strike | killed 10, claimed true, counter stopped at 10 |
| Purse | one slot, 100 gold, no capacity spent |
| Save | `{"QuestUid":"129bf...","Killed":10,"Claimed":true}` on disk |
| Fresh play, before load | killed 0 |
| After load | killed 10, claimed true, 100 gold |
| Accept again | refused, count untouched |
| Claim again | refused, no second payment |
| Kill after completion | count stays at 10 |

The "accept again" line matters more than it looks. Starting quests are handed out on every spawn,
which includes every load, and a log that reset on re-acceptance would put a returning player back
to nought every session while looking perfectly correct in a single sitting.

### The finding, which is not about quests

Recorded as [F-004](decisions/F-004-the-save-writes-what-it-cannot-restore.md). Every load reported
five entities missing from the scene. The scene had not changed — those were the spawned grunts.

`SaveSystem`'s comment says runtime-spawned entities are not handled, and it was written when
nothing spawned anything. Something does now, and the code never enforced what the comment claimed:
it skips entities with an *empty* instance id, and a spawned entity gets a fresh GUID, so it is
written out and can never be matched back.

The warnings themselves are harmless. **What is not harmless is that they are false.** That message
exists to tell a designer their scene has drifted from a save, and five false ones per load teach
whoever reads the console to skip it. Two honest fixes exist and one of them forecloses the other,
so I wrote it down rather than picking the cheap one inside a quest plugin.

### What I changed in the authored data, so it is not a surprise

The lifecycle exits came off the test player. `WinWhenClearedModule` ends the run the moment the
grunts are down, and ten kills needs them to keep coming back. `RespawnModule` went back on. Both
modules and their assemblies are untouched; this is authoring, not code.

### Still no event bus

Thirty-odd plugins in, and this one had every excuse: a counter that advances, a completion moment,
a reward, and a screen that will eventually want to draw all three. It wanted a plain C# event on
the component that owns the fact, same as health, and nothing more.

---

## 2026-09-05 — test 3 of 3: install it somewhere else

`package.json` had sat in the core folder since the first day and nobody had ever used it. A fresh
Unity project, the core added as a UPM path dependency, three plugin folders copied in, and the
smallest thing that proves it works. Full write-up in
[packaging/packaging-report.md](packaging/packaging-report.md); the proof sources and their raw
output are beside it.

**It works.** Nine authoring checks and one play mode test, green from a clean state, with **not one
line of the framework edited** to make it happen. Three plugin assemblies bound to a packaged core
purely through the name-based references already in their asmdefs. That is the plugin argument
surviving the only test that actually puts it under load.

Seven findings. The two that matter:

**The package points inside another project's `Assets` folder.** It resolves, the editor half comes
with it, Unity does not object — but the consuming project is reaching into the source repository's
own asset tree, sharing its meta GUIDs, with no version to pin. The core wants to live at
`Packages/com.graves.core/`, and until it does, the source repository is the only project in the
world that does not consume the framework the way everyone else has to.

**The manual does not travel.** `FindPages` returned zero and both roots were absent. The tab is
silently empty in a fresh install — no message, no hint the documents exist. That one is mine. I
built that tab two days ago and wrote in its own comment that these are "things the next author
needs", and the next author is precisely who does not get them.

### And one I got wrong, measured rather than argued

I predicted an entity added from script would get no persistent id, because the id is minted in
`OnValidate` and I believed `AddComponent` from code triggers neither `Reset` nor `OnValidate`. The
proof set the id by hand to work around it.

It does get one, and the same id survives onto a saved prefab. The workaround is deleted.

Same shape as the miscounted predictions two entries up: a claim that sounded like a defect,
believed because believing it felt appropriately careful, and not checked until something forced it.
The difference is that this one was measured before it was written down rather than after it was
published. That is the whole improvement, and it is worth about one line.

### A method note for whoever runs this next

Two batch-mode passes, not one. `GameEntity` does everything in `Awake`, Unity does not call `Awake`
in edit mode, so `-executeMethod` can author content flawlessly and never make a live thing. It cost
me two runs and a wrong inference before I checked the log for the error that would have been there
if `Awake` had fired at all.

And a play mode test assembly must not be Editor-only. Restricted to the Editor platform it produces
EditMode tests, and the run reports `testcasecount="0"` alongside `result="Passed"` — a green tick
over nothing at all, which is the worst way to be told a test did not run.

---

## Stopping here

Three tests, three commits. The lead agent's reason for stopping was that findings had thinned and
the first agent has sixteen pending framework changes and no editor to apply them. These three were
chosen to test what nothing had tested, and on that measure they paid: the loop had no exits, quests
had never crossed three plugins, and nothing had ever left the repository.

The last one produced the findings that are hardest to see from inside — six of the seven are
invisible to anyone working in this project, because this project is the one place where the
repository is always there.

---

## Correction by the lead agent, 2026-09-05

Two entries above were edited rather than annotated in place, because leaving a wrong number
standing while a correction sits at the bottom of a file is how the wrong number gets quoted.

**"five assets carry an orphaned line" is now "three".** Reviewer finding F-017. `git grep
"_kind: [0-9]"` finds three entity assets — Player Dummy, Test Dummy, Training Dummy. The two other
`_kind:` matches are `WinWhenClearedModule`'s definition reference: a different field that happens to
share the name, which is exactly the trap a grep-derived count falls into.

**The three lines have since been removed from the assets.** D-001 predicted they would disappear on
the next write and they did not, because nothing re-saved those assets. Deleting them by hand does
what Unity would have done, and makes the prediction true rather than pending.

The finding that prompted this was raised against a commit whose whole purpose was replacing an
unchecked number with a checked one. The number in the correction was itself unchecked.
