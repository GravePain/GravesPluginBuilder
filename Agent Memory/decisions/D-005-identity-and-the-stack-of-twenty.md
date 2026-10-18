# D-005 — Assigned identity, and the stack of twenty

**Decided:** 2026-09-05, after Graves asked a question the framework could not answer.
**Post-contact:** yes. The reference was read before the gap was closed.

## The question

> Enhance one arrow in a stack of twenty. What is in the slot now?

The framework could describe the answer and could not produce it. Twenty arrows are **one instance
with a count beside it**, because twenty identical arrows have nothing to tell apart. Enhancing one
makes it different, and the stacking rule already refuses to merge anything carrying state — so the
enhanced one has to become its own object.

**There was no operation that could do that.** The inventory could add, remove by definition, remove
by instance, and move between slots. It could not take one out of a stack and give it an identity.

## The two coherent models, and this framework is only one of them

**Derived identity.** An item IS its definition plus everything about it that can differ. The shipped
forge read as reference locates an item with
`FindItem(id, upgradeLevel, enchantments, socketedItems)` — no instance id exists anywhere.

- Identity and stacking are **one mechanism**. Two items alike in every respect are the same item,
  which is exactly why stacking is free.
- Identity **changes when the item changes**, so every mutation is a re-find, and the display copy
  has to be patched by hand afterwards. Four call sites in one file do that dance.
- The key **must be exhaustive**. Add a fifth kind of per-item state and every existing call is
  silently addressing the wrong thing.
- One particular object **cannot be addressed at all**, and that is deliberate rather than a
  shortcoming.

**Assigned identity, which is this framework.** An instance carries a minted id that owes nothing to
its state.

- Every object is addressable, and its identity survives being changed.
- Identity and stacking are **two mechanisms**: the uid says which one, and `CanStack` says whether
  two may merge.
- That is why "twenty arrows, one of them enhanced" is expressible here and is not expressible there.

**Neither is better. They buy opposite things.** Theirs never needs a split, because the situation
cannot arise: enhancement is gated to a category that does not stack, which is the same rule the
reference framework enforces at validation time. Ours can express the situation and therefore has to be able to perform
it.

## What was built

`InventoryState.Split(slot, quantity)` — takes some of a stack into a slot of its own and gives what
leaves a **fresh instance id**. Reusing the stack's id would hand two different things one identity,
which is the failure the whole uid design exists to prevent.

A small enhancement plugin alongside it, so the story could be tested with a real thing rather than a
stand-in: a state holding a level, a module saying how far an item may be improved, and one call that
improves it. No cost, no failure chance, no forge — that is an economy and an economy is a game's
decision.

## The landmine that turned out to be defused, and why it is still checked

`ShallowCopy` duplicates the state **list** and not the state **objects** in it. Splitting an arrow
off and then enhancing it would ordinarily enhance both.

It cannot here, and the reason is not in this code: **a slot holding more than one thing can only
hold things with no state at all**, because that is what stacking requires. The list being copied is
always empty, so there is nothing to share.

A guarantee that rests on a rule enforced elsewhere is worth checking rather than trusting. A stack
found carrying state means the stacking rule was bypassed somewhere, and the split refuses and says
so instead of quietly producing two arrows that sharpen together.

## Verified, not asserted

Twenty arrows in one slot. Split one, enhance it. Afterwards:

| Check | Result |
|---|---|
| Original slot | 19, same instance id, no state |
| New slot | 1, different id, enhancement level 1, one state |
| Will the two re-merge | No — `CanStack` refuses |

## What this does not do yet

Enhancement changes nothing about a weapon's numbers. The state exists and is saved; nothing reads
it. Wiring it into the equipment bonuses is the obvious next step and needs a decision about which
plugin depends on which, since the thing that scales a bonus by an enhancement level has to know
about both.
