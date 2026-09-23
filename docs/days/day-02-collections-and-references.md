# Day 2 — Collections, and what a variable actually holds

> **Goal:** Hold many players in one variable, and understand why two variables can point at the *same* object.
> **Date:** 2026-09-22 · **Tag:** `day-02`

---

## 0. Where we were

[Day 1](day-01-types-classes-encapsulation.md) produced a single
[Player](../../src/Esports.Console/Player.cs) class — four pieces of data, a
constructor, and three methods. Its rating could only change through `RecordWin()` and
`RecordLoss()`, because `private set` closed the door on everything else.

**What was missing:** a player belongs to a team, and a team has five of them. There
was no way to hold more than one of anything. Five separate `Player1..Player5` fields
would have been Day 1's loose-variables problem all over again, one level up.

*Previous: [Day 1 — Types, classes, encapsulation](day-01-types-classes-encapsulation.md)*

---

## 1. What we built today

[Team.cs](../../src/Esports.Console/Team.cs) — a team with a roster stored in a
`List<Player>`, a five-player limit enforced inside the class, and two computed
properties (`PlayerCount`, `AverageRating`).

Then [Program.cs](../../src/Esports.Console/Program.cs) became a demonstration of the
difference between **value types** and **reference types** — the single biggest
behavioural surprise coming from functional programming.

---

## 2. The flow

1. **A team needs five players.** Five separate `Player1..Player5` fields would be
   yesterday's loose-variables problem one level up, and couldn't answer "how many
   players?" without null-checking each field.
2. **`List<Player>` is one variable holding many.** The `<Player>` part is the element
   type, enforced by the compiler.
3. **The roster limit went inside `AddPlayer`** — same pattern as yesterday's
   `private set`, so it can't be bypassed.
4. **Then: `Player b = a;` followed by `b.RecordWin();` changed `a` too.**
5. **Because `a` never held the player.** It held the *address* of a player. Copying
   the variable copied the address, not the object.

---

## 3. How it works behind the scenes

### What the variable actually holds

```csharp
int x = 10;             // x holds the NUMBER 10
Player a = new Player(...);  // a holds the ADDRESS of an object elsewhere
```

```
int:                          Player:

  x  [ 10 ]                     a  [ →──┐
  y  [ 10 ]   (a copy)          b  [ →──┤   (a copy of the ARROW)
                                        │
                                        ▼
                                  ┌───────────────┐
                                  │ GamerTag Faker│
                                  │ Rating   1872 │
                                  └───────────────┘
                                   ONE object
```

Proof, not inference — the program printed:

```
a.Rating = 1872
b.Rating = 1872
Same object? True
```

`ReferenceEquals(a, b)` asks the blunt question "are these literally the same object?"
and answered `True`.

### Which types copy, which point

| Copies the value | Copies an arrow |
|---|---|
| `int`, `double`, `bool`, `char` | **any `class` you write** |
| `DateTime`, `decimal` | `List<T>`, arrays |
| `enum`, `struct` | `string`* |

\* `string` is technically a reference type, but it is **immutable** — nothing can
change it, so aliasing can never hurt you. This is precisely why strings are immutable
in most languages.

### A computed property runs code on every read

