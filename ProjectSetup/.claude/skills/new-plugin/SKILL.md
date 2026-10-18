---
name: new-plugin
description: Create a new Graves framework plugin - folder, assembly definition, module, instance state - with the decisions that have to be made before writing any of it. Use when starting any new plugin.
---

# Write a plugin

A plugin is a folder, an assembly definition, and at least one `DefinitionModule`. Everything else is
optional. It is worth being slow about the four decisions below, because each of them is expensive to
reverse once content has been authored against it.

## Decide these before writing anything

**1. What does this plugin depend on?** Plugins may depend on plugins — that is settled — but every
dependency is inherited by everyone who installs this one. A plugin that reaches for a second
plugin's type to answer one question usually wants a bridge instead.

**2. One assembly or several?** Split for a *dependency*, never for a category. If half of what you
are writing needs another plugin and half does not, that is two assemblies and the split is real. If
both halves need the same things, it is one assembly however different they look — Unity charges real
per-assembly overhead, and a split that buys nothing costs compile time forever.

**3. What is the authored data, and what is the runtime state?** The `DefinitionModule` says what a
*kind* of thing is; the `InstanceState` says what *this particular one* currently is. If a field
would be the same for every goblin, it is authored. If it differs per goblin or changes during play,
it is state.

**4. Does any of it need to survive a save?** If yes, it lives in an `InstanceState` and **the state
object is the storage, not a copy of it**. Read and write through it. A component that caches its own
copy saves correctly and restores nothing, and the symptom turns up a session later somewhere else.

## The files

```
Assets/<Project>/Plugins/<Name>/
  Graves.Plugin.<Name>.asmdef      references: Graves.Core, plus what step 1 decided
  <Name>Module.cs                  : DefinitionModule    - authored data, and AttachTo
  <Name>State.cs                   : InstanceState       - per-object, saved
  <Name>.cs                        : MonoBehaviour       - behaviour, if any
```

**The module's `AttachTo` is the one method.** It runs once, when the entity wakes, and it gets the
entity. Add the component, configure it from the authored fields, then bind it to its state — in that
order. Binding before configuring clamps a saved value against a default maximum, which is quietly
wrong rather than obviously broken.

**Ask for state by key, not by type**, the moment a second state of that shape could exist. A purse
is an inventory; a typed lookup matches both and returns whichever module order happened to put
first.

**Seed starting contents exactly once.** A loaded save already contains the state; without a guard,
attaching grants the starting gear again on top of it, on every load. It presents as item duplication
rather than as a save fault, so it gets investigated in the wrong place.

## Rules that are not negotiable

- **Never key anything by name, index, or object reference.** Uids only.
- **The core must not depend on this plugin.** Enforced by the assembly graph.
- **No reflection past a private member**, in either direction. If there is no honest door, that is a
  finding to raise.
- **Create entities via `GameEntity.Create`.** Adding the component and assigning the definition
  afterwards is too late: the component resolves everything the instant it exists.

## Before saying it works

- Compile and prove it — see the `verify-compile` skill; a `completed` status is not proof.
- Confirm the new assembly actually loaded, by reflection rather than by reading the asmdef.
- Save, quit, load. Anything that does not come back is a bug in this plugin, not in the save system.
- Write a decision record for any call that had a real alternative: what it cost, and what would have
  to be true for it to be wrong.
