# Driving Unity from here

Written into the repository on 2026-09-05 because `writing-a-plugin.md` pointed at an agent's memory
directory for this, and a memory directory is not something the next agent inherits (review finding
F-005). Anything that lives only in a session dies with it.

Ports and paths are from this machine and this project. **Re-check them rather than trusting them** —
they have already drifted twice in one evening.

---

## Route by task, not by preference

| Task | Tool |
|---|---|
| Live state, Play-mode values, diagnostics | Unity CLI `cmd eval_file` — arbitrary C#, **no domain reload**, ~1.7s |
| "Did my edit actually build?" | Unity CLI `cmd recompile` + `recompile_status` |
| Driving Play mode (enter, step, exit) | CLI `eval_file` |
| Asset moves, reimports, prefab writes, screenshots | UnitySkills REST (typed, `?mode=dryRun` validates first) |
| Reading `.cs` / `.asset` / `.prefab` / `.unity` / `.meta` | **Just read the file.** They are plain text |

Most questions need no Unity at all. Reading files is instant, imports nothing, and cannot disturb
anything.

## The Unity CLI

Requires `com.unity.pipeline` in `Packages/manifest.json` — **per project**, not per machine. Without
it every command fails with `No Pipeline instance found` even with the editor open.

The executable is not on the PATH that non-interactive shells inherit, so call it by full path. The
sub-command is `cmd <tool>`, and parameters are flags after the command name:

```
U="C:/Users/<you>/AppData/Local/Unity/bin/unity.exe"
C="--project-path <project> --format json --non-interactive --no-banner"

"$U" status                          # live editors and their ports
"$U" cmd recompile          $C
"$U" cmd recompile_status   $C       # poll until completed; check failed and errors
"$U" cmd get_console_logs   $C
"$U" cmd eval_file --file <ABSOLUTE .cs> $C
```

**Always pass `--project-path` explicitly.** More than one editor can be open, and this is what stops
a command landing in the wrong project.

### Gotchas that each cost a round trip

- **No `using` directives in an eval body.** It is wrapped in a method, so `using UnityEngine;` parses
  as a *using statement* and fails. Fully qualify everything.
- **`return` a string**, then read it from `data.result.result`.
- **`data.result` is sometimes an escaped JSON string**, not an object — parse it twice.
- **A transient failure right after entering Play mode is normal**; the domain reload kills the eval
  thread. Retry once before believing anything.
- **Creating or opening a scene and modifying it in the SAME call does not stick.** The objects you
  hold before the scene settles are not the ones that get saved, and the write reports success. Worse:
  **reading the value back is also not proof** — re-reading through the object you just wrote returns
  the right answer while you are reading memory about to be discarded. One call opens the scene,
  a later call populates it, a later one saves, and then you read the file on disk.
- **A play mode test assembly must not be Editor-only.** Restricting it to the Editor platform
  produces EditMode tests, and the run reports `testcasecount="0"` and `result="Passed"` — the worst
  available way to be told that nothing ran.
- **`-executeMethod` cannot smoke-test the runtime.** `GameEntity` does its work in `Awake`, which
  Unity does not call in edit mode, so a batch run can author content flawlessly and never produce a
  live thing. Author in an edit-mode pass, save a prefab under `Resources`, and let an unrestricted
  play mode assembly load it the way a shipped game would.
- **`recompile` returns `up_to_date` and does nothing** when Unity has not noticed on-disk edits.
  Force it: `AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate)` then
  `CompilationPipeline.RequestScriptCompilation()`.
- **Ports drift.** Both the CLI port and the UnitySkills port have moved mid-session after a domain
  reload, and **a dead port keeps showing as LISTENING** — netstat finds candidates, only `/health`
  decides which is alive.

## MCP cannot verify a compile

`Unity_RunCommand` compiles its own snippet against the **already-loaded domain**. So after editing a
`.cs` file, `isCompilationSuccessful: true` is about the snippet, and reflection over the loaded
assemblies returns the *pre-edit* domain — a type just written reads as missing, one just deleted
still reads present. This cost an hour and a wrong diagnosis on a previous project.

MCP is still the right tool for one thing: `Unity_ManageEditor` `GetState`, to check whether Graves is
in Play mode. Do that **before every editor action**.

## Never recompile during Play mode

A domain reload wipes non-serialized fields *without re-running `Awake`*, which fabricates bugs that
do not exist and destroys whatever the operator was testing. Check `GetState` first, every time.

## Play-mode observations are not trustworthy by default

Without `Application.runInBackground`, a tool-driven Play session freezes at `Time.frameCount == 1`
while `EditorApplication.isPaused` still reports `false` — every reading is of the same stalled frame.
`Assets/Graves/TestBed/EditorPlayModeFix.cs` forces it on. **Read `Time.frameCount` twice, separated
by real time, before believing any Play-mode result.**

The Project Settings "Run In Background" option does not cover this; it governs built players.

## The habit that actually catches bugs

**A return value is not proof. Read the console, and clear it first.**

Unity swallows an exception thrown inside `CreateGUI` into a `TargetInvocationException` and leaves a
blank window, while the calling script reports success. It writes `m_Script: {fileID: 0}` for a
ScriptableObject whose class is not in a file of the same name, and the asset can never load again —
while the data in it serialises perfectly. Neither produced a failed return; both said so plainly in
the console.

Every bug worth finding on this project was found by running something and then reading what Unity
said about it.
