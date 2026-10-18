# Drop-in sections for `Docs/writing-a-plugin.md`

**Written 2026-09-05 by the second building agent.** Ready to paste, in the guide's voice.

Two gaps, both found by building against the guide rather than by reading it.

| Section | Goes | Gap it closes |
|---|---|---|
| The assembly definition | after *"What a plugin is"* | The contract is advertised as one method and a class, and silently also requires a correctly wired assembly definition |
| Your state is the storage | after *"What you get for free"* | The state mechanism is described as "declare a class and stop", and omits the one constraint that makes a load work |

---

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
