# Deferred work, ranked

**Given by Graves on 2026-09-05, ranked by importance to the player.** His ranking, not this
agent's. Recorded verbatim so the ordering cannot drift.

1. Camera
2. AI
3. Crafting
4. Quests, dialogue
5. Audio

Planned later, but wanted. None of these is cut.

---

## What the ranking tells the build, beyond the order

**Crafting above quests and dialogue is a signal about the kind of game.** Crafting needs
items, equipment and stats to exist and to interact. Quests and dialogue need content authoring
and a conversation system, and lean far less on the backend. That the systems-heavy one ranks
higher says the backend work in stage 2 is aimed correctly, and that stage 2 should not be
thinned in favour of content features.

**AI at two is the thing that proves the controller/motor split.** Single-player removed the
netcode justification for that split, leaving one ground: a player brain and an AI brain drive
the same legs. Until an AI brain exists, that claim is untested and the split is taken on
faith. It is also cheap once locomotion exists, because the motor is already written and the
brain is the only new part.

## The one item this agent argues should move

**Camera is ranked first by the player and is also a tool dependency, which is a separate and
stronger reason to build it early.**

The framework's own rule: you can verify that something runs, you cannot verify that it looks
right, and the operator's eyes are the only instrument for the second. The masked animation
path passed every measurement taken and was still visibly broken.

Locomotion is judged entirely by eye. Without a camera that follows, Graves cannot judge it,
and this agent cannot ask him to. So the argument for building a minimal follow camera inside
stage 1 is not that the player wants one — it is that stage 1 is otherwise unverifiable by the
only method that works on it.

Scope, if accepted: a follow camera and nothing else. Modes, blending, target lock, dialogue
and cinematic framing stay deferred at rank 1 where Graves put them. The distinction is between
a camera as a game system and a camera as an instrument, and only the instrument is being
pulled forward.

---

## Added 2026-09-05 — what the packaging test left behind

Seven findings came out of installing the framework in a fresh project, and six of them are
invisible from inside this repository, because this is the one place where the repository is
always there. Full detail in [../packaging/packaging-report.md](../packaging/packaging-report.md).
Ranked here by what they cost the next person to install it.

1. **Ship the docs inside the package, and make an empty manual say why it is empty.** A fresh
   install shows a blank Manual tab with no message. The documents exist, were written for
   exactly this reader, and are the one thing left behind. Cheapest fix on the list and the
   largest difference to a newcomer.
2. **Move the publishable core out of `Assets` to `Packages/com.graves.core/`.** Consuming
   projects currently point at a path inside this repository's own asset tree, sharing its meta
   GUIDs, with no version to pin. A repository layout change, not a code change - and it would
   make this repo consume the framework the way everyone else has to.
3. **Give the package a README, a CHANGELOG, a LICENSE and a `documentationUrl`.** The Package
   Manager details pane is empty today.
4. **An honest way to create an entity from a definition at runtime.** The spawning plugin
   already reaches past the core with reflection and its own comment asks for this the moment a
   second caller appears. Installing the framework elsewhere is that second caller.
5. **Move `DataRoot` from a compile-time constant to a field on `CoreSettings`.** A framework
   should not decide where a game's own content lives, and a consumer cannot change a `const`
   inside a package without forking it.
6. **Package the plugins.** They install by copying folders today, which works and stays working
   until two projects want different versions of the same plugin.
