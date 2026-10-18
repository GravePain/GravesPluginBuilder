# What order to build the plugins in

The order is not a preference. Two things on this list are far more expensive to add later than to
add early, and two more are cheap to add late and actively harmful to add early. Everything else can
move.

**The rule underneath it:** build what other things must be shaped around before the things that are
shaped around it. Persistence and identity are the two that everything else has to accommodate.
Presentation and observation are the two that accommodate everything else, so they go last.

---

## Before any plugin

A definition registry, a `CoreSettings` asset in a `Resources` folder, one entity definition, and one
entity in a scene that wakes up and spawns a capsule. **Do not start a plugin until that capsule
appears.** Every plugin attaches to that moment, and debugging a plugin through a broken entity is
debugging two things at once.

If the capsule appears as a capsule, that is the core telling you it found no representation. It says
so in the console. That message is the design working, not a failure.

---

## 1. Stats

**Depends on nothing at all — not even the core.**

Shared numeric shapes: a value that may grow with level, and the interface by which something reports
a level. Nothing here knows what a game is.

**Why first.** Every number in every plugin afterwards is either this shape or an argument for why it
is not. Declaring health as a plain float on day one means editing every stat in every plugin on the
day levels arrive — and levels always arrive.

**Cost if skipped:** two unused fields per stat, if levels never come. That is the whole downside,
and it is why this goes first despite doing nothing on its own.

## 2. Vitals

**Depends on:** Stats.

Health: a maximum from the definition, a current value in instance state, damage and healing, and a
death event.

**Why second.** It is the smallest complete plugin — authored data, runtime behaviour, and persisted
state — so it is where the state-as-storage rule gets learned on something small enough to fix. Get
this wrong and the same mistake is in every plugin after it.

**Watch for:** health filling itself before a level source exists. Whoever adds a level source has to
tell health to look again, and health is the only thing that knows whether it was restored from a
save or freshly spawned. Do not let another plugin guess at that.

## 3. Save

**Depends on:** the core only.

Write every entity's instance state to disk and read it back. No game concepts, and no format opinion
imposed on plugins — each state is an opaque blob to the save system.

**Why this early, before there is much to save.** This is the expensive-to-retrofit one. Every plugin
built after it inherits "does this survive a save?" as a question asked while writing, which costs
nothing. Every plugin built before it gets audited for persistence afterwards, which costs a day per
plugin and misses things — the misses being the ones that only appear a session later.

**Three things to get right at the start, because they are painful to change afterwards:**

- **Write atomically.** Write a temporary file, then replace. A crash mid-write must not leave a
  player with no save at all.
- **Carry forward state you cannot read.** A blob belonging to an uninstalled plugin must survive a
  load-and-save cycle untouched. Dropping it means uninstalling a plugin quietly destroys its data.
- **Key state by something permanent.** Not a type name, not a list index.

## 4. Input, then Locomotion

**Input depends on:** nothing but Unity's input system. **Locomotion depends on:** the core.

A device abstraction, then something that moves.

**Why here.** Nothing after this can be tested by playing it until something can walk. Every hour
spent building plugins you can only inspect in the inspector is an hour of untested work.

**Scope warning.** Input grows without limit — action maps, contexts, rebinding, device switching.
Build the abstraction and stop. Write down what was deferred, so the deferral is visible rather than
implied by silence.

## 5. Animation

**Depends on:** the core.

Playing clips on the thing that moves.

**Why here and not earlier.** It is the first plugin where the framework meets real content, and real
content is where assumptions die. Do it once locomotion gives it something to react to.

**The lesson that cost the most:** when blending into a new clip, reconstruct what is currently
playing at its matching speed *and* its matching normalised time. A blend that starts the new layer
at time zero looks perfect on half the inputs and doubles a step on the other half, depending on
which foot was forward. No measurement finds that. Only pressing the key at the wrong moment does.

## 6. Inventory

**Depends on:** the core.

Slots, capacity, stacking, starting contents.

**The one rule that matters:** starting contents reference *definitions*, and instances are made from
them at runtime. Authoring instances into a definition is precisely what broke the previous project.

**Why here.** It is the first collection, and collections are where identity gets tested properly —
two swords are one definition and two objects, and everything that outlives a session is full of
instances rather than definitions.

## 7. Equipment, Currency, Crafting

**Depend on:** Inventory.

Layered on the collection rather than beside it.

**Watch for:** a purse is an inventory. The moment a state type gains a subclass, every lookup by
type becomes ambiguous and returns whichever one the module order happened to put first. Ask by key
from the moment a second one exists.

## 8. Combat

**Depends on:** Vitals, Stats.

Damage arriving at health.

**Consider adopting rather than writing.** If a damage resolver already exists and has been proven,
bridge it — see the Combat Bridge sample. A resolver is all edge cases, and edge cases are exactly
what "already proven" means.

**The first bug will be friendly fire.** A swing finds everything with health inside its arc, which
is correct for a sword and disastrous for a pack of monsters converging on a player. Decide what may
damage what before authoring anything that fights.

## 9. Death

**Depends on:** Vitals, Locomotion.

**Why it is worth calling out at all:** it is a bridge. Vitals raises a death event, locomotion stops
moving, and neither plugin knows the other exists — a third assembly joins them. That shape is how
everything cross-cutting should be built afterwards, so build it consciously and keep it as the
reference.

## 10. AI, then Spawning

**Depend on:** Vitals, Locomotion, and each other.

Something that decides, then something that keeps the world stocked with them.

**Spawning is where the save question comes back.** An entity placed in a scene has an authored
identity; one created at runtime does not. Decide what a save is supposed to do about a respawning
monster *before* building the spawner, not after the first save comes back missing it.

Create entities through the core's `GameEntity.Create`, never by adding the component and assigning
the definition afterwards. The component resolves everything it needs the instant it exists.

## 11. Progression

**Depends on:** Vitals, Stats.

Levels, and everything that scales with them.

**Why this late.** It is the first plugin that changes numbers other plugins have already cached, so
it needs those plugins to exist to be tested against. Anything holding a cached lookup has to be told
to look again — and half the stats responding to a level-up reads as a balance problem rather than a
bug, which is the worst way for a fault to present.

## 12. Quests

**Depends on:** the core, plus an optional half per subject.

**Why nearly last.** Quests observe everything else, so everything else has to exist. Build the log
against the core alone, and put each subject — kill this, collect that — in its own assembly that
installs separately. Neither half requires the other, and a game that wants collection quests without
combat should not have to install combat.

**Decide who counts.** The killer counts its own kills; the victim is never told a quest exists.
Getting that backwards puts quest knowledge into every corpse in the game.

## 13. UI

**Depends on:** everything it displays.

**Last, deliberately.** UI reads from every plugin, so building it early means rebuilding it after
every plugin. It is also the one place where "it works" and "it looks right" genuinely differ, and
that difference is Graves' call rather than a measurement.

---

## Two shapes to reach for throughout

**A bridge, when two plugins need to know about each other.** A third assembly that references both.
Neither of the two learns about the other, and when one is replaced only the bridge dies.

**An adapter, when something already works.** A proven tool keeps its own assembly and knows nothing
about this framework; one new assembly holds one module and one component. Do not rewrite a tool that
has survived real use — the edge cases it already handles cannot be re-derived from a description of
what it does.
