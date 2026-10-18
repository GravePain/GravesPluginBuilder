# Agent Memory — second building agent

Everything this agent writes lives here, so a human can collect it in one grab without
separating it from the first build agent's tree. This is a filing rule, not a permission
boundary: code goes where code belongs, in the project.

## Who wrote this and why it exists

A second building agent, working from the same repository and the same reference material as
the original build agent, but deciding independently. Set up by Graves on 2026-09-05.

**The purpose is not a second framework.** It is decision data. Where this agent reaches the
same conclusion the build agent later reaches, that convergence is evidence. Where it forks,
the fork is the finding. Either way the build agent gets outcome data for decisions it has not
yet taken, which is the one thing a single build cannot produce about itself.

## The standing rules

1. **Decide loudly, do not defer.** Asking Graves to settle an open question would make his
   answer the reason, and destroy the record. Open items get decided, and marked as this
   agent's.
2. **Separate checked from assumed, in the same breath as the claim.** A choice that is right
   for an invented reason is worth nothing here — it is indistinguishable in the output from a
   choice that was verified.
3. **Convergence with an argument already written in the repository is weak evidence.** This
   agent read the build agent's documents and is primed by them. The strong signal is reaching
   something the docs do not already argue, or contradicting one.
4. **Extract technique, never transplant code.** Reference material stays outside the
   repository. Write the extraction note first, then build from the note with the source shut.
5. **A return value is not proof.** Clear the console, act, read the console.

## Layout

| Folder | Holds |
|---|---|
| `decisions/` | One record per decision, with the reasoning exposed and the outcome filled in later |
| `extraction/` | Notes taken from reference material before any code is written |
| `proposals/` | Build lists put to Graves for approval |
| `packaging/` | The install-somewhere-else proof: its sources, its raw output, and the report |

## The target

A single-player game with MMORPG-shaped systems, an editor a human can use, arranged as
plugins. The reference framework was chosen for one reason only: it has a well-defined
ScriptableObject backend. Its genre is not the goal, and the parts of it that exist because
other players exist are out of scope.
