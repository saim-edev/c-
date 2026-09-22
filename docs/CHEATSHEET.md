# Cheatsheet — C# beside what I already know

The left column is C#. The right column is the functional-programming idea it
maps onto. **The "where it breaks" column is the important one** — every analogy
leaks, and the leak is what causes three-week-old bugs.

---

## Collections and pipelines (LINQ)

| C# | Functional equivalent | Where it breaks |
|---|---|---|
| `xs.Select(f)` | `map f xs` | — (clean match) |
| `xs.Where(p)` | `filter p xs` | — |
| `xs.Aggregate(seed, f)` | `fold f seed xs` | — |
| `xs.SelectMany(f)` | `bind` / `concatMap` / `flatMap` | — |
| `xs.OrderBy(f)` | `sortBy f xs` | stable sort, but mutable source |
| `xs.GroupBy(f)` | `groupBy f xs` | returns `IGrouping`, not a map |
| `xs.Any(p)` / `xs.All(p)` | `any` / `all` | — |
| `xs.First()` | `head` | **throws** on empty |
| `xs.FirstOrDefault()` | `headOption` / `listToMaybe` | returns `null` for classes but **`0` for `int`** — not an option type |
| `a.Where(f).Select(g)` | `xs \|> filter f \|> map g` | chaining comes from extension methods, not a real pipe operator |
| `IEnumerable<T>` | a lazy sequence / stream | the **source is mutable**, so enumerating twice can give different answers or redo work |

## Types

| C# | Functional equivalent | Where it breaks |
|---|---|---|
| `class` | a record + the module of functions over it | **mutable, reference identity, aliasing** |
| `record` | a record / product type | equality includes *every* member — adding a field silently changes behaviour |
| `readonly record struct` | an unboxed value | copied on every pass — keep it tiny |
| `interface` | a typeclass / module signature | a type must declare it implements one; no retroactive instances |
| `abstract class` | a signature + partial implementation + state | you only get **one**; spend it carefully |
| `enum` | **not** a sum type | no payload, no exhaustiveness checking, `(MyEnum)99` is legal |
| `Func<A,B>` | `A -> B` | — |
| `string?` | `Option<string>` | **erased at runtime** — it's a very good linter, not a guarantee |
| `int?` | `Option<int>` | genuinely different: a real struct with `HasValue` |
| `var` | `let` with inference | never infers across method signatures — every parameter and return type is written out |

## Control and resources

| C# | Functional equivalent | Where it breaks |
|---|---|---|
| `using (x) { }` | `bracket` / `with_resource` / RAII | — (clean match) |
| `try/catch` | `Either` / `Result` | no checked exceptions; failure is not in the type |
| `throw` | — | there is no idiomatic `Result<T>` in C#; exceptions are the convention |
| `TryGetValue(k, out v)` | returning `Option<V>` | awkward imperative shape; read it as an option |
| `x?.y?.z` | mapping over an option, short-circuiting | — |
| `a ?? b` | `fromMaybe b a` / `getOrElse` | — |
| `x!` | `unsafeFromJust` / `Option.get` | **generates nothing and protects nothing** — treat with fear |

## The things with no analogue

| C# | Read it as |
|---|---|
| `this` | a hidden first argument. `t.Add(p)` is really `Team.Add(t, p)`. |
| virtual dispatch | dictionary passing, where the dictionary is attached to the value instead of passed beside it |
| inheritance | (no FP analogue — treat that as a hint to prefer composition) |
| `event` | a mutable list of callbacks, invoked synchronously, that also creates a **strong reference** |
| `async`/`await` | not green threads, not an IO monad — a compiler CPS transform that chops a method into resume points |

## Instincts to unlearn

- **`if` is not an expression.** Use `?:` or a `switch` expression when you need one.
- **Most methods return `void` and work by mutation.** You will fight this all week.
- **`List<T>` is a growable array, not a cons list.** `Insert(0, x)` is O(n).
- **Closures capture variables, not values.** Invisible in an immutable world; a real bug source here.
- **`==` means reference equality on your own classes**, but character comparison on `string`.
