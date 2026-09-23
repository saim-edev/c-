# Collections — holding many things

How to store more than one of something, and how to pick the right container.

> **The one-line version:** `List<T>` is a **growable array**, not a cons list. Every
> performance instinct you brought from linked lists is backwards here.

---

## 1. The problem that came first

A team has five players. Written the only way you know so far:

```csharp
public class Team
{
    public Player Player1 { get; private set; }
    public Player Player2 { get; private set; }
    public Player Player3 { get; private set; }
    public Player Player4 { get; private set; }
    public Player Player5 { get; private set; }
}
```

This is the loose-variables problem one level up. Three things are wrong with it:

- **Nothing connects them** except a naming habit. `Player3` is related to `Player4`
  only because you typed similar words.
- **You cannot answer "how many?"** without checking each field for `null` by hand,
  one `if` per slot.
- **You cannot walk them.** Averaging the ratings means writing the addition out five
  times, and rewriting it the day the roster size changes.

Five fields is also a lie about the domain. A roster is *a number of players*, and the
number is part of what varies.

---

## 2. `List<T>` — one variable, many items

```csharp
private List<Player> _players = new List<Player>();
```

That is the real line from [Team.cs:17](../../src/Esports.Console/Team.cs#L17).

The `<Player>` part is the **element type**. `List` on its own is a container shape;
`List<Player>` is that shape filled in with what it holds. The bracket syntax is
called a *generic type argument* — the container is written once, in the .NET library,
and works for any element type you name.

### `<T>` is enforced by the compiler, not checked at runtime

This is the whole point of it. Putting the wrong thing in does not fail later, in
production, with a confusing message — it fails at build time:

```csharp
List<Player> roster = new List<Player>();
roster.Add(new Player("Faker", 1847));
roster.Add("Gumayusi");                  // a string, not a Player
```

```
Program.cs(14,20): error CS1503: Argument 1: cannot convert from 'string' to 'Player'
```

The build does not produce an executable. Line and column included. Compare that with
a JavaScript array, which would happily hold the string and hand you
`undefined` when something later reached for `.Rating`.

---

## 3. The big correction: it is an array, not a cons list

This is the single most important thing on this page, because the instinct it corrects
is strong and silent.

A `List<Player>` is **one contiguous block of memory** holding arrows, plus a count.

```
  List<Player> holding 3 players, with room for 4:

   _players ──► ┌─────┬─────┬─────┬─────┐
                │  0  │  1  │  2  │     │   Capacity = 4
                └──┬──┴──┬──┴──┬──┴─────┘   Count    = 3
                   ▼     ▼     ▼
                 Faker  Guma  Keria

   Each slot holds an ARROW to a Player object living
   elsewhere on the heap. The slots themselves are
   side by side, at known distances apart.
```

**`Count` is how many you put in. `Capacity` is how many slots exist.** They are
different numbers and only one of them is your business.

### What happens when it fills up

There is no room to grow into, because the block is a fixed size. So:

1. Allocate a **brand new array of double the size**.
2. Copy every element across, one by one.
3. Throw the old array away for the garbage collector.
4. Point `_players` at the new one.

```
  Count = 4, Capacity = 4.  Add a 5th:

  old ──► ┌─────┬─────┬─────┬─────┐
          │  0  │  1  │  2  │  3  │      discarded
          └──┬──┴──┬──┴──┬──┴──┬──┘
             │     │     │     │   copy all four across
             ▼     ▼     ▼     ▼
  new ──► ┌─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┐
          │  0  │  1  │  2  │  3  │  4  │     │     │     │
          └─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┘
                                            Capacity = 8
```

That is not a story. Printing `Count` and `Capacity` after each `Add` gives:

```
start           Count=0 Capacity=0
after Add(Faker    ) Count=1 Capacity=4
after Add(Gumayusi ) Count=2 Capacity=4
after Add(Keria    ) Count=3 Capacity=4
after Add(Oner     ) Count=4 Capacity=4
after Add(Zeus     ) Count=5 Capacity=8
```

An empty list allocates nothing at all (`Capacity = 0`), jumps to 4 on the first
`Add`, then doubles.

### The performance table, which is the opposite of yours

| Operation | Cons list / linked list | C# `List<T>` |
|---|---|---|
| Add to **end** | O(n) | **O(1) amortised** |
| Add to **front** | **O(1)** | **O(n)** — every element shifts right |
| Get item #5000 | **O(n)** — walk 5000 nodes | **O(1)** — one multiplication |
| Remove from middle | O(1) once you're there | O(n) — everything after shifts |

"Amortised" means: *usually* free, but occasionally one `Add` pays for a whole copy.
Spread over many adds, the average cost per add is still constant — the doubling is
what makes that true. If it grew by one slot each time, every add would copy
everything and appending n items would cost O(n²).

**Building a list by prepending is a normal, cheap thing in your world and an
expensive mistake here.** `list.Insert(0, x)` in a loop is O(n²).

### The practical consequence: pre-size when you know

If you already know how many items are coming, say so, and the doubling-and-copying
never happens:

```csharp
// A roster is five players. Allocate five slots once.
private List<Player> _players = new List<Player>(5);
```

The number in the constructor is the **capacity**, not the count — the list is still
empty, `Count` is still `0`. It is a hint about the future, not contents.

This is a small optimisation for five players and a large one for fifty thousand rows
coming back from a database.

---

## 4. `foreach` — a fold whose accumulator you mutate

From [Team.cs:48](../../src/Esports.Console/Team.cs#L48):

```csharp
public double AverageRating
{
    get
    {
        if (_players.Count == 0)
        {
            return 0;
        }

        int total = 0;

        foreach (Player player in _players)
        {
            total = total + player.Rating;
        }

        return (double)total / _players.Count;
    }
}
```

```
Average rating: 1806.2
```

Read it as the fold it is:

```
  fold (+) 0 (map rating players)

  total = 0            ← the seed
  foreach (...)        ← the traversal
    total = total + …  ← the combining step, writing back into the accumulator
```

The one difference that matters: `total` is **rebound on every pass**, not threaded
through as a new value. It is a fold with a mutable accumulator sitting in a local
variable. Same shape, different mechanics.

**What `foreach` actually does:** it asks the collection for an *enumerator* — a small
cursor object with "move to the next one" and "give me the current one" — and calls
those in a loop until it runs out. You never see it. It matters for exactly one reason,
covered in the traps: mutating the collection mid-loop invalidates that cursor.

---

## 5. The integer division trap

Both `total` and `_players.Count` are `int`. So this:

```csharp
return total / _players.Count;
```

does **integer division**, discards the remainder, and returns a quietly wrong number.
With the real T1 roster the ratings sum to 9031 across 5 players:

```
total / count          = 1806
(double)total / count  = 1806.2
(double)(total / count)= 1806
```

`1806` instead of `1806.2`. No error. No warning. The build is clean and the answer is
wrong.

The fix, [Team.cs:66](../../src/Esports.Console/Team.cs#L66):

```csharp
return (double)total / _players.Count;
```

**The cast has to come first, and the third line above is why.** C# picks which
division to run from the operand types *at that moment*:

```
  (double)total / count        double / int  → promotes int to double
                                             → real division   → 1806.2

  (double)(total / count)      int / int     → integer division → 1806
                               then widen 1806 to a double      → 1806.0
```

Casting the result is too late — the truncation already happened. Casting one operand
changes which operator gets chosen. The parentheses are the whole difference.

This bites hardest when the numbers look plausible. `1806` is not obviously wrong; a
rating average that is silently a fraction of a point off will survive review.

---

## 6. `Count` vs `Count()`

They look like the same thing and are not.

| | What it is | Cost |
|---|---|---|
| `_players.Count` | a **property** on `List<T>` — a stored field | free, O(1) |
| `_players.Count()` | a **LINQ method** on any sequence | O(n) in the general case |

A `List<T>` already knows how many items it holds — it keeps the number and updates it
on every `Add`. Reading `Count` reads that field.

`Count()` with parentheses is LINQ, and LINQ works on anything enumerable, including
things that have no idea how long they are until you walk them. It is smart enough to
shortcut when the underlying thing *is* a `List<T>`, but you cannot see that from the
call site, and if the sequence is a database query or a lazy pipeline it will walk the
whole thing.

**Rule: if the thing is a `List<T>` or an array, use `Count` with no parentheses.**

---

## 7. Choosing a collection

| You need | Use | Why |
|---|---|---|
| An ordered sequence, indexed, that grows | `List<T>` | The default. Contiguous, fast to walk, fast to index. |
| Look something up **by a key** | `Dictionary<K,V>` | O(1) lookup by key instead of scanning. |
| "Is this in the set?" and no duplicates | `HashSet<T>` | O(1) membership, duplicates rejected. |
| To **expose** a list without letting callers change it | `IReadOnlyList<T>` | Indexing and `Count`, but no `Add`. |
| A lazy pipeline you will walk once | `IEnumerable<T>` | Nothing is computed until something asks. |
| A fixed size, decided once, never changing | `T[]` (array) | No growth machinery. Rare in application code. |

**`Dictionary` and `HashSet` have not been used in this project yet.** That is
deliberate — nothing here has needed a keyed lookup or a membership test, and adding a
type before there is a problem for it to solve teaches the type without teaching the
reason. They arrive when a problem asks for them.

The bottom three rows are about the *shape of the promise you make*, not about storage.
`IReadOnlyList<T>` and `IEnumerable<T>` are not containers — they are **views** onto
one. A `List<Player>` is all three of those things at once; which one you name in a
signature is a choice about what you are letting the caller do.

---

## 8. Why the roster is not exposed as a `List<Player>`

[Team.cs:17](../../src/Esports.Console/Team.cs#L17) is `private`, not `private set`,
and this is not a style preference — it follows directly from what a variable holds.

```csharp
public List<Player> Players { get; private set; }   // looks safe. is not.
```

`private set` stops a caller replacing the whole list. It does nothing about the list
itself. Reading the property hands the caller **the arrow to the real roster**:

```
   team._players ──┐
                   ▼
           ┌──────────────────┐
   caller  │ the ONE roster   │      caller.Add(sixth) mutates
    copy ──┤ Faker Guma Keria │      the team's own roster.
     of    │ Oner  Zeus       │      AddPlayer's limit was never
   the ────┘                  │      involved. There is no sixth
   arrow    └─────────────────┘      check to run.
```

The five-player limit lives inside `AddPlayer` at
[Team.cs:30](../../src/Esports.Console/Team.cs#L30). A caller holding the list does not
go through `AddPlayer`, so the limit is not bypassed so much as simply absent.

This is the aliasing lesson applied to a container: see
[memory-and-references.md](memory-and-references.md). The rule that follows from it —
**never hand out a mutable reference to something you enforce rules about** — is one of
the most load-bearing habits in this whole language.

The fix is to expose a view:

```csharp
public IReadOnlyList<Player> Players => _players;
```

`IReadOnlyList<Player>` has `Count` and indexing and no `Add`, so the roster can be
read and walked but not grown from outside. **It is a compile-time guarantee, not a
runtime one** — see the traps.

---

## 9. Coming from functional programming

**What carries over:**

- `List<T>` is the default sequence type, the way a list is in your world.
- `foreach` is a fold with a mutated accumulator.
- LINQ (Day 11-ish) is `map`/`filter`/`fold` under different names, and it is the part
  of C# that will feel most like home.

**Where it leaks — five ways, all of them consequential:**

1. **It is not a cons list.** Growable array. Cheap at the end, expensive at the front,
   free at any index. Section 3 has the table; it is the opposite of yours.

2. **`Add` mutates in place.** It returns `void`. There is no new list — the one you
   passed to a function is the one that changed. `list.Add(x)` is a statement, not an
   expression, and writing `var newList = list.Add(x)` does not compile.

3. **No structural sharing.** Two lists that share 99% of their contents share no
   memory. Copying a list copies the array. The cheap-persistent-update trick you rely
   on has no equivalent here.

4. **No persistence.** There is no old version. Once `Add` runs, the previous state of
   the list is gone. If something else was holding that list, it sees the change.

5. **A lazy `IEnumerable<T>` can redo the work.** Enumerating it twice can execute the
   whole pipeline twice — two database round-trips, two sets of side effects, possibly
   two different answers if the source changed in between. Laziness in your world is
   usually memoised; here it is not. `.ToList()` is how you force it once and keep the
   result.

---

## 10. Coming from JavaScript

Close enough to be useful, and the gaps are worth naming.

| JavaScript | C# |
|---|---|
| `const xs = []` | `var xs = new List<Player>()` |
| `xs.push(p)` | `xs.Add(p)` |
| `xs.length` | `xs.Count` (property, no parentheses) |
| `xs[0]` | `xs[0]` |
| `for (const x of xs)` | `foreach (var x in xs)` |
| `xs.map(f)` | `xs.Select(f)` — LINQ |
| `xs.filter(f)` | `xs.Where(f)` — LINQ |
| `xs.reduce(f, 0)` | `xs.Aggregate(0, f)`, or `xs.Sum(...)` |

Three real differences:

- **A JS array holds anything. A `List<T>` holds one type**, checked at build time.
  Section 2 has the actual error text.
- **A JS array is already sparse and hash-backed underneath.** `List<T>` really is a
  block of memory, which is why the performance table is what it is.
- **`.map`/`.filter` in JS allocate a new array eagerly. LINQ's `Select`/`Where` are
  lazy** — they build a description of the work and do nothing until something walks
  the result. That difference is the source of the re-enumeration trap above.

---

## 11. How a senior thinks about this

- **Expose the narrowest type that works.** Return `IReadOnlyList<T>` from a property,
  take `IEnumerable<T>` as a parameter when you only walk it once, and keep `List<T>`
  as a private implementation detail. The type in the signature is a promise; a wide
  promise is expensive to narrow later, because callers will have started relying on it.

- **Pre-size when the count is known.** `new List<T>(capacity)` costs one line and
  removes every reallocation. Free when you know the number, worthless when you don't —
  do not guess.

- **Never mutate a collection while iterating it.** Collect what you want to change
  into a second list, then apply the changes after the loop ends.

- **Prefer returning a view over a copy** — copying a thousand-element list on every
  property read is a real cost, and a copy also silently discards writes the caller
  thought were doing something. But know that a view is not a guarantee (traps below).

- **What a senior checks first when reading unfamiliar code:** whether a collection
  property is exposed as a mutable type. A `public List<T>` on an entity is one of the
  loudest smells in a C# codebase, because it means the invariants that type claims to
  enforce can be walked around by any caller who feels like it.

- **What a senior refuses to add yet:** a `Dictionary` keyed by gamer tag, before
  anything actually needs to look a player up by tag. Five players in a list is
  searchable by walking it, and the index is only a cost until there is a query to
  justify it.

---

## 12. Traps

- **Integer division is silent.** `total / count` where both are `int` truncates and
  nothing warns you. The `(double)` cast goes on an **operand**, not on the result —
  `(double)(a / b)` is already too late. Section 5.

- **A read-only view is a compile-time guarantee, not a runtime one.** The object behind
  an `IReadOnlyList<Player>` is still the real `List<Player>`, and a determined caller
  can cast it back:
  ```csharp
  ((List<Player>)team.Players).Add(sixth);   // compiles, and works
  ```
  It stops accidents, not attacks. If it must be airtight, return a copy or wrap in
  `_players.AsReadOnly()`.

- **A read-only view is also still *live*.** `IReadOnlyList<T>` means the caller cannot
  change it, not that it cannot change. If the team adds a player, everyone holding the
  view sees a longer list.

- **Mutating while iterating throws.** Removing an item inside a `foreach` over the same
  list:
  ```csharp
  foreach (string tag in roster)
  {
      if (tag == "Gumayusi") { roster.Remove(tag); }
  }
  ```
  ```
  InvalidOperationException: Collection was modified; enumeration operation may not execute.
  ```
  The enumerator carries a version number; `Remove` bumps it; the next `MoveNext` sees
  the mismatch and refuses. This is a *runtime* failure, so it only shows up when that
  branch actually runs.

- **`Count` vs `Count()`.** Property is free, method may walk the whole sequence.
  Section 6.

- **`Capacity` is not `Count`.** `new List<Player>(5)` creates an **empty** list with
  room for five. `Count` is `0`. Passing a size does not create items.

- **The same object in two collections is still one object.** Adding one `Player` to two
  teams does not clone it. A rating change moves both rosters. See
  [memory-and-references.md](memory-and-references.md).

- **`List<T>` is not thread-safe.** Two threads adding at once can corrupt it, silently.
  Not a problem yet — it becomes one the moment a web request handler touches shared
  state, around Day 9.

---

## 13. Where this reappears

- **LINQ** replaces most hand-written loops. The `foreach` in
  [Team.cs:61](../../src/Esports.Console/Team.cs#L61) collapses to
  `_players.Average(p => p.Rating)` — and the integer-division trap disappears with it,
  because `Average` returns a `double` by construction. This is the part of C# closest
  to what you already write.

- **`IEnumerable<T>` becomes load-bearing around Day 13**, when EF Core makes queries
  lazy. At that point a sequence is not a list in memory — it is an unsent SQL query,
  and the moment you enumerate it decides when the database is hit. The
  re-enumeration leak in section 9 stops being a curiosity and starts being a
  performance bug.

- **`Dictionary<K,V>`** arrives when something needs a keyed lookup — a standings table
  keyed by team, most likely.

- **Aliasing**, the reason section 8 exists, becomes the change tracker's whole job when
  the database lands on Day 10: two variables pointing at one object becomes two
  variables pointing at one database row.

---

*Introduced: [Day 2](../days/day-02-collections-and-references.md)*
