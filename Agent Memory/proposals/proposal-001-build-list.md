# Proposal 001 — the build list

**Written:** 2026-09-05. **Status:** APPROVED 2026-09-05, with a correction — see the end of this document.

Read first: the fifteen project documents, the whole core runtime and editor surface, both
plugin trees, and the movement section of the reference framework's manual.

---

## What the target changed

The brief is a single-player game with MMORPG-shaped systems, a human-usable editor, arranged
as plugins. The reference framework was chosen for its ScriptableObject backend and for nothing else.

Two consequences fall out of single-player immediately, and both narrow the work.

**The netcode argument for the controller/motor split is gone.** `Docs/plugin-categorisation.md`
justifies that split partly on replicating intent. Nothing will be replicated. The split still
earns its place, on one ground only: the player's brain and an AI brain drive the same legs. A
narrower claim than the repository currently makes, and it must not be inherited unexamined —
the same failure as the reference animation runner's re-entrancy guard, a mechanism kept after its reason evaporated.

**Their identity scheme becomes purely a warning.** `name.GetStableHashCode()` was chosen to
save bytes on the wire. There are no wires. Nothing about it transfers.

## The line I am building to

The framework's own test, unchanged: the core owns identity, structure, extension and
integrity; plugins own behaviour and presentation. A game is a loop — the player acts, the
world responds, the state persists, and it is worth doing again.

Three plugins exist. The loop is open at the front: nothing produces input, nothing moves, and
nothing changes as a result.

---

## Stage 0 — read the backend, write the note

**0.1 Extraction note: the reference framework's ScriptableObject backend.**

Read for the data model, not for technique and not for genre. The questions:

- How a definition type relates to its runtime instance, across items, skills and actors.
- How level-based stats are expressed as data (base value plus per-level bonus, per the manual).
- What is authored as a ScriptableObject and what is authored on a prefab, and why the line
  falls where it does.
- What their equipment slots key against.

Deliberately not read: networking, database, guilds, parties, chat, trading, world events, art.

No code. Output is a note in `extraction/`.

## Stage 1 — close the loop

This is the minimum that makes a game rather than a framework. Three plugins.

**1.1 `Graves.Plugin.Input`** — action maps per context, context switching, device changes.
Produces intent and holds no opinion about what consumes it. The animation plugin's demo
assembly already reads the keyboard; it is layer 5 living in the wrong plugin and is retired
into this one.

**1.2 `Graves.Plugin.Locomotion`** — two assemblies, because it spans two categories.
`Graves.Plugin.Locomotion` is the motor: intent in, movement out, character controller based.
`Graves.Plugin.Locomotion.Player` is the brain that turns input into intent. A navmesh motor is
a sibling plugin later, not a branch inside this one.

**Hard requirement, taken from the reference.** The manual's own swap procedure requires
reassigning the movement field on `Player` *and* on `PlayerSkills`. A second, unrelated system
holds a reference to the concrete movement component, so swapping the motor is surgery rather
than a swap, and the surgery grows with every system that ever asks where the actor is
standing. **Nothing here may hold a reference to a concrete motor.** Consumers take intent or
an abstraction. If a second system ever needs reassigning when the motor changes, this has
reproduced the defect it was written to avoid.

**1.3 `Graves.Plugin.Vitals`** — health as authored data plus per-instance state, damage,
death. The "world responds" beat. This is the first plugin whose state genuinely matters to a
save, so it is also the first real test of the persistence contract by someone other than its
author.

At the end of stage 1 a person can move, be hurt, die, and have that survive a restart.

## Stage 2 — the backend the reference framework was chosen for

**2.1 Stats and progression** — level-based values as data. Plugin, not core: what a stat is,
and how it grows, is a decision a particular game makes.

**2.2 Skills and abilities as definitions** — a new definition category, authored in the
window, driving the existing animation runner through named requests rather than Animator
parameters. This is the "8b" half the categorisation document predicted and does not yet have.

**2.3 Equipment** — items that modify an actor. The first plugin that has to make two other
plugins agree, and therefore the honest test of whether the contract holds.

## Stage 3 — the human surface

