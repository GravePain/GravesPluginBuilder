# GravesPluginBuilder

An experiment in developer and AI synergy, with a real framework as the output.

A native C# Unity framework for the spine every game needs: entities, items, inventories, skills,
quests and recipes, authored as ScriptableObjects through one editor window, and extended by plugins
the core knows nothing about. Built in roughly twelve hours by two AI building agents working
independently from one repository, with a third agent reviewing read-only, and one human at the
editor. Every decision, mistake and correction is written down in this repository.

**The framework is real and wanted. The working method is the actual subject.**

Unity 6000.3 or later. No asset-pack dependencies. MIT licence.

---

## Contents

- [What you can do with it](#what-you-can-do-with-it)
- [Start here](#start-here)
- [The line](#the-line)
- [Layout](#layout)
- [What exists](#what-exists)
- [The research side](#the-research-side)
- [What is in `Docs/`](#what-is-in-docs)
- [What is in `Agent Memory/`](#what-is-in-agent-memory)
- [Two rules that are load-bearing](#two-rules-that-are-load-bearing)
- [Reference material](#reference-material)

---

## What you can do with it

**Play the game that proves it.** Open `Assets/Game/Survival/SurvivalLoop.unity` and press play.
WASD moves, E gathers, C crafts, F5 saves, F9 loads. Three saplings, two outcrops, four wood and two
stone make a stone axe. The game lives in its own assembly outside `Assets/Graves`, and nothing in
the framework references it.

**Author content without writing editor code.** Open **Graves → Authoring Window**. Every kind of
definition in the project is a tab: entities, items, skills, quests, recipes, equipment slots,
crafting stations. Create one, name it, add modules. Each module is a plugin's data and draws
itself, so adding a field to a plugin costs no editor code. **Validate** reports integrity problems
rather than repairing them. **Report** copies a full diagnostic to the clipboard. The **Manual** tab
renders the framework documentation beside the thing you are authoring.

**Write a plugin.** The contract is one virtual method and a serialisable state class, plus an
assembly definition that references the core. Read
[`writing-a-plugin.md`](Assets/Graves/Core/Documentation~/writing-a-plugin.md) first. Thirty-seven
plugin assemblies in this repository show the shapes worth copying.

**Install it somewhere else.** `Tools/build-package.sh` assembles `com.graves.core` as a Unity
package. Drop the output in another project's `Packages/` folder, copy in the plugin folders you
want, and author. This was proved in a fresh, empty project with nine authoring checks and one play
mode test, with no line of the framework edited to make it happen. The report is in
[`Agent Memory/packaging/packaging-report.md`](Agent%20Memory/packaging/packaging-report.md).

**Study the method.** If what interests you is how a person and several agents built and checked
this, start with [The research side](#the-research-side).

## Start here

| Read this | For |
|---|---|
| [`Docs/user-stories.md`](Docs/user-stories.md) | What was asked for, in the operator's own words. Every scope decision is checked against it |
| [`Docs/where-things-stand.md`](Docs/where-things-stand.md) | The state of the project at the end of the build, written for picking it up cold |
| [`Core/Documentation~/overview.md`](Assets/Graves/Core/Documentation~/overview.md) | What the framework is and what it gives you |
| [`Core/Documentation~/writing-a-plugin.md`](Assets/Graves/Core/Documentation~/writing-a-plugin.md) | The plugin contract. Read before writing code |
| [`Core/Documentation~/plugin-categorisation.md`](Assets/Graves/Core/Documentation~/plugin-categorisation.md) | Where a thing goes, and the minimum for a game |
| [`Core/Documentation~/tooling.md`](Assets/Graves/Core/Documentation~/tooling.md) | How to drive Unity from a terminal, and what lies to you while you do |
| [`Core/Documentation~/packaging.md`](Assets/Graves/Core/Documentation~/packaging.md) | How this ships, and why it ships that way |

The framework documentation lives inside the package at
[`Assets/Graves/Core/Documentation~/`](Assets/Graves/Core/Documentation~/index.md) so it travels with
an install and renders in the editor's Manual tab. `Docs/` holds the project's own record.

## The line

**The core owns identity, structure, extension and integrity. Plugins own behaviour and
presentation.** Or: the core is the bookends, the plugins are the book.

The core defines nothing about 2D or 3D, controllers, movement or physical interaction, and it
depends on no plugin. That is enforced by assembly definitions and checked by reflection over the
loaded domain, not by convention.

## Layout

```
Assets/Graves/Core       the framework              ships as the package com.graves.core
Assets/Graves/Plugins/*  plugins                    ship as Samples~, installed by choice
Assets/Graves/Data       definitions and settings   never ships; per project
Assets/Graves/TestBed    scenes and borrowed art    never ships; this repository only
Assets/Game/Survival     the game built on it       its own assembly, references the framework
Docs/                    the project record         user stories, reviewer files, research notes
Agent Memory/            the second agent's record  decisions, proposals, packaging proof
ProjectSetup/            a starter kit              CLAUDE.md, skills and build order for a new game
Tools/                   build-package.sh           assembles the distributable package
```

The test bed policy, and the rule that nothing may reference the test bed, is in
[`Docs/test-bed-policy.md`](Docs/test-bed-policy.md).

## What exists

**Core.** Definitions with a permanent uid, a registry that reports rather than repairs, runtime
instances with per-object state, a module extension point, `GameEntity.Create` for making an entity
from script, a save system with JSON slots and atomic writes that carries forward state from an
uninstalled plugin rather than destroying it, level-based values, an `IMortal` contract, a settings
asset and a version stamp. Version 0.1.0; see
[`CHANGELOG.md`](Assets/Graves/Core/CHANGELOG.md).

**Editor.** One authoring window (create, duplicate, delete, search, reorder, validate) that renders
a definition's own inspector, a Manual tab, and a diagnostic report for bugs found in projects nobody
else can run.

**Plugins.** Thirty-seven assemblies, grouped by what they do:

- **Input, locomotion, camera.** An input source, a motor resolved at attach time rather than
  wired by hand, a player brain, a follow camera.
- **Vitals, death, lifecycle.** Health, damage, death and respawn, win and lose conditions.
- **Combat, AI, skills.** Attackers, a chase brain that hunts targets, skills as pure data with
  effects, requirements and blockers supplied by other plugins.
- **Animation.** Named clips played through a Playables graph over a live Mecanim state, with a
  masked upper-body path.
- **Inventory, equipment, enhancement, currency.** Positional slots, stacking that cannot merge two
  distinguishable objects, equipment slots with stat bonuses, enhancement that scales those bonuses,
  gold in a purse.
- **Gathering, crafting, progression, quests.** Harvestables, recipes with requirement modules,
  crafting stations, experience and levels, quests that count kills and pay rewards.
- **Spawning, UI.** A spawner, a UI root, a health bar binding.

Measured at the end of the build, fourteen of nineteen top-level plugins could be deleted without
any other plugin's root breaking.
The live list of modules is the authoring window's **Add module** menu, built by reflection,
because a roster in a document went stale twice in one day.

## The research side

The question underneath the framework: **how does one person get reliable work out of AI agents
that can run code, when nobody is checking them?** The answers found here are specific and
transferable.

**Three agents, three jobs.**

- A **building agent** with the full context of the conversation, able to run Unity, read consoles
  and test in play mode.
- A **second building agent**, set up a day later, working from the same repository and the same
  brief but deciding independently. Its purpose was decision data: where it reached the same
  conclusion as the first agent, that convergence is evidence; where it forked, the fork is the
  finding. Everything it wrote is in `Agent Memory/`.
- A **read-only reviewer** that may write exactly three files, never runs anything, and never
  commits. Its charter says to treat commit messages and documentation as claims, never as evidence,
  because the same agent wrote the code and the message.

**What that produced.**

- The reviewer found two live data-loss bugs and four false documentation claims in its first hour,
  in code the building agent had tested and described as verified, without running anything.
- The reviewer's first question was that the five user stories existed only in an agent's memory.
  They are now in the repository, which is why `Docs/user-stories.md` exists.
- The second agent recorded predictions before building and scored them afterwards, including the
  ones it got wrong. One of its findings, that a plugin's state must be its storage rather than a
  copy, became a constraint on the plugin contract that was invisible from inside the framework.
- A packaging test installed the core in a fresh project and found seven gaps, six of them invisible
  from inside this repository because this is the one place the repository is always present.
- Four field notes on working with agents, each recording what the operator did and why it worked:
  constraining a reviewer so its only action is to report, catching an invented detail that happened
  to be true, stopping an agent that was spinning out with two words, and why an agent's
  self-criticism needs more scrutiny than its claims of success.

**What to expect from the record.** It is written in the agents' own voices, dated, and not tidied
after the fact. The operator is quoted directly throughout. Decisions say what was checked and what
was assumed in the same breath. Where an agent was wrong, the correction is a later entry rather than
an edit, so you can see the mistake and the catch.

## What is in `Docs/`

| File | What it is |
|---|---|
| [`user-stories.md`](Docs/user-stories.md) | The five original stories and what was said about the project, quoted exactly |
| [`where-things-stand.md`](Docs/where-things-stand.md) | The closing state: what is finished, by what definition, and what is deliberately not done |
| [`test-bed-policy.md`](Docs/test-bed-policy.md) | Why the repository carries scenes and borrowed art, and the rules that keep that deliberate |
| [`DRAFT-pending-changes.md`](Docs/DRAFT-pending-changes.md) | The first agent's holding list after reading the second agent's notes: each change, what it rests on, and what was still missing. Kept as a record of the cross-agent exchange |
| [`research/working-with-claude.md`](Docs/research/working-with-claude.md) | Four field notes on the working method, each recording what the operator did and what it caused |
| [`reviewer/CHARTER.md`](Docs/reviewer/CHARTER.md) | The read-only reviewer's rules: three files, no running, claims are not evidence |
| [`reviewer/journal.md`](Docs/reviewer/journal.md) | The reviewer's passes, in order, each naming the commit range it covered and what it could not check |
| [`reviewer/findings.md`](Docs/reviewer/findings.md) | Every place a claim and the code disagreed, cited. Statuses are the reviewer's alone |
| [`reviewer/questions.md`](Docs/reviewer/questions.md) | What the reviewer could not determine from the artifacts, which the charter treats as documentation defects |
| [`reviewer/replies.md`](Docs/reviewer/replies.md) | The building agent's answer to each finding, with what changed and where to verify it |

## What is in `Agent Memory/`

Everything the second building agent wrote, filed in one place so it can be read, or removed, as a
unit. Its [`README.md`](Agent%20Memory/README.md) states its standing rules.

| Folder or file | What it holds |
|---|---|
| [`journal.md`](Agent%20Memory/journal.md) | The build in order: closing the loop, the backend, the session where a person played it, the handover, and the corrections |
| [`decisions/D-*.md`](Agent%20Memory/decisions) | One record per design decision, with the reasoning exposed and the outcome filled in later. Entity kinds, input scope, when to split an assembly, damage filtering, identity and stacking, plugin-to-plugin dependencies, who counts a kill |
| [`decisions/F-*.md`](Agent%20Memory/decisions) | Findings made by running things: a scene-editing trap in the tooling, state as storage, cached lookup and module order, a save that writes what it cannot restore |
| [`proposals/`](Agent%20Memory/proposals) | The build list put to the operator and approved, the ranked backlog in the operator's order, and drop-in sections for the plugin guide |
| [`packaging/`](Agent%20Memory/packaging) | The install-somewhere-else proof: its test sources, raw output, and the report |
| [`user-stories-given-to-this-agent.md`](Agent%20Memory/user-stories-given-to-this-agent.md) | Stories given directly to the second agent, quoted exactly, with its reading underneath |
| [`Lead Agent Feedback mid build retro.md`](Agent%20Memory/Lead%20Agent%20Feedback%20mid%20build%20retro.md) | The first agent's retrospective on the second agent's decisions: where they converged, where they forked, and where the second agent was right |

## Two rules that are load-bearing

**A return value is not proof.** Read the console, and clear it first. Every bug worth finding here
was found that way, and several were reported as successes by the code that had just failed.

**You can verify that something runs. You cannot verify that it looks right.** The masked animation
path passed every measurement taken and was still visibly broken. It was found by a human pressing a
key on the wrong foot.

## Reference material

Commercial frameworks were studied during this build for technique, never copied, and their source
was kept outside the repository so it could not be committed by accident. The extraction notes
written from that reading were removed before publication. Where the record refers to "the
reference" or "the reference framework", that is what it means. The only third-party content in the
repository is Unity's own Starter Assets humanoid and clips under `Assets/Graves/TestBed/ThirdParty`,
documented in the test bed policy.
