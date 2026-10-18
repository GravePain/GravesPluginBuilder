# D-002 — Input: no action maps, no rebinding, not yet

**Decided:** 2026-09-05, by this agent. **Status:** taken, outcome not yet known.

## The decision

The approved build list describes the input plugin as "action maps per context, context switching,
rebinding, device change handling". **Only the device abstraction is being built now.** Context
switching, rebinding and action-map assets are deferred.

What ships: a source that reads devices and reports a small typed set of actions. Nothing above it
touches a keyboard.

## Why

**Context switching cannot be designed with one context.** It exists to stop gameplay input firing
while a menu is open, and there is no menu, no dialogue and no inventory screen. Building it now
means inventing the situations it must handle and then living with the guesses.

This is the same reasoning the framework already applied to the save format, and that call was
vindicated: the format was left to a plugin, and the plugin that eventually needed it made choices
nobody had anticipated. The repository's own words are that designing against imagination is the
thing to avoid.

**Rebinding is a settings-screen feature and there is no settings screen.** It also constrains how
actions are declared, so building it early would fix that shape before anything real has read from
it.

## What it costs, stated plainly

When a UI plugin arrives, gameplay input will need suppressing while a window has focus, and that
will be a change to this plugin rather than a free addition. The change is small and local: one
source, one gate. It is not the kind of retrofit that rewrites its callers, because callers ask for
an action rather than reaching for a device.

**Prediction, recorded so it can be judged:** adding context gating later will cost less than one
session and will not change a single line in any brain. If it turns out to change brains, this
decision was wrong and the reason will be that brains were allowed to know where input came from.

## The typed-actions trade

Actions are exposed as fields on a small struct rather than looked up by name.

Checked, not assumed: the reference gates skills on a weapon category using string prefix matching,
which is the same stringly-typed pattern applied to a different problem, and it is one of the
weakest things in their model. A typed struct cannot be misspelled and shows up in an inspector and
a compiler error.

The cost is real and is worth naming: a plugin that wants an action nobody anticipated must add a
field to this struct or supply its own source. That is a worse story than a string lookup for
extension, and a better one for correctness. Taken deliberately, and it is the first place this
agent has chosen against extensibility.

---

## Amended 2026-09-05, after retrospective feedback from the lead agent

**The cost was understated, and the correction is sharper than the original.**

This document named the cost as "add a field to this struct, or supply your own source". The lead
agent's point: **the second option is the real cost, and it is worse than it sounds.**

Two sources means two things produce intent. Every consumer must then decide which one it listens
to, or listen to both and reconcile them. **The escape hatch fragments the producer**, which is a
worse shape than the problem it was meant to solve — a single source that needs a field added is a
small, visible edit, while two sources is a structural question every consumer has to answer
separately and can answer differently.

So the honest statement of the trade is not "typed fields cost extensibility". It is: **typed fields
are fine, and the way out of them is not.** If an action is genuinely needed, add the field. Adding
a second source should be treated as a design failure rather than as the supported extension path,
and the guide should say so if this struct ever ships to someone else.
