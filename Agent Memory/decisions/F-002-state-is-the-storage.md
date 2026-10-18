# F-002 — A load that restores everything and changes nothing

**Found:** 2026-09-05, first test of the save plugin by a plugin its author did not write.
**Status:** fixed in vitals. The underlying gap in the guide is not fixed.

## What happened

A grunt was hurt from 30 to 20 and the game was saved. The file on disk was correct — the wounded
grunt at 20, its sibling at 30, three dummies at 100, every state tagged with its type and key.

The grunt was then hurt to 10 and the save reloaded. The load reported:

    loaded=True restored=6 missing=0 unresolved=0 problems=0

Six entities restored, nothing missing, nothing unresolved, no problems. And the grunt was still on
10.

The state object said 20. The live component said 10. **Both numbers were correct and they were
different numbers.**

## Why

The save plugin restores with `JsonUtility.FromJsonOverwrite` onto the existing state object, in
place. Its own comment explains why:

> Overwrites in place rather than replacing the list, so a plugin holding a reference to its own
> state object does not find itself writing to an orphan after a load

That sentence is a design statement: **the state object is the storage, and a plugin is expected to
read through it.** The inventory plugin, written by the same author as the save plugin, does exactly
that.

The vitals component did not. It kept its own integer and copied it into the state whenever health
changed — a write-through cache with no invalidation. Saving worked, because the copy was pushed out
on every change. Loading did nothing, because the bytes landed in an object nobody was reading.

**The save plugin was right and the new plugin was wrong.** No error was possible from either side:
the save system did precisely what it promised, and the health component's own reads were internally
consistent.

## The fix

Health no longer keeps a copy. It reads and writes through the bound state when it has one, and
falls back to a private field when it does not, so it still works with no framework, no instance and
no save present.

**The fix deleted a class rather than adding one.** A separate component existed purely to copy
values between health and its state, subscribing to two events to do it. With the state as the
storage there is nothing to copy, nothing to subscribe to, and no window in which the two can
disagree. A load needs no notification, no callback and no polling — the bytes arrive in the object
already being read.

Verified after the change: hurt to 20, saved, hurt to 10, loaded, and the grunt is on 20.

## Why this belongs in the plugin guide, and currently does not

The guide describes the state mechanism as *declare a serialisable class and stop*. That is true of
the declaration and misses the constraint that makes it work.

The missing sentence is roughly:

> **Your state object is the storage, not a copy of it.** Read and write through it. A component
> that caches its state in a field will save correctly and restore nothing, and the load will report
> complete success.

The two existing plugins do not expose this. The inventory reads through its state and works. The
save plugin cannot detect that anybody has cached anything. **The gap was only visible to a third
plugin, written by someone who had read the documentation and not the save plugin's source.** That
is the strongest available argument that the contract needed writing down.

## The wider point about verification

Every instrument said this worked. The save file was correct on disk. The load result reported six
restored and zero problems. A component reading its own health got a consistent answer. The only way
to see the fault was to compare two numbers that no single caller ever compares — the live value and
the stored value, side by side, in a probe written for the purpose.

The existing rule on this project is that a return value is not proof. This adds a sharper case: a
detailed success report, with counts, is also not proof.
