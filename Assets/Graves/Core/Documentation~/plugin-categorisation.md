# Plugin categorisation

**Status: working document, for discussion. Not a directive and not decided.**

The question it exists to answer: how plugins are grouped, and what the minimum set is before the
framework can be said to make a game.

---

## Graves' input

<!-- Paste below. Anything here is yours, unedited - I will not rewrite it, only respond underneath. -->


1. Bootstrap & Lifecycle
App entry point, persistent root object, service registration (locator or DI), deterministic init order, and a top-level game state machine (Boot → Menu → Loading → Gameplay → Pause). Everything else hangs off this.

2. Scene & World
Async/additive scene loading, scene descriptors as ScriptableObjects (what to load, what persists), spawn points, transition handling, optional streaming. Owns the "loading screen" contract.

3. Data & Definitions
Your SO layer. Definitions (ItemDefinition, AbilityDefinition, ActorDefinition) vs runtime instances (ItemInstance with stack count, durability). Registries/databases keyed by stable IDs, Addressable references. This is the spine of an SO-based RPG, so it deserves its own module rather than being smeared across gameplay.

4. Events & Messaging
Event bus or SO event channels. This is what lets UI, audio, and quests react to gameplay without hard references.

5. Input
Abstraction over the Input System: action maps per context (gameplay / UI / dialogue), context switching, rebinding, device change handling, input buffering for combat. Nothing above this layer reads raw devices.

6. Actors (Entities)
Player, NPC, interactable props — all the same thing: an Actor root with composed components (Health, Stats, Inventory, Interactable, Faction). Built from ActorDefinition via a factory, pooled where appropriate. "Player" is just an Actor with a particular brain attached.

7. Controllers (Brains) and Motors
The split that matters most for an RPG: Controller produces intent (move vector, look direction, action requests), Motor turns intent into movement (CharacterController or Rigidbody). PlayerController is input → intent; AIController is behaviour tree → intent. Same motor for both, so NPCs and players share locomotion, and it's netcode-friendly since intent is what you replicate.

8. Animation Bridge
Drives Animator parameters from motor/combat state, routes animation events back to gameplay, IK, and modular character assembly (mesh swapping, skinned attachments — your Blender skin mesh work sits here).

9. Camera
A camera system with modes (follow, orbit, target-lock, dialogue, cinematic), priority/blend rules, driven by game state and gameplay events. Cinemachine underneath is fine; the framework owns the mode switching.

10. Gameplay Systems
The RPG content systems, each data-driven from layer 3: Stats/Attributes and modifiers, Damage & Health, Abilities and cooldowns, Status Effects, Inventory/Equipment, Interaction, Quests, Dialogue, Loot, Progression. Each is a service plus per-actor components.

11. AI
Perception (sight/hearing), decision (BT/FSM/utility), NavMesh movement via the motor, encounter/spawn management.

12. UI
Screen/panel stack with push/pop and modal support, view-models bound to gameplay data through events (not polling), HUD vs menu vs world-space, and input-mode handoff with layer 5.

13. Audio & Feedback
Audio cues as SOs, mixer routing, music state tied to game state; VFX/feedback pooled and triggered from events.

14. Persistence
ISaveable contract, save slots, serialization of runtime instances by ID (never by asset reference), versioning/migration.

15. Networking (optional seam)
Authority, replication of intent and state. If you keep the controller/motor split and event-driven UI, this slots in later without a rewrite.

16. Tooling & Utilities
Validation for SO databases, debug console/cheats, custom inspectors, plus the generic stuff: object pooling, timers, async helpers, extension methods.


---

## Discussion

### A measurement worth keeping against the sixteen layers

**Only five types in the whole of the commercial MMORPG framework studied as a reference derive from `ScriptableObject`:** item, skill, quest,
recipe, item mall.

A framework sold commercially for years on the strength of its data-driven design has a backend of
five authored types. That is a measurement rather than an opinion, and it is the strongest available
argument against building sixteen categories because a map has sixteen headings.

### The risk to name first

**Built in full, this list is the framework this project reacts against.**

Sixteen layers, every one of them reasonable, is exactly how you get *"it's good, it has a lot of
features, it's not very user friendly, and the amount of features it does have also makes it horribly
complicated."* That is the thing this project exists in reaction to, and nothing in the list is wrong
— the danger is entirely in treating it as a backlog rather than a map.

Used as a map it is excellent: it says where a thing goes when it turns up, which is precisely what
stops a framework smearing concerns across itself. Used as a backlog it produces sixteen half-built
systems and no game.

So the useful question is not "which of these do we build" but **"which of these can we not add later
without a rewrite"**. Only those need deciding now. Everything else is allowed to not exist.

### Your read on the shape is right

Core owns the beginning and the end — bootstrap and definitions at one end, tooling and validation at
the other. The middle is yours.

Worth stating that way explicitly, because it is a good test for future decisions:

> **The core is the bookends. The plugins are the book.**

