---
name: debug-session
description: Trace a bug from symptom to line using the five-step method plus the Visual Studio debugger and git archaeology. Use when something is broken, throwing, returning a wrong value, or behaving unexpectedly.
---

# Debug session

**The rule that governs everything here: confirm with a breakpoint, not by reading.**
Reading tells you what the code *should* do. A breakpoint tells you what it *does*.

## The five-step method

1. **Reproduce it.** If you can't reproduce it you aren't debugging, you're
   guessing. Get the exact input, the exact account, the exact steps.
2. **Find a unique string.** The error text, a status code, a JSON field name, a
   button label. Anything literal.
3. **Search for it.** `Shift+F12`, `Ctrl+Shift+F`, or `git grep`. A literal search
   finds in seconds what reading finds in hours.
4. **Walk outward.** From the hit: who calls this? (`Shift+F12` Find All
   References, `Ctrl+K,Ctrl+T` Call Hierarchy.) Keep going up until you reach an
   entry point — a controller action, a hub method, a worker loop.
5. **Confirm with a breakpoint.** Set it, inspect the real values, compare against
   what you expected. The gap is the bug.

## Visual Studio keys worth knowing

| Key | Does |
|---|---|
| `F9` | toggle breakpoint |
| `F5` | start / continue |
| `F10` / `F11` / `Shift+F11` | step over / into / out |
| `F12` / `Ctrl+F12` | go to definition / **implementation** (the one that matters with interfaces) |
| `Shift+F12` | find all references |
| `Ctrl+T` | go to anything by name |
| `Ctrl+-` | navigate back |
| `Ctrl+Alt+I` | Immediate Window — evaluate arbitrary expressions at a breakpoint |
| `Ctrl+Alt+E` | Exception Settings |

**Beyond plain breakpoints** — the ones most people never learn:

- **Conditional breakpoint** (right-click the breakpoint, Conditions): break only
  when `team.Tag == "G2"`. Essential inside loops.
- **Hit count**: break on the 500th iteration, not the first.
- **Tracepoint** (right-click, Actions): print a message and keep running. Logging
  **without editing code and without stopping**.
- **Exception Settings** (`Ctrl+Alt+E`): break the moment an exception is *thrown*,
  not where it's caught. This is how you find the true origin.

## When the code won't tell you, git will

```bash
git log -S"the exact string"      # which commit introduced or removed this?
git blame -w -L 40,60 path/file   # who last touched these lines? -w ignores reformats
git show <sha>                    # the reasoning behind that commit
git bisect start                  # binary search for the commit that broke it
git bisect bad HEAD
git bisect good day-18
# test, answer good or bad, repeat, then:
git bisect reset
```

`git bisect` finds the culprit among 120 commits in roughly 7 tests. It only works
if commits build — which is exactly why the curriculum insists on small, always
compiling commits.

## Always: record it

Append every real bug hunt to `docs/concepts/debugging.md`:

```markdown
## <date> — <one-line symptom>
**Symptom:** what was observed, phrased the way a user would say it
**Root cause:** the actual defect
**How I found it:** which step of the method got there
**Fix:** what changed
**Lesson:** what would have prevented it
```

This file becomes more valuable than any course. Never skip it.
