---
name: daily-lesson
description: Runs one day of the 25-day C#/.NET curriculum end to end — theory, behind-the-scenes lab, build, doc, commit, checkpoint. Use when starting or continuing a numbered day ("let's do day 7", "continue the curriculum", "next day").
---

# Daily lesson

One day of the Esports Tournament Engine curriculum. Follow the `teaching-style`
skill throughout, and `code-commenting` for every file written.

The full plan is at
`C:\Users\saim.irshad\.claude\plans\create-a-react-and-virtual-galaxy.md`.

## Shape of a day (~4.5 hours)

| Block | Time | What happens |
|---|---|---|
| Theory | 1.5h | Concepts in order, each as what → why → how → where, with the flow |
| Behind-the-scenes lab | 0.75h | Prove it: sharplab, ILSpy, logged SQL, allocation counters |
| Build | 2h | Write the code, commented, against the real domain |
| Doc + checkpoint | 0.25h | Write the day file, answer the questions, commit and tag |

## Procedure

1. **Open with where we are.** One or two sentences: what exists, what today adds,
   what it unblocks. Never start cold.
2. **Theory block.** Concepts in the planned order. Numbered flows for anything
   multi-stage. Check in after each concept — don't deliver 90 minutes of monologue.
3. **Behind-the-scenes lab.** He runs it himself where possible. The point is that
   he *sees* the mechanism rather than taking your word for it.
4. **Build block.** Real files, real paths, from the plan. Comment per the
   commenting skill. Build after each meaningful step — never write 200 lines
   before compiling.
5. **Write the day doc.** Copy `docs/days/_TEMPLATE-day.md` to
   `docs/days/day-NN-<kebab-title>.md` and fill **all eleven sections**.
6. **Append to the concept files.** The per-day file is a journal; the
   `docs/concepts/*.md` files are what he will actually re-read. Anything durable
   goes in both. Update `docs/GLOSSARY.md` with new terms and `docs/DECISIONS.md`
   with any choice that had a real alternative.
7. **Update `docs/INDEX.md`** — fill in today's row.
8. **Commit and tag.**
   ```powershell
   git add -A
   git commit -m "feat(day-NN): <imperative summary>"
   git tag -a day-NN -m "Day NN - <title>"
   git push origin main
   git push origin day-NN
   ```
   2-4 commits per day, each one building. Small always-compiling commits are what
   make `git bisect` work on Day 24.
9. **Checkpoint questions.** Ask them **and wait for his answers before revealing
   anything.** A question he answers wrong and then corrects is worth ten he reads
   the answer to. Record both his answer and the actual one in the doc.

## Rules

- **Never skip the doc or the commit.** They are deliverables, not admin.
- **Never run ahead of the plan's sequencing** — concepts are ordered deliberately
  (memory before classes, DI before EF, basics before SignalR).
- If a day runs long, stop and carry the remainder forward explicitly in section 11
  of the doc. Do not compress the theory to make it fit.
- Flag known traps *before* he hits them, not after.
