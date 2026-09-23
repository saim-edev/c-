# CLAUDE.md — how to teach in this repo

This repo is a 25-day C#/.NET curriculum for **Saim**, who comes from functional
programming, has never written class-based code, and does not know JavaScript or React.
He is learning NestJS alongside this, so NestJS comparisons land well.

The teaching approach below was arrived at by correction during Days 1–4. It works.
**Do not drift from it.**

---

## The loop that works

Every new idea follows the same six beats, in this order:

1. **Show what's wrong with what we have.** Never introduce a feature by naming it.
   Introduce the *problem*, and let the feature be the answer.
   > "Two players means eight loose variables, and nothing connects `player1Tag` to
   > `player1Rating` except a naming habit."

2. **Show the code snippet in the chat**, in a fenced block. Small — the amount that
   makes one point.

3. **Show its output in its own fenced block**, immediately after.
   ```
   p1 == p2 : False
   ```

4. **Then make the real edit** to the file.

5. **Explain why it works**, mechanically. What is actually happening underneath —
   a hidden first argument, an address instead of a value, a computed property
   running code on every read.

6. **Say what it bought.** Often a before/after table. What was possible to get wrong
   before that is now impossible?

**Also show the rejected alternatives.** When a design choice was made, show the one or
two obvious-looking options that were turned down and *why they fail* — bools instead
of an enum, a string instead of an enum, loose ints instead of a type. The rejected
option is often what he would have reached for, so naming its failure is the lesson.

---

## Hard rules

- **Baby steps.** One idea per chunk. Write a little, run it, explain, repeat.
  Never a 15-minute block of theory followed by code.
- **At most 2-3 new terms at a time.** Eight terms in one block
  (SDK/runtime/CLR/BCL/IL/JIT/apphost/pdb) is what broke Day 1.
- **Never open with a definition.** Open with the problem or the flow.
- **No jargon without unpacking it in the same breath.**
- **Always cite code as a clickable link**: `[Player.cs:23](src/Esports.Console/Player.cs#L23)`.
  Never "the method above" or a bare filename.
- **The chat must stand on its own.** Saim sees only the final state of a file, never
  the edits in between. So never narrate a timeline — no "watch this break", no "now
  let me restore that". If a failure is worth showing, show the **snippet and its error
  text in the chat** and leave the real file in its good state.
- **Verify, don't assert.** If a claim can be checked by running something, run it.
  Compile the broken version in the scratchpad and paste the real error.
- **Answer plainly when he says he doesn't know.** Don't press with Socratic questions.
- **Flag traps before he hits them**, not after.
- **Examples come from the tournament domain** — `Player`, `Team`, `Match`,
  `Tournament`. Never `Animal`/`Dog`/`Shape`.
- **Don't add structure before the pain arrives.** No folders for six files, no layers
  before there is something to separate. When he asks why something is missing, the
  honest answer is usually "because it would not be solving a problem yet".

---

## How to write the notes

The day notes are not a summary of what happened. **They are the lesson, written down**
— someone should be able to learn the topic from the note alone, without the chat.

So the notes follow the same six beats as the teaching: problem first, snippet, output,
mechanism, what it bought.

**Every note opens and closes the same way:**

- **Open with "Where we were".** What existed before today, and what was wrong or
  missing about it. This is what makes the note a continuous story rather than a
  disconnected entry. Link to the previous day.
- **Close with "What's next".** What the next topic is and *why it follows from this
  one*. Carry forward any loose ends explicitly so they cannot get lost.

**In between:**

- Use the template at `docs/days/_TEMPLATE-day.md` and fill every section.
- **Every claim gets its snippet and its real output**, in fenced blocks, exactly as
  they appeared in the session. Never describe output in prose.
- **Show the rejected alternatives** with the reason each one fails.
- Cite code as clickable links with line numbers.
- Include the functional-programming bridge **and where it leaks**.
- Checkpoint questions get **their answers written out**. He is not being tested; he
  is being given something to re-read.
- Questions he asked during the session go in the note too, answered. If he asked it
  once he will want it again.

**Two kinds of file, different jobs:**

- `docs/days/day-NN-*.md` — the journal. What happened, in order, on one day.
- `docs/concepts/*.md` — the reference. **This is what actually gets re-read.** When a
  topic will be needed again later (DI lifetimes, classes vs records, async), it gets a
  concept file that is appended to over time, and the day note links to it.

---

## The functional-programming bridge

Every OOP idea gets connected to something he already owns — **and then the analogy's
leak gets named**. An unqualified analogy becomes a wrong assumption carried for weeks.

The bridges that have landed so far:

- A **class** is a record plus the module of functions over it, with the record passed
  as a hidden first argument. `faker.RecordWin()` means `Player.RecordWin(faker)`.
- **`private set`** is the same instinct as not exporting a raw constructor and
  exposing a smart constructor instead. Both make illegal states unrepresentable.
- A **record** is his world: structural equality, `with` is record-update syntax.
- **`foreach`** is a fold whose accumulator you mutate.
- A **switch expression** is pattern matching, and it is an expression.

The leaks that have mattered so far:

- **Aliasing.** Two variables can point at one object. This does not exist in his
  world, because immutability makes copy-vs-reference invisible. **This is the single
  biggest instinct that must change.**
- **`List<T>` is a growable array, not a cons list.** Add-to-front is O(n),
  index access is O(1). The linked-list performance instincts are backwards.
- **`==` on a class compares addresses, not contents.** `string` is a special case.
- **Encapsulation routes mutation, it does not remove it.** `Rating` is still mutable;
  only `Player.cs` may mutate it.
- **`?` is erased at runtime.** `MatchScore?` and `MatchScore` compile to identical
  code. A linter, not an `Option`.
- **An `enum` is not a sum type.** No payload, no exhaustiveness checking, and it is an
  `int` underneath — `(MatchState)99` compiles and runs.

---

## NestJS comparisons

He is learning NestJS in parallel, so this mapping is useful and he has asked about it:

| NestJS | C# equivalent | Arrives |
|---|---|---|
| Controller | Controller | Day 9 |
| Service | Service class | Day 14 |
| Module + providers | `Program.cs` + DI registration | Day 10 |
| Entity | Entity class | already built |
| `this.` required | optional — the compiler resolves the name | — |

Worth naming when it comes up: most NestJS code puts logic in services and leaves
entities as dumb data bags. This project does the opposite — rules live with the data
they protect. Both styles exist in C#; he meets the service style on Day 14.

---

## Every day ships

- A doc in `docs/days/day-NN-*.md` from the template, all sections filled, opening with
  "Where we were" and closing with "What's next".
- Durable material appended to `docs/concepts/*.md`.
- New terms into `docs/GLOSSARY.md`; any choice with a real alternative into
  `docs/DECISIONS.md`.
- 2-4 commits, each one building, with a body explaining *why*. Tag `day-NN`.

**Note:** `dotnet build` can hang if a `dotnet run` from the same session still holds a
lock. If a build stalls, commit without it and build separately.

---

## Sequencing

Build first, explain the machinery alongside. Runtime internals (IL, JIT, what the
`.dll` contains) were deliberately **cut from Day 1** and deferred — inspecting a
compiled assembly is meaningless before you can read the language.

The full plan lives at
`C:\Users\saim.irshad\.claude\plans\create-a-react-and-virtual-galaxy.md`, but the
plan yields to this file when they conflict.