That also explains why 3 and 16 already feel core-owned while 6 through 13 obviously are not: the
bookends are about *structure and integrity*, the middle is about *what your game happens to be*.

### Two genuine gaps this exposes in the core

Both are structural, both pass the "does this make plugin authoring easier for the next person" test,
and both are expensive to add once plugins are written against their absence.

**1. Runtime instances, from layer 3.** The core has definitions and no concept of an instance. Your
line — *"Definitions vs runtime instances (ItemInstance with stack count, durability)"* — is the gap.
Two swords are the same definition and different objects: different durability, different enchants,
one in a chest and one equipped.

This is not a nice-to-have. It is the exact shape of the failure on the previous project, where
authored inventory content held per-instance state whose identity drifted from the database. The core
already owns identity; instances are where identity actually gets tested, and layer 14's *"serialize
runtime instances by ID, never by asset reference"* is unbuildable until instances exist.

**2. Events and messaging, from layer 4.** Currently a plugin that needs another plugin has to
reference it — and the moment that happens, the one-way dependency rule that holds between core and
plugins stops holding *between* plugins, quietly, with no compile error to catch it.

Layer 4 is the mechanism that prevents it, which makes it core rather than a plugin. It is the single
highest-value addition on this list, and the one most expensive to retrofit, because every plugin
written before it exists will have solved the problem some other way.

Not to be built speculatively — built the first time two plugins genuinely need to talk, which is
probably persistence asking everything to save.

### Where I would push back

**Layer 16's grab bag.** *"Object pooling, timers, async helpers, extension methods"* is where
frameworks go to die. A utilities module accretes forever because nothing is ever obviously out of
scope for it. Let each utility live with the one thing that needs it until a second thing needs it
too. Validation and the debug console are real and belong in core tooling; the grab bag is not the
same kind of thing and should not share a heading with them.

**Layer 15's "slots in later without a rewrite."** The controller/motor split genuinely helps, and
replicating intent is the right instinct. But netcode also changes *who owns state* and *when state
is allowed to change*, and no amount of clean layering makes that free. Better to say: this split
makes networking possible later rather than painless later. Decide deliberately whether it is ever
happening, because designing for it costs something real in the meantime.

**Layer 9's Cinemachine.** First-party Unity package, so it passes the letter of the no-asset-packs
rule. Fine inside a camera *plugin*. It must never appear in the core, and a game that installs the
camera plugin then inherits it — which is the reason the plugin split exists, so this is working as
intended rather than a problem.

**Layer 6 quietly argues against something in the core.** *"Player is just an Actor with a particular
brain attached"* is exactly right, and it says player-ness comes from the controller, not the data.
`EntityDefinition.Kind` currently says otherwise. I have flagged `Kind` as removable twice; this is
the strongest argument yet for removing it, and it comes from your own model.

### Build order is not dependency order

Two entries sit in the wrong place for building, though they are in the right place for depending:

- **Layer 4 (events)** is listed fourth and is used by almost everything above it. Built late, every
  earlier system gets rewritten against it.
- **Layer 14 (persistence)** is listed fourteenth, but every system needs to know *how it saves* from
  the day it is written. This is why the core already ships the `ISaveable` contract and deliberately
  no format — the contract must be early, the implementation can be late.

### The minimum standard for a game

The honest answer is not a count of layers. **A game is a loop:**

> the player acts → the world responds → the state persists → and it is worth doing again

Which is: **input → intent → a system that changes something → persistence.** Against your numbering
that is 5, 7, one of 10, and 14, on top of what already exists in 3 and 6.

Everything else on the list makes a game *better*, not *a game*. Camera, audio, UI polish, AI,
networking, quests — real work, all of it optional to the definition. And the loop above is roughly
five plugins, not sixteen.

That is the number worth aiming at, because five plugins is also the honest test of whether the
plugin contract holds. If plugin five costs what plugin one cost, this framework works.

### Smoke test: does the plugin that exists fit a category?

Graves' test, and the answer is **no — not cleanly.** Which is worth more than a yes would have been.

`Graves.Plugin.Animation` looks like it belongs in **8. Animation Bridge**. Read what layer 8
actually says it does:

> Drives Animator parameters from motor/combat state, routes animation events back to gameplay, IK,
> and modular character assembly.

The plugin does **none** of those four things. It sets no Animator parameters. It routes no animation
events — it silences them. No IK. No mesh swapping.

What it does is take the Animator's output away and drive the pose from a `PlayableGraph`. A bridge
*connects* gameplay to Mecanim's state machine. This **circumvents** the state machine, and that is
the entire point of it: an ability clip never has to exist as a state in a controller.

So the failure is not a naming quibble. **Layer 8 conflates two different jobs**, and it does so by
quietly assuming Mecanim is how animation gets played:

- **8a. Animation playback** — *how a clip gets on screen at all.* Mecanim controllers, a Playables
  runner, Animancer. A project picks exactly one, and it is a mechanism, not a policy.
- **8b. Animation bridge** — *how gameplay state becomes an animation request.* "I am moving at 4m/s,
  I am attacking, I have landed." Depends on 8a and knows nothing about how it works.

