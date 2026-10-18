# User stories

**The foundation. Every scope decision is checked against this document.**

Given by Graves on 2026-09-04, deliberately without implementation guidance:

> *"The reason why I'm not telling you how to code this is because I have some good ideas. I have
> some bad ideas. Architecting an entire framework is not something that I've done before, so it
> would be unfair of me to influence your decisions."*

Recorded here on 2026-09-05, because the reviewer's first pass found they existed only in an agent's
memory (`Docs/reviewer/questions.md`, Q-001). The charter tells the reviewer to check scope against
them; it correctly reported it could not, because they were not in the repository. Anything living
only in a session dies with that session.

---

## The five stories, as given

**1. Definitions as ScriptableObjects**

> "I want to be able to define my player, the NPCs, the enemies, and the items that exist within my
> game through the use of scriptable objects."

**2. A user-friendly editor**

> "I want to use a user friendly editor interface to be able to create the scriptable objects and set
> the properties of them, or to edit or delete them."

**3. The core defines nothing about presentation or physicality**

> "The core framework has no definition in shapes of whether it's 3D, 2D, the type of controller, the
> type of movement, or any physical interactions with the scene."

**4. A plugin system with a one-way dependency**

> "The core will have a plugin system... the plugins' functionality will allow the core dataset to be
> extended... At no time will the core be dependent on any of these plugins. However, the reverse is
> not true."

**5. Everything else is a plugin, and it is all native**

> "The core will allow things such as the player controller, the animations, weapon sets, the player
> prefab, the scene, the save system to all be plugged into it through separate plugins... All code
> must be native C#. It must not have external dependencies. Unity built-in dependencies are fine,
> but dependencies on an asset pack is not."

## What was said about the project itself

It is an experiment in working method as much as a framework:

> *"Yes, and the biggest thing, bigger than this project, is me understanding the best way you can
> help me."*

And the framework is a reaction to a specific failure, a commercial RPG framework:

> *"It's good, it has a lot of features, it's not very user friendly, and the amount of features it
> does have also makes it horribly complicated."*

**That sentence is the scope test.** More features is the failure mode, not the goal.

## Latitude explicitly given

On where the core/plugin line falls:

> *"I don't want you to feel that you have no choice over whether that's a plugin or part of the core,
> because you are building this core for other Claudes to build plugins for, and we don't want to make
> things hard for them."*

So the test for any core-versus-plugin decision is: **does this make plugin authoring easier for the
next person?** If yes it may belong in the core. If it is a decision a particular game would make, it
is a plugin.

## Derived, not given

These were reasoned from the stories and agreed in conversation. They are recorded separately because
they carry less authority than the five above — a future agent may overturn them with a good argument,
which is not true of the stories.

- **The line:** the core owns identity, structure, extension and integrity. Plugins own behaviour and
  presentation. Or: **the core is the bookends, the plugins are the book.**
- **The core ships a slot for a default representation and no content**, so a plugin can be tested
  before any representation plugin exists. Graves fills the slot.
- **Boundaries are compiler-enforced by asmdef**, not convention.
- **The editor renders a definition's own inspector**, so a plugin gets working editor UI for free and
  adding a field costs no editor code.
- **The plugin authoring guide is a deliverable**, not documentation-after. See
  `Docs/writing-a-plugin.md`.

## The minimum standard for a game

Not a count of layers. A loop:

> the player acts → the world responds → the state persists → and it is worth doing again

Roughly five plugins, not sixteen. See `Docs/plugin-categorisation.md`.

## Emergent stories — captured 2026-09-05, awaiting confirmation

Graves' observation, and the reason this section exists:

> *"There's been a lot of organic user stories come from me this session that might not necessarily
> have been recognised as a user story or captured as a user story."*

**These are the building agent's reading of things Graves said while working**, not stories he sat
down and wrote. Each carries his words so the interpretation can be checked against the source. They
are marked pending until he confirms, corrects or rejects them — an agent's paraphrase of an operator
is exactly the kind of claim this project has learned not to trust.

### E-1 — Competing plugins for one category *(pending)*

> *"Maybe we'll have more than one type of inventory plugin to pick from and you pick one."*

A category is a slot; a plugin is a candidate for it. Some slots take one occupant, some take many,
and that is a property of the category rather than the plugin.

Already acted on: two plugins claiming one `StateKey` silently corrupt saves, so `ValidateStateKeys`
now reports it. See `Docs/plugin-categorisation.md`.

### E-2 — A plugin may be more than one thing *(pending)*

> *"A plugin is allowed to be more than one thing. So if there's, say, two definitions that clearly
> fit where you just made, that works as well."*

The smoke test became "can every part of this plugin be named?" rather than "which single category is
this?". Where a plugin spans categories, each category gets its own assembly so the spanning is
visible in the build.

### E-3 — Installed by choice, not by default *(pending)*

> *"Copying out the whole thing and having all the plugins initially uninstalled, and you pick what
> you want to install, and it's already in the assets folder."*

One package. Nothing installs itself. An installed plugin lands in `Assets` as editable source, not
read-only in a package cache. See `Docs/packaging.md`.

### E-4 — Consumable from a separate project *(pending)*

> *"If I'm going to do any serious testing with this, I'm going to have to take it to another Unity
> project."*

The framework must be usable from a project that has none of the development tooling installed, and a
bug found there must be reportable without the agent being able to run anything. Produced the `file:`
package reference and the diagnostic report.

### E-5 — The framework repository stays clean *(pending)*

> *"I really don't want to adulterate the [project] where the core is being built."*

Test content is unavoidable — without it nothing is ever run — so it is quarantined rather than
banned, in its own tree, with its own assembly, and it never ships. See `Docs/test-bed-policy.md`.

### E-6 — Windows that behave *(pending, and not built)*

> *"What I liked... is the window manager, the opening and closing and remembering your position. It
> was easy to drag around. It was easy to drag items between the various slots. It felt simple, but I
> know it's not."*

A future UI plugin requirement, recorded now so it is not lost. Directly caused one framework change
already: inventory slots have positions, because dragging between slots is meaningless without them.

### E-7 — Extract technique, never transplant code *(confirmed by repetition)*

> *"Not a line goes to your clipboard."*

Reference material lives outside the repository so vendor source cannot be committed by accident.
Read it, understand the mechanism, write it fresh, and write the extraction note before building.

### E-8 — Saves must survive the game changing under them *(carried, drove the design)*

> *"Imagine updating an item in game six months after release where players have established saves."*

Said of the previous project, but it is the constraint most of this framework's identity work exists
to satisfy. State from an uninstalled plugin is carried forward rather than dropped; a deleted
definition is reported rather than substituted; nothing is keyed by name, index or object reference.

## How to use this document

When judging whether something belongs, ask in this order:

1. Does a story ask for it?
2. If not, does it make plugin authoring easier for the next person?
3. If not, is it a decision a particular game would make? Then it is a plugin, not core.
4. If none of the above — it is scope creep, and the framework this reacts against failed by having
   too many features rather than too few.
