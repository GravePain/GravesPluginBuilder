# D-003 — Split an assembly for a dependency, not for a category

**Decided:** 2026-09-05, by this agent, with Graves' explicit go-ahead to change the rule if the
reasoning was sound. **Status:** rule changed, no code changed.

## The rule as it stands in the repository

`Docs/plugin-categorisation.md`:

> where a plugin spans categories, give each category its own assembly

## The rule this agent is working to instead

**Split an assembly when a part carries a dependency the rest should not inherit, or when a part is
useful installed on its own. Otherwise merge it.**

Category is not a trigger. It is a description that usually agrees with one.

## Why the change

**The repository's own strongest evidence was produced by the dependency rule, not the category
rule.** The animation demo assembly was split off before the categorisation document existed, and
for one reason: it needed the Input System, and forcing that package on every user of the clip
runner was unacceptable. The categorisation independently agreed afterwards.

That document draws the right conclusion from it — two arguments landing on the same boundary is
evidence the boundary is real — but then promotes the wrong one of the two to a rule. The
dependency argument is the one that did the work. The category argument arrived second and
confirmed.

**Category alone splits things that have no reason to be apart.** Sixteen headings applied
faithfully would produce assemblies whose only distinction is which paragraph of a document they
were filed under. That is cost with no benefit: Unity carries real per-assembly overhead, and each
assembly is one more thing a plugin author must wire correctly.

**Dependency is checkable and category is a judgement.** Whether a part pulls in a package is a
fact about the build. Whether it is "layer 7 or layer 8a" is an opinion, and rules resting on
opinions drift.

## The second clause, and why it is needed

Dependency alone would merge vitals into combat, since vitals carries nothing combat does not
already have. That merge would be wrong, and the reason is not category either.

**Vitals is useful with combat deleted** — destructible scenery, fall damage, a survival game with
no fighting in it. Combat is not useful with vitals deleted, because there is nothing to damage.
The dependency runs one way and the standalone usefulness runs the other, so they are two units of
installation rather than one.

## Applied to everything built today, including the cases that could have failed

| Split | Dependency reason | Standalone reason | Verdict |
|---|---|---|---|
| Locomotion / Locomotion.Player | Player half pulls the Input System | — | stands |
| Combat / Combat.Player | Player half pulls the Input System | — | stands |
| Combat / Combat.Animation | Animation half pulls the clip runner | — | stands |
| Vitals / Combat | none — same dependencies | Vitals is useful alone | stands |
| Input | pulls the Input System | needs nothing from the core | stands |
| Camera | pulls input for orbiting | — | stands |

**Nothing merged.** That is worth stating plainly rather than presenting as vindication: the rule
was chosen before the table was filled in, vitals and combat genuinely could have merged under the
dependency clause alone, and the second clause was added because that merge felt wrong and the
reason had to be articulated. A rule invented to preserve existing structure would be worthless
here.

**The test to apply next time, in one line:** delete the other assembly. If what remains is still
useful, or if merging would force a dependency on someone who does not want it, the split is real.

## An observation this exposed, recorded rather than acted on

`Graves.Plugin.Input` references nothing from the framework. The assembly definition originally
named the core, the compiler dropped it because not one core type is used, and the loaded domain
showed the truth.

So it is the first assembly here that is optional, installable, deletable, and completely
independent of the framework it ships with. That is evidence the plugin contract is thin enough to
be absent entirely, which is a stronger claim than the guide currently makes.

The name was left alone. Calling it a plugin is still accurate — it is a thing a game chooses to
install — and renaming it now would churn three dependents to correct a word.

## The tax that should be documented, and currently is not

The guide says a plugin is one virtual method and a serialisable class, and that is true of the
code. It then quietly also requires a correctly wired assembly definition, and getting that wrong
produces errors that read as missing *types* rather than missing *references*, which sends the
author looking in the wrong file entirely.

The advertised contract is smaller than the real one. Worth closing before an outside author meets
it.

---

## Amended 2026-09-05, after retrospective feedback from the lead agent

Two corrections, both accepted. Recorded here rather than edited in above, so the original stands.

### The second clause is provisional

"Useful installed on its own" rests on **one case**, and it was articulated after a merge felt
wrong rather than before. That is the weakest way a rule can arrive, and this document already said
so, which is the only reason it should be trusted at all.

**Marked provisional until a second, unrelated case tests it.** If the next candidate split turns on
standalone usefulness and the clause produces an answer that feels wrong, the clause is doing the
work of taste rather than the work of a rule, and it should go.

The first clause is not provisional. It is causal, checkable, and produced the evidence.

### The consequence this document failed to state

**The rule demotes `Docs/plugin-categorisation.md` from a rule-giver to a vocabulary.**

Under the category rule, which layer a part belonged to had a structural consequence: it decided
whether that part got its own assembly. Under this rule it has none. Two parts in different
categories, with the same dependencies and no standalone use, are **one assembly** — and the
categorisation has nothing to say about it.

That is correct and it should be said out loud, because that document currently reads as though it
governs something. What it is genuinely good at is naming things, and its strongest moment was the
smoke test that could not place the animation plugin and split layer 8 into playback and bridge as a
result. That is a vocabulary earning its keep, and it is a different job from deciding builds.

**So the categorisation is a map for placing and naming work. The dependency rule decides
assemblies. Neither one does the other's job.**
