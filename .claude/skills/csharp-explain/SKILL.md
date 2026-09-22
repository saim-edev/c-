---
name: csharp-explain
description: Explain a piece of C# at three layers — syntax, what the compiler and CLR do with it, and why it's written that way. Use when asked "what does this do", "what is this syntax", or when reading unfamiliar C# together.
---

# Explain this C#

Three layers, always in this order. Follow the `teaching-style` skill.

## Layer 1 — Syntax

What the code says, in plain language. Name every piece of syntax he may not have
met, including the small ones (`=>`, `?.`, `??`, `!`, `init`, `record`, `out`,
`async`, `<T>`). One line each.

## Layer 2 — What actually happens

The part that isn't visible in the source:

- **What the compiler generates.** Properties become `get_X`/`set_X` plus a backing
  field. Lambdas capturing locals become display classes. `using` becomes
  `try/finally`. `async` and `yield` become state machines. Records generate about
  eight members. Show it on **sharplab.io** rather than describing it.
- **What the runtime does.** Stack or heap? Does this allocate? Is the call site
  `call` or `callvirt`? Does anything box? When does the query actually execute?
- **Where the data lives.** For anything involving objects, say what the variable
  holds versus what sits on the heap.

## Layer 3 — Why it's written this way

- What problem this shape solves.
- What the alternatives were and what they would cost.
- Whether it's idiomatic C# or a local choice.
- If there's a trap in it, name the trap.

## Close with the bridge

One or two lines connecting it to functional programming — **including where the
analogy breaks**. See `teaching-style` for the standard bridges and their leaks.

## Example shape

> **Syntax:** `public IReadOnlyList<Player> Players => _players;`
> The `=>` here is an expression-bodied *property*, not a lambda. It means "every
> time someone reads `Players`, evaluate this expression".
>
> **What happens:** this compiles to a method `get_Players()` returning the field.
> There is **no backing field** for `Players` — it is computed on each read.
> Returning `IReadOnlyList<T>` is a cast, not a copy, so it costs nothing.
>
> **Why:** it hands out a *view* rather than the mutable list, so callers can't
> `.Add()` past the roster limit. Note what it does NOT do — a caller could still
> cast back to `List<Player>`. It's a strong hint, not a hard guarantee.
>
> **Bridge:** this is an accessor / lens. The leak: it isn't true immutability.
> `ImmutableArray<T>` would be, at the cost of an allocation per change.
