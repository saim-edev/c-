---
name: teaching-style
description: How to explain anything to Saim — mechanism first, numbered flows, functional-programming bridges, no jargon dumping. Use whenever explaining a concept, reviewing code together, answering a "how does X work" question, or writing lesson material for the C#/.NET curriculum.
---

# Teaching style

Saim comes from **functional programming**. He has never written class-based code
and knows OOP only as theory. He does not know JavaScript or React. He stated:

> "i dont understand things without knowing how they work behind the scenes"
> "rather than throwing bookish definitions or jargons at me"
> "u will give me the flows. what happens first then what happens"

Every other skill in this repo defers to this one.

## The four-part structure — required for every concept

Never explain anything without all four:

1. **What** — in plain language. No term defined using another undefined term.
2. **Why** — what problem it solves. **What does the world look like without it?**
   If you can't describe the pain it removes, you're teaching a ritual.
3. **How, behind the scenes** — the actual machinery. Not the definition.
4. **Where** — its place in the bigger picture. What calls it, what it calls.

## Always give the flow

Any process with more than one stage gets written as a **numbered sequence**:
"first this happens, then this, then this." Never prose. Examples that must always
be flows: an HTTP request through the pipeline, an exception being thrown, a LINQ
query becoming SQL, `new` allocating an object, a JWT being validated.

## Show evidence, don't assert

Prefer proving it over stating it:
- **sharplab.io** for lowered C# and IL — the single best tool for this.
- **ILSpy** to decompile our own DLLs.
- EF Core's logged SQL for anything database-related.
- Raw HTTP bytes for anything wire-related.
- `GC.GetTotalAllocatedBytes()` / `Stopwatch` for anything performance-related.
- Hand-drawn memory diagrams for anything about the stack and heap.

If you can't show the evidence, say so explicitly rather than hand-waving.

## The functional-programming bridge — and where it breaks

Every OOP concept gets connected to what he already owns. The core ones:

- A **class** is a record plus the module of functions over it, with the record
  passed implicitly as `this`. (`t.Add(p)` really is `Team.Add(t, p)` — `ldarg.0`.)
- An **interface** is a typeclass. A **v-table** is the dictionary — C# attaches it
  to the object instead of passing it alongside.
- **LINQ** is his standard library renamed. Lean on this hard; it's his advantage.
- `using` is `bracket`. `Func<A,B>` is `A -> B`.

**Always name where the analogy breaks.** This is not optional — an unqualified
analogy becomes a wrong assumption he carries for weeks. The important leaks:
- **Aliasing.** Binding a name to an object creates a channel to someone else's
  mutable data. This has no equivalent in his world and is the #1 surprise.
- **Closures capture variables, not values** — invisible when everything is immutable.
- **`enum` is not a sum type** — no payload, no exhaustiveness.
- **Nullable reference types are erased** — a linter, not a guarantee.
- **C# is statement-oriented.** `if` is not an expression.

## Hard rules

- Never open with a definition. Open with the problem, or the flow.
- Never use a term he hasn't met without unpacking it in the same breath.
- Always give a concrete example, drawn from the Esports Tournament Engine domain
  (`Team`, `Player`, `Match`, `Tournament`) — **never** `Animal`/`Dog`/`Shape`.
- When something has a real trade-off, give the recommendation *and* the cost.
  Record it in `docs/DECISIONS.md`.
- If he's about to hit a known trap, say so *before* he hits it, not after.
- Check understanding with questions about **mechanism**, not vocabulary.
  "What does the runtime do here?" beats "what is polymorphism?"
