# The test bed

This repository contains game content that is not part of the framework: a humanoid borrowed from
Unity's Starter Assets, two scenes, a grid material, a hop clip.

That is deliberate, and this document is the plan for keeping it deliberate. **The problem was never
the test bed existing — it was the test bed existing by accident.**

## Why it cannot simply be removed

A framework with no test bed is a framework nobody has run. Every bug found here so far was found by
running something: the invisible character, the masked path double-stepping, the scene reference
saving as `fileID: 0`, three exceptions that no return value reported. Delete the test bed and the
honest status of every feature drops back to "compiles".

Graves' own framing, and the reason this is written down rather than argued about each time: accept
that it is going to exist, then plan how it is managed.

## The four trees

| Folder | Contains | Ships |
|---|---|---|
| `Assets/Graves/Core` | the framework | yes, as the package |
| `Assets/Graves/Plugins/*` | plugins | yes, as `Samples~` |
| `Assets/Graves/Data` | authored definitions, settings, registry | never — per project |
| `Assets/Graves/TestBed` | scenes, test clips, borrowed content | never — this repo only |

`Data` and `TestBed` are separate on purpose. In a real game project only `Data` exists. Keeping them
in one folder called "Project" told nobody anything, which is how the tangle started.

## The boundary is compiler-enforced, not promised

`Assets/Graves/TestBed` has its own assembly definition. It references the core and the plugins;
**nothing references it.** A framework assembly that reaches into the test bed is a compile error, not
a code-review catch — the same trick that enforces the plugin contract.

Verify it the same way, by reflection over the loaded domain rather than by reading asmdef files.
**A roster here goes stale, and this one did — twice.** It was one assembly short until review
caught it, and then three of the assemblies it named stopped existing: `Graves.Plugin.Stats` and
`Graves.Plugin.Save` moved into the core, and `Graves.Plugin.Animation.Demo` was retired into the
test bed. **So the roster is gone and only the shape is stated**, because the shape is the part that
has never changed:

- the core references nothing
- every plugin references the core, and may reference other plugins
- **nothing at all references the test bed**

Check it by reflection over the loaded domain rather than by reading this file or the `.asmdef`s —
both describe a request, and only the loaded assemblies describe the result. Two questions, both of
which must answer no: *does the core reference any plugin assembly*, and *does anything reference the
test bed*.

## Rules

**One scene per capability, named after the capability.** `HumanoidRunnerTest`, not `Test3`. A scene
earns its place by covering something no other scene covers.

**Borrowed content lives under `TestBed/ThirdParty/<name>/` and says where it came from.** A future
reader must never have to guess whether something is framework or borrowed.

**Import the minimum.** Starter Assets is 82MB; 11MB of it is here. The 71MB of textures contribute
nothing to judging whether a blend reads correctly and stayed out. Apply the same test to anything
new: does this asset change what the test can tell us?

**Everything here is disposable.** If deleting an asset would lose framework behaviour, it is in the
wrong folder — move it into the core or a plugin.

**Prune at each packaging bump.** When a capability is covered by a newer scene, the older one goes.
Test beds rot faster than frameworks because nothing fails when they are stale.

## Third-party content currently here

**Unity Starter Assets** — `TestBed/ThirdParty/StarterAssets`. Unity's own free third-person
controller kit, available to anyone with Unity. Models and animations only; no textures, no
materials, no scripts, and not their Animator Controller.

Used because the animation runner's whole value is blending *out of a live Mecanim state*, and a
capsule with no rig and no controller cannot demonstrate that. Materials were skipped deliberately —
they are transparency-driven, and a missing texture can resolve alpha to zero, presenting as an
invisible character that reads exactly like a broken runner.

The `.meta` files travel with the FBXs because the humanoid rig is defined in them. Without them
Unity reimports as Generic and the avatar breaks.

## The rule this all serves

The test that matters is not who made a thing or what it cost. It is: **can the framework compile and
run with this deleted?** Anything the core needs present is a dependency. Everything in `TestBed` and
`Data` fails that test in the right direction — take them away and the framework is untested, but it
is not broken.
