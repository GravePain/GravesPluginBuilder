# F-001 — Opening a scene and editing it in the same eval call writes nothing

**Found:** 2026-09-05, building the locomotion test scene. **Status:** understood, worked around.

## What happened

A single scripted call created a definition, created a scene, added an entity, assigned the
definition to it through a serialized property, and saved. Every step reported success. The saved
scene contained:

    _definition: {fileID: 0}

At runtime the core reported it correctly and loudly — the entity had no definition, so nothing was
spawned and it disabled itself. The framework behaved exactly as designed. The tooling had lied.

## What was actually wrong

Not the assignment. Splitting the work proved it: a second call that operated on the **already open**
scene assigned the same property from the same asset and read it back successfully on the next line.

So the rule is narrow and worth stating precisely. **Creating or opening a scene and then modifying
objects in it within the same call does not stick.** The objects obtained before the scene settles
are not the ones that end up saved.

The same likely applies to an asset created and then reused in one call, which is why the working
version reloads the definition from its path rather than holding the reference the create call
returned.

## Why this belongs in a note rather than being quietly fixed

This is the third time this project has recorded the same shape of failure, and the first two are
already in the repository: an exception inside a window's setup swallowed into a blank window, and a
scene object reference saving as `fileID: 0` while the write reported success. The existing guidance
calls it out in one line — a return value is not proof.

**This instance adds something that line does not carry: the successful read-back is also not
proof.** The first attempt confirmed its own write by re-reading the property through the same
object it had just written, and got the right answer. It was reading memory that was about to be
discarded. Only the file on disk settled it.

## The habit that caught it

Reading the scene file. Scenes, assets and prefabs are plain text, and grepping one field took
seconds and gave an answer no amount of scripted verification had produced. When a write matters,
read the bytes.

## Working procedure, for whoever automates scene building next

1. One call creates assets. Reload them from their paths afterwards; do not reuse what create
   returned.
2. One call creates or opens the scene, and does nothing else.
3. A later call populates and wires it.
4. A later call saves.
5. Then read the file on disk and confirm the reference has a real guid rather than `fileID: 0`.
