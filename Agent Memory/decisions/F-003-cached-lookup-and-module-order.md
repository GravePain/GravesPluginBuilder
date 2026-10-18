# F-003 — Half the stats scaled with level and half did not

**Found:** 2026-09-05, first test of the progression plugin. **Status:** fixed.
**Post-contact:** decided after reading the lead agent's retrospective.

## What happened

The player reached level two. Damage rose from 10 to 13, exactly as authored. Maximum health stayed
at 200 when it should have been 220, also exactly as authored.

Same mechanism, same struct, same level. One worked.

## Why

Health and the attacker each look up a level source once and hold it, because a component lookup on
every read of a stat is not free and components do not normally come and go.

**Modules attach in author order, and vitals attaches before progression.** Configuring health reads
its maximum, which asks for a level source, finds none because progression has not run yet, and
caches that answer permanently. Combat happened to be read for the first time after progression had
attached, so it cached the real thing.

The order was not chosen. It is the order the modules happen to sit in on the definition.

## Why it is worth a document

**It does not present as a bug.** Nothing errors, nothing is null, no log appears. A level-up simply
does less than the numbers say it should, and only for some stats. Read as a player, that is a
balance problem. Read as an author, the values on the asset look correct, because they are.

**It scales with the number of plugins.** Any plugin that caches a lookup of another plugin's
component inherits this, and the failure depends on authoring order in a definition, which nobody
thinks of as significant. This is the second time on this project that a cached copy of somebody
else's state has failed silently - see `F-002-state-is-the-storage.md`, where a save restored
correctly into a value nobody was reading.

**The pattern is the same both times:** something held a private copy of a fact that lives
elsewhere, and the copy went stale with no error. Once for state, once for a component reference.

## The fix, and the shape it had to take

Consumers gained a method to look again, and the progression module calls it after installing a
level.

The obvious version of that call reaches for health and the attacker by name, which would make a
plugin about levels depend on a plugin about combat, and force every game wanting levels to ship a
combat system. **An interface avoids it:** consumers declare that they read a level, progression
refreshes whatever declares it, and the dependency stays pointing the right way.

Verified after the change: level two, maximum health 220, damage 13.

## The rule this suggests, offered rather than adopted

Caching a lookup of another plugin's component is safe only if the cache can be invalidated and
something is responsible for invalidating it. **A cache with no invalidation path is a bug waiting
for an authoring order.**

Not made a general rule yet, because two cases is not a pattern and the alternative - looking up on
every read - has a real cost in the hottest paths. The narrower statement is safe: **if you cache,
expose the refresh, and make whoever installs the thing you cached call it.**
