# Writing a plugin

For whoever picks this up next — most likely another Claude, working alone with Graves.

Read this before writing code. It is short on purpose. Everything in it was verified in the editor
rather than reasoned about, and where something is untested it says so.

---

## The line

**The core owns identity, structure, extension and integrity. Plugins own behaviour and
presentation.**

The core defines nothing about 2D or 3D, controllers, movement, or physical interaction, and it
never depends on a plugin. That is not a convention you are asked to respect — it is enforced by
assembly definitions, and you will get a compile error if you break it.

Ask one question when you are unsure where something belongs: *does this make plugin authoring
easier for the next person?* If yes, it may belong in the core. If it is a decision a particular
game would make, it is a plugin.

## What a plugin is

Four things, and only the first is required.

1. An **asmdef**. See the next section — it is the part that goes wrong.
2. A **`DefinitionModule` subclass** — your data. It becomes a sub-asset of the definition.
3. An override of **`AttachTo(GameEntity)`** if you need runtime behaviour.
4. A **`[DefinitionCategory]`** attribute, only if you are adding a whole new *kind* of definition
   rather than extending an existing one.

That is the entire contract. There is no registration call, no manifest, no interface to implement.

**"Only the first is required" is meant literally.** Two plugins exist and they are shaped nothing
alike:

- **Animation** is data-shaped. A module holds named clips, `AttachTo` puts a runner on the entity,
  and the runner owns its own frame loop.
- **Save** is service-shaped. It has no module, no attribute, and never touches a definition. It
  operates across everything at once and has no per-definition settings to author.

Both are plugins by every rule that matters — native C#, an assembly, and deletable without breaking
anything. If what you are building is a service rather than data, do not invent a module for it just
to look like a plugin.

**Plugins may depend on plugins.** One direction only, and Unity refuses to compile a cycle, so the
compiler enforces the direction without anyone checking. An earlier version of this guide said an
asmdef references "Graves.Core and Unity modules, nothing else", which read as a prohibition and was
wrong: 25 plugin assemblies now reference another plugin.

**Dependencies are what keep a plugin concise.** A plugin that may not reference what it needs has to
reimplement it, and is then two things. Forbidding dependencies does not produce clean plugins — it
produces bloated ones, or it pushes the shared part into the core, which is the outcome this design
exists to prevent.

The judgement that remains is not about direction. It is about surprise:

> **Would someone installing this plugin be surprised to find the other one required? Where the
> answer is yes, split.**

**The bridge pattern.** Where two plugins should not know about each other, a third can own the join
and depend on both. `Death` depends on vitals and locomotion so that neither depends on the other;
delete it and both still work. That concentrates the coupling in one optional, deletable place rather
than removing it — which is the trade, and it is worth making.

## The assembly definition, which is the part of the contract nobody mentions

Item one of the four above is the only one that is always required, and it is also the only one that
can go wrong in a way that wastes an hour.

A plugin needs a `.asmdef` beside its scripts. Copy this and change the two names:

```json
{
    "name": "Graves.Plugin.YourThing",
    "rootNamespace": "Graves.Plugins.YourThing",
    "references": [
        "Graves.Core"
    ]
}
```

`name` is what other assemblies reference. `rootNamespace` is what Unity puts at the top of a new
script. They differ by one letter — `Plugin` singular in the assembly name, `Plugins` plural in the
namespace — because that is what the existing plugins do, and consistency is worth more here than
either choice.

**A missing reference does not say "missing reference".** It says the *type* cannot be found, so
`GameEntity` reads as though the core is broken or the class was deleted. The error points at your
file and the fault is in a `.json` you were not looking at. If a core type will not resolve and you
are certain it exists, check the references array before you check anything else.

**Unity drops a reference you do not use.** List `Graves.Core` and never touch a core type, and the
compiled assembly will not reference it. This is correct behaviour and it means the `.asmdef` is a
request rather than a description — see the reflection check below, which is the truth.

### When to split into more than one assembly

Plugins here span categories, and where they do, each part gets its own assembly. The trigger for
splitting is not the category though:

> **Split when a part carries a dependency the rest should not inherit, or when a part is useful
> installed on its own. Otherwise keep it in one.**

The animation plugin is the worked example, and it was split before anyone wrote a rule down. Its
demo half reads the keyboard, which needs the Input System, and forcing that package on every user
of the clip runner was unacceptable. The split came from the dependency. The categorisation agreed
afterwards, from unrelated reasoning, which is the outcome to aim for.

The test, in one line: **delete the other assembly.** If what remains is still useful, or if merging
would force a package on somebody who does not want it, the split is real. If neither is true, one
assembly.

Splitting for tidiness costs something and buys nothing. Unity carries per-assembly overhead, and
every assembly is one more thing to wire correctly.

### Check the graph rather than reading the file

