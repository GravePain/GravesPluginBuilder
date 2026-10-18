# Where things stand

Written 2026-09-05, at the end of the second day. For picking this up cold — by Graves after a break,
or by a session with none of this in its context.

## The framework is finished, by a definition that was written down first

Four things, all done:

1. **No open findings.** Twelve answered in `Docs/reviewer/replies.md` — ten fixed, one moot, one
   already correct. `findings.md` still shows them open because that file is the reviewer's and a
   builder does not mark their own homework.
2. **The draft is empty.** `Docs/DRAFT-pending-changes.md` — everything done or declined in writing
   with the reason.
3. **An event bus exists**, after three recorded refusals, reversed on outside evidence.
4. **A game runs on it.** `Assets/Game/Survival/SurvivalLoop.unity`. Gather, craft, save, load.

**The stopping rule, which is the part that matters:** the framework is done. Anything it still lacks
is a fact about a game, discovered by building one, added then. Both frameworks studied this week died
without ever writing that sentence.

## The game

Open `Assets/Game/Survival/SurvivalLoop.unity` and press play. **WASD** move, **E** gather, **C**
craft, **F5** save, **F9** load. Three saplings, two outcrops, four wood and two stone makes a stone
axe.

It lives in its own assembly outside `Assets/Graves`, and nothing in the framework references it.

## Shape, in numbers rather than adjectives

- **14 of 19 plugins** can be deleted with no other plugin's root dying.
- The only removals that kill anything: Locomotion (3 roots), Inventory (3), Vitals (2), Input (1),
  AI (1).
- **46 modules** across 24 assemblies. The live list is the Graves window's "Add module" menu, built
  by reflection. Do not write it down anywhere; a roster in a document went stale twice in one day.

## What moved into the core, and why

`Stats`, `Save`, plus two new pieces. Each on a measurement, not a preference:

- **Stats** — removing it killed seven plugin roots. A standard library mislabelled as optional.
- **Save** — a core that defines content and cannot make any of it survive a restart is a
  prerequisite, not a product. Reverses a written decision, and says so in `ISaveable`.
- **`IMortal`** — three plugins depended on Vitals to ask "is it dead". Vitals went from five roots
  to two.
- **`GameInstance.IsPlain`** — "are these two the same thing" was one line inside the inventory's
  stacking rule, and crafting had to depend on inventory to ask it.

**The rule that came out of it: only move something into the core when it is clean.** Save moved with
three findings open against it and they were closed afterwards. That worked and the order was wrong.

## If picking this up cold, read these three first

- `Assets/Graves/Core/Documentation~/writing-a-plugin.md` — what a plugin is and the rules that are
  not negotiable.
- `Docs/reviewer/replies.md` — every finding, its scope, and why core and plugin findings are not the
  same claim.
- The reference notes on a commercial framework built twice by professionals and abandoned. Those
  notes were removed before publication, but the conclusion is the shape this project is trying not
  to become: a core rated 3.3 while every one of its plugins rated 5.0, buyers waiting months on one
  maintainer, and two frameworks that never produced a game.

## Open, and deliberately not done

- **An inventory window.** The shape is settled — see `Graves.Plugin.UI.Vitals` for the pattern — and
  it should be built inside the game first. One consumer is not enough to make it a plugin.

