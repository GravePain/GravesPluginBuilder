# This project builds a game

A game, made with the Graves framework. Not a study of the framework, not a report about it — a
game. The framework is a tool here, and if it gets in the way that is a fact about the framework
worth writing down, not an argument for working around it quietly.

Graves is the sole owner and operator. There is no team, no second reviewer, and nothing you write
gets a second pair of eyes before it becomes a decision. **An unverified claim from you is not an
opinion in a discussion; it is the answer.** That is the single most important thing to hold on to.

## What is already decided

These came from a framework built and tested before this project existed. They are not open.

**Identity is a uid, never a name, an index, or an object reference.** A previous project persisted
object references, and after a restart they resolved to a boot texture, a bag mesh and an arm bone —
silently, reporting nothing wrong. Definitions are referenced by uid. Instances carry their own.

**A plugin's state IS its storage, not a copy of it.** Read and write through the state object. A
cached copy saves correctly and restores nothing, and the symptom appears one session later in a
place unrelated to the cause.

**The core must not depend on a plugin.** Enforced by assembly definitions, checked by reflection
over the loaded domain — never by reading the `.asmdef` files, which describe a request rather than
the result.

**Do not pick locks.** No reflection past a private member, on either your own code or an adopted
tool's. If something has no honest door, that is a finding to raise, not an obstacle to route
around. This rule has been broken once in the framework's history and it produced a workaround that
warned in its own comment that it should not spread.

**Split an assembly for a dependency, not for a category.** Two things that always ship together
belong together, however different they look.

## How to work

**Verify, then claim, and say what you checked in the same breath.** "Compiles clean" means you ran
a build and read its result. Unity will report success for work it did not do — see the tooling
notes — so the claim needs the evidence attached.

**Decide loudly. Do not defer.** When there is a call to make, make it, write down what it cost, and
carry on. A deferred decision reappears three plugins later with more attached to it. If the call
turns out wrong, the written record is what makes it cheap to reverse.

**Three Play-mode tests, then write it up.** Chasing a bug in circles past that point is a signal to
stop and describe what is known, not to try a fourth variation.

**Ask Graves.** He is at the editor and answers in seconds. Tools report state; only he reports
intent. He also steers by asking questions he already knows the answer to — when he asks why
something must be a certain way, the honest answer is usually that it need not be.

**Write the reasoning down, not just the change.** A commit that says what changed is half a commit.
The half that matters is why, and what it cost. Context dies at the end of a session; the artifacts
are what survive.

## The tools, in the order to reach for them

Most questions need no Unity at all. Reading files is instant and cannot disturb anything.

| Need | Reach for |
| --- | --- |
| Live state, Play-mode values, driving Play mode | Unity CLI `cmd eval_file` |
| "Did it build?" | Unity CLI `cmd recompile` then `recompile_status` |
| Asset moves, reimports, prefab writes, screenshots | UnitySkills REST |
| Anything else | MCP, last |

```
U="C:/Users/<you>/AppData/Local/Unity/bin/unity.exe"
C="--project-path <this project> --format json --non-interactive --no-banner"

"$U" status                        # live editors and their ports
"$U" cmd recompile          $C
"$U" cmd recompile_status   $C
"$U" cmd get_console_logs   $C
"$U" cmd eval_file --file <ABSOLUTE .cs> $C
```

**Always pass `--project-path`.** More than one editor can be open, and this is what stops a command
landing in the wrong project.

### What lies to you, and how

**MCP cannot verify a compile.** It reads the pre-edit domain and will report a clean build of code
you have already changed.

**MCP cannot reliably read the console.** `Unity_ReadConsole` has returned zero entries while the
editor showed fourteen errors on screen. If Graves says there are errors, there are errors — ask him
for the text rather than probing for six calls.

**`recompile_status` can return a stale `completed`.** Check that the DLL under
`Library/ScriptAssemblies/` is newer than the source you edited. That is the only proof.

**Ports drift**, both the CLI's and UnitySkills'. A dead port still shows as LISTENING, so netstat
finds candidates and only `/health` confirms one. They have moved mid-session after a domain reload.

**Never recompile during Play mode.** A domain reload wipes non-serialized fields without re-running
`Awake`, fabricating bugs that do not exist. Check whether the editor is playing first, every time.

**Check `Time.frameCount` is advancing** before trusting any Play-mode reading. A frozen editor
reports `isPaused == false` and hands you the same stalled frame over and over.

**"Main thread operation timed out after 5000ms"** means Graves is working in the editor. It is not a
broken build.

### Editor traps that report success while doing nothing

- **Exceptions inside `CreateGUI` are swallowed** into a `TargetInvocationException`. A script can
  report "window opened" for a window that threw. Read the console.
- **Scene references save as `fileID: 0`** if written and saved in the same operation. Reopening the
  scene is the fix; marking it dirty is not.
- **Serialized list elements ignore field initialisers.** A new element is zeroed, not defaulted, so
  an enum field lands on whatever zero means and throws every frame.
- **A ScriptableObject must live in a file named after its class**, or Unity writes
  `m_Script: {fileID: 0}` and the asset opens blank. Unity says so in the console; read it rather
  than diagnosing from YAML.
- **A new `.asmdef` may not appear until it is imported explicitly.** Import it, then recompile, then
  check by reflection.

### Escaping

Do not pass C# through a shell heredoc into another interpreter. `\n` inside a C# string literal has
been eaten by the layers and turned into a real newline mid-string, producing fourteen compile
errors from one edit. Write the file directly. Prefer `StringBuilder` and
`Path.DirectorySeparatorChar` over escape sequences.

## What to write down

**A decision record** whenever a call had a real alternative: what was decided, what it cost, and
what would have to be true for it to be wrong. In `Docs/decisions/`.

**A finding** whenever something claimed does not match what the code does. Findings are cheap to
raise and expensive to discover late.

Both are for the next session, which will not remember this one.

## The order to build in

See `Docs/build-order.md`. Read it before starting the first plugin — the order is not arbitrary and
two of the entries are there specifically because retrofitting them later is the expensive direction.
