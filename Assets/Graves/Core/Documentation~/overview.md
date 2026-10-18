# Graves Plugin Builder

**A Unity framework for building games out of data and plugins.**

Define your player, monsters, items, skills, quests and recipes as ScriptableObjects, author them in
one window, and add behaviour by installing plugins. The core knows nothing about 2D or 3D,
controllers, movement, physics or rendering — and never will.

Native C#. No asset-pack dependencies. Unity 6000.3 or later.

---

## What you get

**A core that owns four things and nothing else:** identity, structure, extension and integrity.
Everything a game *does* lives in a plugin you chose to install.

**One authoring window.** Every kind of definition in the project, in tabs, with search. Create,
duplicate, delete, reorder. A plugin's fields draw themselves — adding a field costs no editor code,
and a plugin can supply a custom drawer that will be picked up automatically.

**A manual in the tool.** Every document in the project, rendered and browsable, one click from the
thing you are authoring.

**Save that survives you changing the game.** State from a plugin you have uninstalled is carried
forward rather than destroyed, so removing a plugin and putting it back returns the data intact.

**Integrity checks that report rather than repair.** A deleted definition, a duplicated identity, two
plugins claiming one state key, an item whose definition is gone — each is named, and none is quietly
fixed.

---

## The shape of it

```
Definition          what a thing IS          authored once, permanent uid
   └── Modules      data plugins attach      the extension point
Instance            a PARTICULAR thing       its own identity, its own state
   └── State        per-object data          durability, health, contents
```

A definition is a kind of thing. An instance is one of them. Two swords share a definition and differ
by their state — which is what lets one be enchanted and the other not.

**Nothing is ever keyed by name, index or object reference.** Rename an asset, reorder a list, move a
folder: saves survive all of it.

---

## Authoring

1. Open **Graves → Authoring Window**
2. Pick a tab — Entities, Items, Skills, Quests, Recipes, Equipment Slots, Crafting Stations
3. **New**, name it, and add modules
4. Each module is a plugin's data. Order matters and the window says so
5. **Validate** at any time. **Report** copies a full diagnostic to the clipboard

Put a `GameEntity` in a scene, give it a definition, press play. It spawns a body, attaches its
modules and gets out of the way.

---

## Plugins

Twenty, in twenty-eight assemblies. Install what you want; nothing depends on anything it does not
name.

| | |
|---|---|
| **Input** | Devices to typed intent. Nothing above it reads a keyboard |
| **Locomotion** | Intent to movement. Player brain and AI brain drive the same motor |
| **Camera** | Follow and orbit |
| **Vitals** | Health, damage, death |
| **Combat** | Attacks, arcs, reach, damage filtering |
| **AI** | Perception, chasing, attacking |
| **Death** | What happens to a body. Depends on vitals and locomotion so neither depends on the other |
| **Stats** | Level-scaled values. Depends on nothing at all |
| **Progression** | Experience, levels, growth |
| **Skills** | Abilities as definitions, with timing, requirements, effects and animation |
| **Equipment** | Slots, equipping, stat bonuses |
| **Inventory** | Positional slots, stacking that cannot merge two distinguishable things |
| **Currency** | Gold, as a purse |
| **Crafting** | Recipes and stations |
| **Enhancement** | Upgrading a particular object |
| **Quests** | Objectives, tracking, rewards |
| **Spawning** | Restocking a world |
| **Lifecycle** | A session that can be won or lost |
| **Animation** | Named clips over a live Mecanim state, masked or full body |
| **Save** | Slots, atomic writes, versioning |
| **UI** | Windows, health bars |

---

## Writing your own

Four things, and **only the first is required**:

1. An assembly definition
2. A `DefinitionModule` subclass — your data
3. An `AttachTo(GameEntity)` override, if you need behaviour
4. A `[DefinitionCategory]` attribute, only for a whole new kind of definition

No registration, no manifest, no interface to implement. The core calls `AttachTo` once and leaves
you alone — **there is no core update loop and never will be.**

Plugins may depend on plugins, one direction only, and Unity refuses to compile a cycle so nobody has
to police it. Dependencies are what keep a plugin concise: a plugin that may not reference what it
needs has to reimplement it, and is then two things.

See `Docs/writing-a-plugin.md`.

---

## What it deliberately does not do

**No opinion on 3D or 2D.** No controller, no movement model, no physics, no rendering. The core
references `UnityEngine.CoreModule` and nothing else.

**No save format in the core.** Only the contract. Where the bytes go is a plugin's decision.

**No event bus.** Components raise typed events and neighbours subscribe. A global message broker has
been predicted three times and earned zero — every case turned out to be one hop between things that
already knew each other.

**No content.** No models, no clips, no prefabs. The core ships a slot and you fill it.

**No networking.** Single-player by design.

---

## Requirements

Unity **6000.3+**. Universal Render Pipeline for the sample content. The Input System package, only
if you install the input plugin.

Nothing else. No paid assets, no third-party libraries.

---

## Honest limits

Written down because a framework that hides these costs you a day each.

- **Packaging is designed and unbuilt.** `package.json` exists; the `Samples~` layout does not.
  Installing today means a path dependency plus copying plugin folders by hand.
- **Runtime-spawned entities do not survive a save.** Persistent ids are minted in the editor;
  something spawned at play time has none.
- **A stack has one identity for many objects.** Twenty arrows are one instance with a count, so
  enhancing "one arrow" enhances all twenty. There is no split operation yet.
- **Only a killing blow through an `Attacker` counts** for quests. Nothing that drowns, burns or falls
  is claimed by anyone.
- **The authoring window shows a raw module list alongside the good one**, because the detail panel
  draws the whole inspector.

---

## Where it came from

Built in two days by two AI agents working from the same repository and deciding independently, with
one human operator and a third read-only agent checking both. Every architectural decision is
recorded with its reasoning, its alternatives, and — where one was made — a prediction that was later
scored.

That record is in `Agent Memory/` and `Docs/`, and it is browsable from the Manual tab. It is not
history for its own sake: several of the design choices here exist because a *previous* project made
the other one and lost data.