**3.1 A runtime UI plugin.** Window stack, drag between slots, position preserved. The
technique is already extracted and is one sentence: nothing remembers anything, the object is
never destroyed. Looks dated in the source, is sound underneath, and only the look is being
left behind.

**3.2 Authoring window work**, driven by what actually hurts after authoring real content. Not
speculatively.

---

## Core changes proposed

**C-1 — remove `EntityKind`.** *Mine to decide, deciding it: it goes.*

Checked, not assumed: `grep` over `Assets/Graves` finds three hits, all inside
`EntityDefinition.cs` — the field, its property, and the enum. No consumer anywhere. Two
authored assets carry a serialised value.

The field's own tooltip says it is "used to sort the authoring window". The window never reads
it. That is a claim/code mismatch as much as a design question.

Story 1 names "my player, the NPCs, the enemies" as the things to define, which reads as an
argument to keep it. It is satisfied instead by three definition assets. Being the player is
conferred by which brain is attached, which is exactly what stage 1.2 builds, and an enum that
says so separately is a second truth to keep in step.

**Recorded as a prediction, so the outcome is data:** removing it will cost nothing, because
nothing consumes it. If a later stage needs to ask "which definition is the player", the answer
should come from the controller, not from a field.

**C-2 — events and messaging: not yet, and here is the prediction.**

The repository's argument is that events are unearned after two plugins, because persistence
turned out not to need them. That held. My prediction is that **stage 3.1 earns them**: a
health bar must react to damage without the UI referencing vitals, and no amount of layering
avoids that. Stage 2.3 may earn them sooner.

Recorded now, before building, so that if it is wrong the wrongness is visible rather than
quietly corrected.

## What is deliberately not on this list

Guilds, parties, chat, trading, auction houses, world events, quests, dialogue, factions,
crafting. The catalogue of 126 addons is genre, not backend, and the reference was chosen for
its backend. Networking is not happening; it is what single-player means.

Camera, audio and AI are real work and absent from the loop. They arrive when something needs
them, not because a map has a heading for them.

---

# APPROVED — 2026-09-05, with one correction from Graves

> *"correct the game loop is killing things hence AI and it would be nice to see it but it
> doesnt matter to the player the order you do them as long as they are done at the end, on the
> basis approved"*

## The correction, and what it changes

Stage 1 as proposed read the loop as *move, be hurt, die, persist*. That is a loop in which the
player is only ever the object. **The loop is killing things.** The player must be the subject:
an act of aggression, something that answers it, and something that dies.

Two things move as a result.

**AI moves into stage 1.** It was ranked second in the deferred list and it is not deferred —
without something that fights back there is nothing to kill and no loop. It also stops being an
optional proof of the controller/motor split and becomes a load-bearing part of the first
playable thing.

**Vitals widens into combat.** Health, damage and death are necessary and not sufficient.
Damage has to be *dealt* by an act the player commits, or the player is a victim rather than an
agent.

Also confirmed: the camera is wanted but its ordering is free, so it is built inside stage 1 as
an instrument rather than as a game system, on the argument already recorded.

## Stage 1, as approved

| | Plugin | Categories | Why it is in the loop |
|---|---|---|---|
| 1.1 | Input | 5 | Produces intent. Nothing above it reads a device |
| 1.2 | Locomotion, two assemblies | 6, 7 | Motor takes intent; player brain produces it |
| 1.3 | Camera, follow only | 9 | Instrument: locomotion is judged by eye and cannot be judged without it |
| 1.4 | Vitals | 10 | Health, damage, death — the thing that can be lost |
| 1.5 | Combat | 10 | The act. The player is the subject of the loop, not its object |
| 1.6 | AI brain | 11 | Something that answers, and the only test of the motor/brain split |

Order within the stage is this agent's to choose; Graves has said the ordering does not matter
to the player provided everything lands. Stage 0 stays first because the extraction note is
what the rest is built from, and it now has to cover how the reference expresses combat as
data, not only items.

**Nothing was cut.** The deferred list keeps its ranking, minus AI and camera, which have been
pulled forward for the reasons above.
