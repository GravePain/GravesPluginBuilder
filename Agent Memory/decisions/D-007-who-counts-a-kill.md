# D-007 — Who counts a kill

**Status:** applied. `Graves.Plugin.Quests.Combat.QuestKillTracker`.
**Date:** 2026-09-05
**Prompted by:** test 2 of 3 — "kill ten of something, get a reward".

## The decision

**The killer counts its own kills. The victim is never told a quest exists.**

A component on the entity that swings subscribes to its attacker, and when a blow leaves the target
dead it tells that entity's quest log which *kind* of thing died, by definition uid.

## What it was chosen against

The framework already had the opposite pattern sitting in front of me, and it works:
`ExperienceReward` lives on the thing that dies, listens to its own death, and reaches into whoever
killed it to pay out. Copying that shape would have been the path of least resistance, and it is
wrong here for one reason.

**Experience is a property of the victim. A quest is not.** What a goblin is worth to kill is a fact
about goblins, so the goblin should carry it. Being the tenth goblin someone was asked to kill is
not a fact about goblins at all — it is a fact about the person doing the asking. Putting the
counting on the victim would mean:

- adding a quest to hunt wolves requires editing the wolf, and
- forgetting to edit the wolf produces a quest that is silently never completable.

That second one is the killer. A quest that never progresses looks identical to a quest that is
broken, and the mistake is made in a file nobody would think to open.

The direction chosen matches how the content is authored. The quest names its target; the target
does not know it has been named, and never needs to.

## What it costs, stated rather than buried

**Only a killing blow delivered through an `Attacker` counts.** Something that drowns, burns down
over time, falls off the world, or is removed by a script is a death nobody claims. When any of
those exist, `QuestKillTracker` is the file that has to learn about them.

That is a real limit and it is not hidden — but it is also the honest limit for the game that
exists. Every point of damage in this framework currently flows through `Attacker.Strike`, melee and
skills alike, so there is no second path today.

## Why it listens to the hit rather than to the death

Subscribing to the victim's `Died` event would mean subscribing to something new on every swing and
unsubscribing from a corpse. The attacker already reports every blow it lands, and `Strike` refuses
to hit anything already dead — so a victim that is dead immediately after being struck was killed by
that blow, and there is no second claimant.

## What this bought

The quest plugin's own assembly references the core and nothing else. It does not know what combat
is, what health is, or what an inventory is. See [D-006](D-006-plugin-to-plugin-dependencies.md) —
this is the pattern applied twice more: a combat-facing assembly reports kills in, an
inventory-facing assembly pays rewards out, and neither is required to install the other.