[Team.cs:41](../../src/Esports.Console/Team.cs#L41):

```csharp
public int PlayerCount
{
    get { return _players.Count; }
}
```

There is no stored `PlayerCount` anywhere. It looks like a field to callers but is a
function. That is the whole point of properties — C# code can look like it is touching
data while actually running code.

---

## 4. Why it's needed

**Why a `List<T>`:** one variable, any number of items, a real `Count`, and the
element type checked at compile time.

**Why `_players` is `private` and not `private set`:** this distinction is the
difference between a rule and a suggestion.

- `private set` → outsiders can **read** the list. Once they hold the `List<Player>`,
  they hold the arrow to the real roster and can `.Add()` straight past the limit.
- `private` → outsiders cannot see it at all.

After today's aliasing demo this isn't a style opinion — it follows directly from what
a variable holds.

**Why aliasing matters at all:** a method taking a `class` parameter can reach back and
change your object. A method taking an `int` cannot.

```csharp
void Boost(int rating) { rating += 25; }   // caller's value unchanged
void Boost(Player p)   { p.RecordWin(); }  // caller's player IS changed
```

---

## 5. Where it fits

`Team` is the second entity of the tournament engine. `Match` will pair two teams and
`Tournament` will organise matches.

The aliasing lesson is load-bearing for everything after it: when the database arrives
on Day 10, "two variables pointing at one object" becomes "two variables pointing at
one database row", and the change tracker's whole job is built on that idea.

---

## 6. Coming from functional programming

**The analogy:**
`List<T>` is the sequence type you reach for by default. `foreach` is a fold whose
accumulator you mutate. `ReferenceEquals` is physical equality — `==` in OCaml,
`reallyUnsafePtrEquality#` in Haskell.

**Where the analogy breaks — three ways, all important:**

1. **`List<T>` is not a cons list.** It is a growable array.

   | Operation | Linked list | C# `List<T>` |
   |---|---|---|
   | Add to end | O(1) | O(1) |
   | Add to **front** | O(1) | **O(n)** — everything shifts |
   | Get item #5000 | O(n) | **O(1)** |

   The performance instincts built on linked lists are backwards here.

2. **`Add` mutates in place.** It does not hand back a new list. There is no structural
   sharing and no persistence.

3. **Aliasing is the real hazard.** In a world where everything is immutable, the
   difference between "a copy of the value" and "a copy of the reference" is invisible,
   because nobody can change either one. Here it is the default and it is observable.
   **This is the one instinct that must actively change.**

---

## 7. New C# syntax I met today

| Syntax | Means |
|---|---|
| `List<Player>` | a list whose every element is a `Player` |
| `new List<Player>()` | creates an empty one |
| `_players.Add(player)` | appends — **mutates the list in place** |
| `_players.Count` | how many items (a property, not a method — no parentheses) |
| `foreach (Player p in _players)` | walk every item, one at a time |
| `if (x >= 5) { ... }` | a condition |
| `return;` | leave the method now, hand nothing back |
| `(double)total` | a cast — convert before using |
| `ReferenceEquals(a, b)` | are these literally the same object? |
| `private List<Player> _players` | a **field**, not a property. `_` prefix is the convention for private fields. |

---

## 8. Traps and gotchas

- **Integer division silently truncates.** [Team.cs:70](../../src/Esports.Console/Team.cs#L70):
  ```csharp
  return (double)total / _players.Count;   // 1806.2
  return total / _players.Count;           // 1806  ← wrong, and SILENT
  ```
  Both are `int`, so C# does integer division and throws the remainder away. No error,
  no warning, just a quietly wrong number. The `(double)` cast is what saves it.

- **Handing out a `List<T>` hands out the arrow.** A public `List<Player>` means any
  caller can `.Add()` past your limit. `IReadOnlyList<Player>` is the usual fix — not
  yet applied here, coming up.

- **`Count` is a property, `Count()` is a LINQ method.** They are different things that
  do the same job. `Count` (no parens) on a `List<T>` is free; `Count()` may walk the
  whole sequence.

- **The same object in two collections is still one object.** Adding `shared` to two
  teams did not clone it. Both rosters moved when it changed.

---

## 9. Checkpoint questions

1. **Q:** `Player b = a; b.RecordWin();` — why did `a.Rating` change?
   **A:** `a` never held the player, it held the player's address. `b = a` copied the
   address. One object, two names.

2. **Q:** Same shape with `int x = 10; int y = x; y += 5;` leaves `x` at 10. Why the
   difference?
   **A:** `int` is a value type — the variable holds the number itself, so `y` got a
   copy of the value rather than a copy of a pointer.

3. **Q:** Why is `_players` declared `private` rather than `private set`?
   **A:** `private set` would still let outsiders read the list, and a readable
   `List<T>` can be `.Add()`-ed to — bypassing the roster limit. `private` hides it
   entirely.

4. **Q:** `total / _players.Count` where both are `int` and the real answer is 1806.2.
   What do you get and why?
   **A:** `1806`. Integer division discards the remainder, silently.

5. **Q:** `string` is a reference type. Why does aliasing never cause bugs with it?
   **A:** It's immutable — nothing can modify a string, so sharing one is always safe.

---

## 10. Commands I ran

```powershell
dotnet run --project src/Esports.Console
```

---

## 11. What's next

Working and committed:
- [Player.cs](../../src/Esports.Console/Player.cs) — encapsulated, constructor, methods
- [Team.cs](../../src/Esports.Console/Team.cs) — roster in a `List<Player>`, limit enforced
- Aliasing understood and demonstrated

**Next:** comparing two players. `==` on a class does **not** do what you expect, and
the reason follows directly from today's aliasing lesson.

**Still open:** `_players` should be exposed as `IReadOnlyList<Player>` rather than not
exposed at all — a team you can't enumerate isn't much use.