Assembly definitions are claims. The loaded domain is evidence, and the two have already disagreed
on this project — an editor reference appeared in the core's runtime assembly while every `.asmdef`
and every document said otherwise, and only running this found it:

```csharp
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
    if (a.GetName().Name.StartsWith("Graves"))
        // GetReferencedAssemblies() is the truth
```

What must hold, whatever the roster has grown to: the core references nothing, every plugin
references the core or another plugin, and nothing at all references the test bed.

### A new assembly may not appear until it is imported

Adding a `.asmdef` and asking Unity to recompile can report success while the new assembly is not in
the domain at all — verified on 2026-09-05, when three new assemblies compiled "cleanly" and were
absent from the reflection check. Import the `.asmdef` explicitly, then recompile, then check the
graph again. **A clean compile is not proof your assembly exists.**

---

## The one method

```csharp
public override void AttachTo(GameEntity _entity)
```

Called once per entity, during `Awake`, in author order. Then the core leaves you alone.

**The core does not tick and never will.** A frame loop is behaviour. If your module needs one, add
your own component to the entity here and own it from there — that is what the animation plugin
does, and it is why the core has no `Update`.

## What you get for free

**Editor UI.** The authoring window's detail panel is an `InspectorElement` bound to the selected
asset, so your fields render themselves. Adding a field to a module costs zero editor code. Do not
write a custom window; if you need custom drawing, write a `PropertyDrawer` and it will be picked up.

**Discovery.** Module types are found by reflection. Your module appears in the window's "Add module"
menu as soon as it compiles, and disappears when deleted. No core edit either way.

**Identity.** Definitions have a `Uid` minted once and never reassigned. Reference definitions by uid,
never by name and never by index. There is deliberately no numeric id: on a previous project recycled
numeric ids caused an old save to resolve to the *wrong item*, silently, carrying the player's
upgrades onto something else.

## Spawning an entity from script

`GameEntity.Create(definition, position, rotation, parent)`. That is the whole of it, and it is the
only supported way — do not add the component yourself.

```csharp
var entity = GameEntity.Create(_definition, where, transform.rotation);
if (entity == null) return;   // it said why
```

**The reason the method exists is an ordering trap.** A `GameEntity` resolves everything it needs in
`Awake`, and `Awake` runs the instant the component exists. Add the component first and the entity
has already woken, found no definition, complained and disabled itself — before you get a chance to
say what it is. The first plugin that spawned anything hit this, worked around it by setting the
private field with reflection, and left a comment saying that should not spread. It no longer needs
to.

`Create` sidesteps the trap by starting from something that is not yet awake: a prefab whose root is
saved inactive, or a bare inactive object when the project has assigned no prefab. Either way there
is a real window in which to assign the definition, and `Definition` is an ordinary settable property
inside it. **After waking, the setter refuses and says so** — an entity changing what it is midway
through its life would leave modules attached to a thing that no longer exists.

**Assign `CoreSettings.EntityPrefab`** when every spawned entity should start with something in
common: a layer, a tag, a rigidbody, a collider. **Its root object must be saved inactive**; the core
checks, refuses, and names the prefab rather than letting it fail as a mystery.

**A runtime entity has no persistent id.** `PersistentId` is authored, minted in the editor, and
`Create` deliberately clears whatever a prefab carried — otherwise every entity from that prefab
would claim to be the same one. It still gets a unique `Instance.InstanceId`, minted per run. What
that means for saving is a save plugin's problem, not the core's: a spawned thing is not the same
kind of thing as one placed in a scene, and the framework does not pretend otherwise.

## Your state is the storage, not a copy of it

`InstanceState` is where per-object state lives: durability on this sword, health on this goblin.
The guide above says to declare a serialisable class and stop, and that is true of the declaration.
There is one constraint on how you *use* it, and getting it wrong fails in a way nothing reports.

**Read and write through your state object. Never cache its values in a field.**

A save restores by overwriting your state object in place, so that a plugin holding a reference to
it does not end up writing to an orphan afterwards. Your object is the same object before and after
a load; only its contents change. That is what makes a load work with no notification, no callback
and nothing for you to subscribe to.

A component that keeps its own copy will:

- save perfectly, because the copy is pushed out whenever the value changes
- restore nothing, because the bytes land in an object nobody is reading
- be told it succeeded, in detail

That last point is the dangerous one. This exact bug produced:

```
loaded=True restored=6 missing=0 unresolved=0 problems=0
```

while the thing on screen kept the health it had before the load. The save file was correct on disk.
Every component read a consistent answer. **The only way to see it was to print the live value and
the stored value side by side**, which no ordinary caller ever does.

So, concretely:

```csharp
// wrong - saves fine, never loads
private int _current;
public int Current => _current;

// right - the state is where the number lives
private MyState _state;
public int Current => _state.Current;
```

Keep a fallback for the unbound case if your component should work with no instance and no save
present, which is worth doing - it lets the thing be tested with none of this framework running.

