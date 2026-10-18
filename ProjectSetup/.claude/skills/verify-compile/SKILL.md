---
name: verify-compile
description: Prove that Unity actually rebuilt the code you just changed, rather than reporting success for work it did not do. Use after any edit to a .cs file, before claiming anything compiles.
---

# Verify a compile

Unity will tell you a build succeeded when it never ran. This procedure is the only thing that
distinguishes "it compiled" from "something said completed".

## The procedure

**1. Check the editor is not playing.** A recompile during Play mode triggers a domain reload that
wipes non-serialized fields without re-running `Awake`, fabricating bugs that do not exist.

```bash
U="C:/Users/<you>/AppData/Local/Unity/bin/unity.exe"
C="--project-path <ABSOLUTE project path> --format json --non-interactive --no-banner"
```

Evaluate a one-line script that returns `EditorApplication.isPlaying`, `isCompiling` and
`Time.frameCount`. If it is playing, stop and exit Play mode first.

**2. Trigger the build and poll.**

```
"$U" cmd recompile        $C
"$U" cmd recompile_status $C
```

**3. Do not trust `completed`.** `recompile_status` has returned `{"status":"completed",
"failed":false}` for a build that never ran. The proof is timestamps:

```bash
stat -c '%y %n' <the .cs file you edited> Library/ScriptAssemblies/<its assembly>.dll
```

**The DLL must be newer than the source.** If it is not, the status is stale — trigger again and
poll again. Report the timestamps when claiming a clean build; they are the evidence.

**4. If it failed, read the errors from the status result**, not from the console.

## What does not work, and will waste a round trip each

- **MCP cannot verify a compile.** It reads the pre-edit domain and reports clean builds of code you
  have already changed.
- **MCP cannot reliably read the console.** `Unity_ReadConsole` has returned zero entries while the
  editor displayed fourteen errors. If Graves says there are errors, believe him and ask for the
  text rather than probing.
- **Ports drift**, both the CLI's and UnitySkills'. A dead port still shows as LISTENING. If commands
  stop landing, run `"$U" status` to find the live one — it has moved mid-session after a domain
  reload.
- **A new `.asmdef` may not appear** until it is imported explicitly. Import, recompile, then confirm
  the assembly exists by reflection over the loaded domain — never by reading the `.asmdef` file,
  which describes a request rather than a result.

## Checking assembly boundaries

The invariants are enforced by reflection over what actually loaded, not by reading the asmdefs:

- does the core reference any plugin assembly? (must be no)
- does anything reference the test bed? (must be no)
