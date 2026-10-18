# Combat Bridge — adopting a tool that already works

This sample does not compile on import, and that is the point. It references an assembly that is
not part of this package and never will be: an external combat framework with its own
`DamageResolver`, `CharacterBrain` and `HealthSystem`. Import it, point the assembly definition at
your own tool, and it becomes about forty lines of translation.

## What it is for

Anything already proven — a damage resolver, an inventory, a dialogue runner, a tool carried between
projects for years — should not be rewritten as a plugin. Rewriting throws away the one thing that
cannot be re-derived from a shape sheet: every edge case it already survives.

**So adopt it instead.** The tool stays exactly as it is, in its own assembly, knowing nothing about
this framework. A third assembly references both and holds one module and one component. If the tool
is ever replaced, only that third assembly dies.

```
  Framework.Runtime  ←──  Graves.Plugin.CombatBridge  ──→  Graves.Core
   (yours, untouched)      (the only new code)              Graves.Plugin.Vitals
```

Neither side references the other. That is the whole trick, and it is the same trick the Death
plugin uses to join two plugins without either depending on the other.

## The two questions a bridge has to answer out loud

**1. Who owns the number.** Both sides here keep health, because both were written to. This bridge
says: the combat framework's `HealthSystem` is the truth during a fight, and the entity's `Health` is
the record that survives one. They stay in step because both are driven from the same resolved hit,
never from each other — a mirror copying a mirror drifts, and it shows up as a health bar that
disagrees with the damage numbers.

Measured across twelve hits of 10.5 damage against 100 health: the two sides never differed by more
than half a point, died on the same hit, and both totals came to exactly 100.

**2. What it refuses to do.** This bridge does not push a loaded value back into `HealthSystem`.
There is no public way to set that field, and the only open door — applying the missing damage —
would fire the framework's own damage and death events on load. A character restored at low health
would play a hit reaction; one restored at zero would die again on the loading screen.

**A bridge must not pick locks on either side.** Reaching past a private member with reflection is
what the first plugin in this framework did, and it was wrong then too. The gap is real, it is
reported once and loudly at attach time, and the fix belongs in the adopted tool: a way to set
current health. Until then, a saved fight resumes with that side at full.

## Two details worth stealing

**Carry the remainder.** The combat framework deals fractional damage; health here is whole numbers,
because a save has to round-trip exactly. Rounding each hit on its own loses up to half a point every
time, and over a long fight a character survives blows that killed them. Carrying the remainder into
the next hit makes the total exact and costs one float.

**Check the hit was aimed here.** The framework's `OnHitResolved` is a static event, so every bridge
in the scene hears every hit. Without the target check, one landed blow damages every living thing in
the level — and everything dying at once looks nothing like its cause.

## Verifying it

Put the tool's assembly in the project, fix the reference name in the `.asmdef`, and compile. Then
drive it from a scratch object rather than a scene: build a defender, resolve a dozen hits through
the tool's own entry point, and print both sides' health after each one. If the two columns diverge,
the bridge is copying rather than translating.
