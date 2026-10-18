# D-004 — A damage filter instead of factions

**Decided:** 2026-09-05, by this agent, while fixing a bug found by running the game.
**Amended:** same day, after retrospective feedback from the lead agent.

## The problem it solved

Two grunts spawned symmetrically, converged on the player, and killed each other. A swing finds
everything with health inside its arc, which is correct for a sword and disastrous for a pack
arriving together.

## The decision

An interface answering one question about one victim: may this be hurt. Any component on the
attacker may veto. No filter means hit everything, which keeps a person's sword indiscriminate.

The AI plugin supplies one implementation: a hunter may only hurt things carrying the AI target
marker. Every hunter is therefore harmless to every other hunter.

Chosen over a faction system because factions are large — teams, hostility tables, reputation — and
none of that is needed to stop two monsters killing each other.

## The risk, as first stated, and why that statement was wrong

Originally recorded as: *when factions are built, that class is what they replace.*

**That mis-states where the cost lands.** The lead agent's correction, accepted:

> "every hunter is harmless to every other hunter" is a POLICY, and content will be designed around
> it before factions exist. Encounters get built assuming monsters never hurt each other. By then it
> isn't a class to swap, it's a behaviour to migrate, and the cost sits in content rather than code.

That is right, and it is the more expensive kind of cost. Swapping a class is an afternoon. Revising
every encounter that was designed assuming monsters cannot harm one another is not, and it is paid
by whoever authored the content rather than by whoever wrote the code.

**Recorded as a migration cost, not a replacement.** The question to ask before authoring anything
that leans on it: would this encounter still work if monsters could hurt each other? Each time the
answer is no, the migration gets more expensive.

## What would reduce the cost, if it becomes worth doing early

Nothing about the filter needs replacing to add factions — it is the right seam. What needs watching
is content. A single authored encounter that only works because a monster cannot friendly-fire is
the first payment on a debt nobody recorded.
