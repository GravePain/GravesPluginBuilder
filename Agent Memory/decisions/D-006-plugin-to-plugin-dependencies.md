# D-006 — Plugins may depend on plugins

**Decided:** 2026-09-05, after Graves pointed out that nobody had ever ruled on it.
**Status:** stating a rule the project has been following without saying so.

## The gap

Every document here is careful about one direction: **the core must not depend on a plugin**, and it
is enforced by assembly definitions rather than promised. That rule is stated repeatedly and checked
by reflection.

**Nothing anywhere says whether one plugin may depend on another.** Graves noticed the omission and
argued that forbidding it is impossible.

He is right, and the evidence is already in the build. Combat cannot hurt anything without vitals.
Equipment cannot take an item out of a bag without an inventory. A skill that swings cannot avoid the
thing that owns swinging without duplicating hit detection, which is worse. Fourteen of the
twenty-odd plugin assemblies here reference at least one other plugin, and every one of those
dependencies was load-bearing when it was made.

## The rule

**Plugin-to-plugin dependencies are allowed, and are the normal way a plugin uses another plugin's
concepts.** What matters is not whether they exist but what shape they are.

**One direction only.** If combat depends on vitals, vitals must never depend on combat. A cycle is
not merely untidy - Unity refuses to compile it - so the compiler enforces this without anybody
having to check. That is the same trick the core boundary uses, and it is why no rule is needed
beyond naming it.

**A dependency must be a decision.** Adding a reference to make a compile error go away is how a
plugin quietly acquires a dependency nobody wanted. The test is the one already written down: would
somebody installing this plugin be surprised to find the other one required?

**Where the answer is yes, split rather than depend.** That is D-003 doing its job, and it is why
equipment has a stats half, why skills have combat, animation, equipment and vitals halves, and why
this document's own example got its own assembly. A game that wants equipment and no combat installs
the core equipment plugin and stops.

## What this does NOT license

**It does not weaken the core rule.** The core still references nothing, and that is still checked
against the loaded domain rather than against the assembly definitions, because those two have
already disagreed once on this project.

**It does not make a dependency free.** Every reference is a thing a future author must install,
compile and understand, and the cost lands on somebody who did not make the choice. The reference
project this framework reads for requirements is a warning here: its addons depended on so much of
its core that a change of shape meant a rewrite of 477 files, and the person who wrote them did not
pay it.

**The thinner a plugin's knowledge of anything else, the longer it survives.** That was said about
the core and it applies unchanged between plugins.