**Bind after you configure, not before.** A saved value gets clamped against your limits, and if the
limits have not been applied yet it is clamped against defaults. That produces a value that is wrong
and plausible, which is the worst kind.

**Distinguish "never set" from "set to zero".** A fresh instance and a corpse both hold zero health.
Without a flag saying the state has been written at least once, a load either revives the dead or
kills the newborn, depending on which way you guessed. One bool is cheaper than either.

## Rules that are not negotiable

**Native C# only.** Unity built-in modules are fine. Asset packs are not. This includes packages you
might reach for out of habit — if the core or a shipped plugin needs it present, it is a dependency.

**Extract the technique, never transplant the code.** Reference material lives in a folder
deliberately *outside* the repository so vendor source cannot be committed by accident. Read it,
understand the mechanism, write it fresh. Write an extraction note first and then work from the
note with the source closed.

Do not carry a source's workarounds across without asking what they were for. The animation runner
this plugin was studied against built a `PlayableGraph` per call and kept a list of graphs it had failed to clean up, plus a 0.1s
re-entrancy guard. Both existed to cover the per-call churn. One persistent graph removed the need
for either — confirmed by spamming the trigger, which is exactly the case the guard was there for.

**Announce every fallback.** A silent fallback is how a project ships the placeholder. The core logs
which representation step it took and why; do the same.

**Report, do not repair.** When something is wrong, say so loudly and leave the data alone. A missing
module is reported by name, not cleaned up.

## When the core changes

**Right now the core changes freely, and that is only because one agent owns every plugin.** Runtime
instances, an authored persistent id on `GameEntity`, a corrected `ISaveable` key contract, a replaced
inventory slot model — every one of those would have broken an outside plugin.

The moment someone else writes one, that stops. There is no version policy yet, and there needs to be
one before it is needed rather than after.

Why this is stated here rather than left implicit: a neighbouring project's addon ecosystem — 85
plugins, years of work — went through a discontinuity when its core changed shape, and **the author
who wrote them did not port them.** Porting was not porting; those plugins had to know so much about
the core that a shape change meant a rewrite of 477 files.

**The small contract is what makes a core change survivable.** One virtual method and a serializable
class. No SQL, no hook attribute, no compile symbol, no core method to inject into. Keep it that way,
and keep your plugin's knowledge of the core as thin as it will go — that thinness is what decides
whether your plugin survives the next change or gets left behind.

## Removing a plugin

Verified 2026-09-05 by actually removing the animation plugin:

- `Graves.Core` compiles with it gone, 0 errors
- the window still opens, still lists categories, and shows *"Missing module – its plugin is not
  installed"* in place of the module
- the registry is intact and definitions survive
- **the `.asset` file is byte-for-byte unchanged** — the authored data is still in it

**The danger is not removal, it is saving afterwards.** The data survives only while nobody writes
the definition back out; a save with an unresolvable module replaces the reference with nothing and
the loss is permanent. `DefinitionRegistry.Validate()` reports each one by name and says so.

Restoring the plugin does **not** rebind within the same session — the editor holds the broken
binding, and forcing a reimport does not shift it. **An editor restart restores everything**:
confirmed, zero validation problems, every module and every authored clip back, masks included.

So removal is fully reversible. Just do not be alarmed by what you see before the restart, and do not
try to "fix" it by re-saving — that is the one action that turns a recoverable state into a
permanent loss.

## Verifying your work

The single most useful habit here: **a return value is not proof.** Read the console after any editor
action, and clear it first so what is there is from this run.

Three bugs in the core and plugin surfaced only this way, none of which produced a failed return:
an exception inside `CreateGUI` that Unity swallows into a blank window, a serialized list element
arriving with zero-initialised fields instead of its declared defaults, and a scene object reference
saving as `fileID: 0` while the write reported success.

Check your assembly boundaries by reflection rather than by reading the asmdef:

```csharp
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
    if (a.GetName().Name.StartsWith("Graves"))
        // GetReferencedAssemblies() is the truth
```

That check caught `UnityEngine.PhysicsModule` being pulled into the core by the one line naming
`Collider` in order to *remove* one — a physics dependency inside the comment claiming there wasn't
any.

And the limit of all of it: **you can verify that something runs. You cannot verify that it looks
right.** The masked animation path passed every measurement and was still visibly broken — the legs
restarted their walk cycle, which Graves found by pressing the key on the wrong foot. Build the thing
so he can look at it, then ask.

## Tooling

See `Docs/tooling.md` — in this repository, deliberately, because this line used to point at an
agent's memory directory that no future reader inherits. Short version: the Unity CLI's `eval_file` for live state
with no domain reload, `recompile` + `recompile_status` for "did it build", UnitySkills for asset
operations, MCP last. **MCP cannot verify a compile** — it reads the pre-edit domain, so a type you
just wrote reads as missing.

Never recompile while Graves is in Play mode. Check `GetState` first, every time.
