# D-001 — `EntityKind` goes

**Decided:** 2026-09-05, by this agent. **Status:** applied, and the prediction below resolved
CORRECT - the removal compiled clean and nothing consumed the field.

*Header corrected 2026-09-05: it read "not yet applied to the code" long after it had been applied.
A status line that lags the work is exactly the kind of stale claim this project keeps a reviewer
for, and it was found while auditing a different mistake.*

## The question

`EntityDefinition` carries an enum with three values — player, NPC, enemy. The reviewer logged it as
an open question (Q-002) because the repository argues both ways and records no decision.

## Checked, not assumed

`grep` over `Assets/Graves` for `EntityKind` and `.Kind` returns three hits, all inside
`EntityDefinition.cs`: the field, its property, and the enum declaration. **No consumer anywhere.**
Two authored assets carry a serialised value.

The field's own tooltip says it is *"Used to sort the authoring window"*. The authoring window never
reads it. That is a claim contradicted by the code, independent of whether the field should exist.

## The decision, and the argument that decided it

**It goes.**

The argument against removal is the strongest one and comes from Graves' first user story, which
names *"my player, the NPCs, the enemies"* as the things to define. That reads like a three-way
split in the data.

It is satisfied by three definition assets. Nothing about that story requires the framework to know
which of the three a definition is.

**What decided it was building, not reasoning.** Stage 1 put a player control module and an AI brain
module on two definitions. Being the player is now conferred by carrying a module that reads a
keyboard; being a monster by carrying one that chooses its own targets. Swap the modules and the same
body changes hands, with nothing else touched anywhere.

So the answer to *"which definition is the player"* is already recorded, in the modules, where it is
also actionable. `Kind` would be a second place that says the same thing, kept in step by hand, and
able to disagree. Two truths about one fact is the failure this framework's identity work exists to
prevent, and the categorisation document reached the same conclusion from the opposite direction:
player-ness comes from the controller, not the data.

## Why it is not applied yet

Removing it is a core change, and the core is the one thing shared with the first build agent's
work. It costs nothing to defer and the deferral is visible here rather than implied by silence.

**Prediction, recorded before doing it:** the removal will break nothing, because nothing reads it.
The only visible effect will be two authored assets losing a serialised line. If something does
break, the interesting part will be what was reading it, and this file will be wrong in a way worth
reading.