The existing plugin is squarely **8a**. A combat plugin would supply **8b** and would talk to 8a
through named requests — `Play("Jump")` — rather than through Animator parameters.

That distinction earns its place: it is what allows the playback mechanism to be swapped without
touching gameplay, which is exactly the swap the previous project could not make.

**The test caught a second thing too.** The plugin ships two assemblies:
`Graves.Plugin.Animation` (8a) and `Graves.Plugin.Animation.Demo`, which reads the keyboard — that is
**layer 5, Input**, not animation at all. It was split into its own assembly for dependency reasons
before this list existed, and the categorisation independently agrees with the split. Two different
arguments landing on the same boundary is decent evidence the boundary is real.

**Conclusion:** the categorisation is doing its job. A map that cannot place the one thing you have
built is not yet a map — and rather than filing the plugin under the nearest heading, the heading
needed splitting.

### A plugin may span categories — but every part must be nameable

Graves' correction, and it is the right one: **one plugin, one category was never the rule.** A plugin
is allowed to be more than one thing. The existing one already is — 8a playback plus a slice of
5 Input — and that is fine rather than a smell.

So the smoke test needed restating. It is not *"which single category is this?"* It is:

> **Can every part of this plugin be named?**

That version keeps the diagnostic power while allowing the obvious reality that useful plugins
straddle layers. And it still fails loudly in the two cases worth catching:

- **A part with no name at all** — either the map is missing an entry (which is what happened with
  8a, and the map changed), or that part is doing two jobs braided together and should be pulled
  apart.
- **A part whose name is a layer the plugin has no business being in** — the signal that something
  has quietly grown a second responsibility.

**The practical rule that makes "more than one thing" honest rather than a mush:** where a plugin
spans categories, give each part its own assembly **when it carries a dependency the rest should not
inherit, or when it is useful installed on its own.**

*(Corrected 2026-09-05. This originally said "give each category its own assembly", which promoted
the wrong argument to a rule. The animation demo was split because it needed the Input System — a
dependency reason — and the categorisation agreed afterwards. Two arguments landing on one boundary
is evidence the boundary is real; it is not licence to promote the confirming one. Category is a
judgement, dependency is a fact about the build.*

*The consequence, stated because it was missed the first time: **category now has no structural
consequence.** Two parts in different categories, with the same dependencies and no standalone use,
are one assembly. This document is a vocabulary, not a rule-giver.)* Then the spanning is visible in the build
rather than living in someone's head, the dependency direction between the parts is enforced by the
compiler, and anyone can take the part they want without the rest.

The animation plugin already looks like this, and not by foresight — the input half was split off
because it was the only thing needing the Input System, and forcing that package on every future user
of the runner was unacceptable. The dependency argument and the categorisation argument reached the
same boundary independently. That is the outcome to aim for: **if the categories are real, they
should keep agreeing with conclusions reached for entirely unrelated reasons.**

### A prediction that turned out wrong

Recorded because a wrong prediction is worth more to the next reader than a quiet correction.

The argument for building instances before events ran: instances stand alone, events have nothing to
carry until something exists worth talking about, and **persistence would be the first genuine case
of two plugins needing to talk** — so events would earn their existence there.

The first half held. The last clause did not. Saving needs no messaging at all: it finds `ISaveable`
by scanning components and reads instance state directly. **Events were still unearned after two
plugins**, and after twenty they were still unearned - every case that looked like it wanted a bus
turned out to want a local typed event or a bridge assembly.

**They are earned now, and it was outside evidence that did it rather than anything in this
repository.** Three things, none of them a prediction:

- A commercial framework's community plugins advertise *"fully event-driven, zero coupling to any
  specific gameplay system"* as their headline feature. Three separate third-party plugins say a
  version of it. The bus is what let strangers extend a framework they could not edit.
- A long-running extension ecosystem for a different asset runs on string-named global events, and
  its extensions are one file each. The cost of that design is real - a typo in an event name fails
  silently, forever - and it is a cost of the *string*, not of the bus.
- The bus that framework ships is typed, which answers every objection recorded here, all of which
  were to the untyped kind.

**What changed is the question.** "Do two of our plugins need to talk?" - still no. "Can somebody who
cannot edit this framework extend it?" - not without one. The second question only became real when
this stopped being a thing being built and started being a thing that could be published.

Recorded this way round on purpose: three refusals and then an acceptance, with the evidence that
turned it. A rule that never reverses is not a rule, it is a preference that got lucky.

### The governing principle

**Do not make the framework fit the definition. Make the definition fit the framework.**

With one caveat, so the map is not toothless: the definition still gets a vote. When it cannot
describe something that was built, usually the map is wrong — as it was for layer 8. But sometimes
the thing is wrong, and a part that resists naming *because it is doing two jobs at once* is the map
earning its keep. The distinction is whether the unnameable part is coherent. 8a was coherent and had
no name. A braided part is not coherent, and needs splitting rather than a new heading.

