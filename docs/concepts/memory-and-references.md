# What a variable actually holds — value types, reference types, aliasing

The one idea from the first week that changes how you read every other line of C#.

---

## The problem

Two lines. Nothing in them looks like a mutation of `a`.

```csharp
Player a = new Player("Faker", 1847);
Player b = a;

b.RecordWin();

Console.WriteLine($"a.Rating = {a.Rating}");
Console.WriteLine($"b.Rating = {b.Rating}");
```

```
a.Rating = 1872
b.Rating = 1872
```

`a` was never touched. `a` changed anyway.

If you come from a language where values are immutable, this is not a small surprise —
it breaks the assumption that a name refers to a thing only you can affect. There is no
bug here and no clever feature. `b = a` simply did not do what it looks like it does.

---

## What a variable holds

A variable is a small labelled box. The question is what goes in the box.

For some types the box holds **the value itself**. For others the box holds **an
address** — a number saying "the real thing lives over there".

```csharp
int    x = 1847;                       // the box holds 1847
Player a = new Player("Faker", 1847);  // the box holds an ADDRESS
```

```
  VALUE TYPE                     REFERENCE TYPE

   x [ 1847 ]                      a [ →──┐
                                          │
   assign:                        assign: │
   int y = x;                     Player b = a;
                                          │
   y [ 1847 ]                      b [ →──┤
                                          │
   TWO boxes, TWO numbers.                ▼
   y += 25 changes y only.          ┌──────────────────┐
                                    │ GamerTag  Faker  │
                                    │ Rating    1847   │
                                    └──────────────────┘
                                    TWO boxes, ONE object.
                                    b.RecordWin() changes the
                                    object BOTH arrows point at.
```

That is the whole mechanism. `=` always copies **the contents of the box**. For an
`int` the contents are a number, so you get a second number. For a `Player` the contents
are an address, so you get a second copy of the address — and one object with two names.

Two names for one object is called **aliasing**.

`new` is the word that creates an object. One `new` means one object, no matter how many
variables end up pointing at it.

```csharp
Player a = new Player("Faker", 1872);
Player c = new Player("Faker", 1872);   // a second `new` → a second object
```

```
   a [ →──┐                c [ →──┐
          ▼                       ▼
   ┌──────────────┐        ┌──────────────┐
   │ Faker  1872  │        │ Faker  1872  │
   └──────────────┘        └──────────────┘
   Two objects. Same contents. Different addresses.
```

---

## Which types are which

| Value type — the box holds the value | Reference type — the box holds an address |
|---|---|
| `int`, `long`, `short`, `byte` | every `class` you write — `Player`, `Team`, `Match` |
| `float`, `double`, `decimal` | every `record` you write — `MatchScore` |
| `bool`, `char` | `List<T>`, arrays (`int[]`, `Player[]`), `Dictionary<K,V>` |
| `DateTime`, `TimeSpan`, `Guid` | `object`, and every interface type |
| every `enum` — `MatchState` | delegates and lambdas |
| every `struct`, and `record struct` | `string` — but see below |
| a nullable value type — `int?` | |

The rule underneath the table: **`struct` and `enum` are value types; `class` and
`record` are reference types.** Everything else follows. `int` is a `struct` in the base
library, `MatchState` is an `enum`, `Player` is a `class`.

### `string` — the special case that hides the rule

`string` is a reference type. Every string variable holds an address. And yet strings
never produce the surprise at the top of this file, for one reason:

> **A string cannot be modified. Ever. By anyone.**

There is no method anywhere that changes a string in place. `ToUpper()`, `Replace()`,
`Trim()`, `+` — every one of them builds a **new** string and hands it back. So two
variables sharing one string is perfectly safe: neither can do anything to it the other
could notice.

This is exactly why strings are immutable in C#, Java and JavaScript. They get shared
constantly, so making them unmodifiable deletes a whole class of bug.

C# also overrides `==` on `string` so it compares the characters rather than the
addresses. That is a second special case, layered on the first.

```csharp
string s1 = "T1";
string s2 = "T" + 1.ToString();     // built at runtime, so a separate object

Console.WriteLine($"s1 == s2               : {s1 == s2}");
Console.WriteLine($"ReferenceEquals(s1, s2): {ReferenceEquals(s1, s2)}");
```

```
s1 == s2               : True
ReferenceEquals(s1, s2): False
```

Two different objects, and `==` still says `True` — because for `string`, and only for
`string` among the everyday reference types, `==` was taught to look inside.

**The trap:** `string` is everyone's first reference type, so it teaches the wrong
lesson. `Player` behaves the way reference types actually behave. `string` is the
exception you happened to meet first.

---

## Method arguments — where this bites daily

Passing an argument *is* an assignment. The parameter is a new box and it gets a copy of
the caller's box contents. Same rule, same two outcomes.

```csharp
static void BoostValue(int rating)  { rating += 25; }
static void BoostObject(Player p)   { p.RecordWin(); }

int score = 100;
BoostValue(score);
Console.WriteLine($"after BoostValue(score)  : {score}");

Player faker = new Player("Faker", 1847);
BoostObject(faker);
Console.WriteLine($"after BoostObject(faker) : {faker.Rating}");
```

```
after BoostValue(score)  : 100
after BoostObject(faker) : 1872
```

```
  BoostValue(score)                  BoostObject(faker)

  caller  [ 100 ]                    caller  [ →──┐
             │ copies the NUMBER                  │ copies the ADDRESS
             ▼                                    ▼
  method  [ 100 ] ──► 125            method  [ →──┤
                                                  │
  Two separate numbers.                           ▼
  The caller's 100 is untouched.        ┌──────────────────┐
                                        │ Faker    1872    │
                                        └──────────────────┘
                                     ONE player. The caller sees it change.
```

As a rule you can apply without thinking:

- A method taking a **value type** cannot affect the caller's variable.
- A method taking a **reference type** can reach back and change the caller's object.

Note precisely what the method *cannot* do in the second case: it cannot make `faker`
point somewhere else. `p = new Player(...)` inside `BoostObject` would only repoint the
method's own box. It can change the object; it cannot change which object the caller is
looking at. (`ref` and `out` lift that restriction — a later topic.)

---

## `ReferenceEquals` — the blunt question

`==` means different things on different types: addresses on a `class`, contents on a
`record`, characters on a `string`. When you need to cut through all of that and ask the
one physical question — *are these two names for one object?* — there is a method that
only ever asks that.

```csharp
Player a = new Player("Faker", 1847);
Player b = a;                            // same object, second name
b.RecordWin();

Player c = new Player("Faker", 1872);    // different object, same contents

Console.WriteLine($"ReferenceEquals(a, b) : {ReferenceEquals(a, b)}");
Console.WriteLine($"a == c                : {a == c}");
Console.WriteLine($"ReferenceEquals(a, c) : {ReferenceEquals(a, c)}");
```

```
ReferenceEquals(a, b) : True
a == c                : False
ReferenceEquals(a, c) : False
```

`ReferenceEquals` cannot be overridden, so it cannot lie to you. It is the tool for
answering "is this the same object, or a copy?" while debugging. On value types it is
not useful — see the traps at the end.

What `==` does on each type is covered in
[classes-vs-records.md](classes-vs-records.md).

---

## The consequence in this codebase

Here is the line this whole idea pays for —
[Team.cs:17](../../src/Esports.Console/Team.cs#L17):

```csharp
// A List<Player> is ONE variable holding MANY players.
// <Player> is the element type - the compiler will reject anything else.
//
// `private` here, not `private set`. The difference matters:
//   private set  -> outsiders can READ the list, and could then .Add() to it
//   private      -> outsiders cannot see this field at all
private List<Player> _players = new List<Player>();
```

The rule it protects lives in [Team.cs:30](../../src/Esports.Console/Team.cs#L30):

```csharp
public void AddPlayer(Player player)
{
    if (_players.Count >= 5)
    {
        Console.WriteLine($"  [rejected] {Tag} roster is full - cannot add {player.GamerTag}");
        return;
    }

    _players.Add(player);
}
```

### Why not `private set`

`private set` is the pattern on [Player.cs:12](../../src/Esports.Console/Player.cs#L12)
and on [Team.cs:7](../../src/Esports.Console/Team.cs#L7) — read freely, write only from
inside. It is the right instinct, and it is the wrong tool here, because of what a
variable holds.

```csharp
public List<Player> Players { get; private set; }   // "read-only", supposedly
```

```
   caller's variable                 Team's field
        │                                 │
        └────────────►  ┌───────────┐ ◄───┘
                        │ the real  │
                        │  roster   │
                        └───────────┘
        ONE List object, TWO arrows
```

A getter that returns a `List<Player>` hands back **the address of the real roster**.
The caller now holds the same list the `Team` holds. And `List<T>` is mutable:

```csharp
t1.Players.Add(new Player("Bench", 1500));   // a 6th player. No error.
```

The private setter stopped nobody, because nobody needed the setter. They never
*replaced* the list — they reached through the arrow and changed the list already there.
The five-player limit in `AddPlayer` becomes decoration: a locked door beside an open
window.

`private` closes the window. Outsiders cannot name `_players` at all, so the only route
onto the roster is `AddPlayer`, and the limit is real.

### What `IReadOnlyList<Player>` does and does not buy

The obvious next step is to expose a read-only *view*:

```csharp
public IReadOnlyList<Player> Players { get { return _players; } }
```

**What it gives you:** the type has no `Add`, `Remove`, `Clear` or indexer setter, so
the ordinary mistake stops compiling. That is genuinely worth having, and it is where
this codebase is heading.

**What it does not give you — three leaks, in order of how likely they are to matter:**

1. **The elements are still reference types.** The view protects the *list*, not the
   players in it. `team.Players[0].RecordWin()` goes straight through and changes the
   real player.

2. **It is a view, not a snapshot.** `IReadOnlyList<Player>` is an interface that the
   real `List<Player>` implements — the same object, seen through a narrower window.
   Later calls to `AddPlayer` change what the caller is holding, under their feet. If
   they are mid-`foreach` over it, they get
   `InvalidOperationException: Collection was modified`.

3. **It can be cast back.** The object really is a `List<Player>`, so a cast reopens the
   full interface:

   ```csharp
   List<Player> smuggled = (List<Player>)team.Players;
   smuggled.Add(new Player("Bench", 1500));    // compiles, runs, works
   ```

   `IReadOnlyList<T>` is a statement of intent enforced by the compiler, not a wall
   enforced at runtime. It stops accidents, not determination — the right trade for
   internal code, and worth knowing before relying on it for anything else.

The genuine wall is handing back a copy — `_players.ToList()` — and a copy costs
memory on every call, so it is a decision rather than a default.

| Exposure | Can add past the limit? | Can mutate a player? | Cost |
|---|---|---|---|
| `public List<Player>` | yes, trivially | yes | none |
| `private set` on a `List<Player>` | yes, trivially | yes | none |
| `IReadOnlyList<Player>` | only by casting | yes | none |
| `_players.ToList()` — a copy | no | yes | a new array per call |
| `private`, nothing exposed | no | no | you cannot enumerate the team |

---

## Coming from functional programming

**The analogy that works:** the box-holds-an-address model is not new to you. A value in
a functional language is usually a pointer to a heap object too — a list, a record, a
closure. The runtime has always worked this way. `Player b = a;` is physically the same
operation as `let b = a`.

**Where it leaks:** in an immutable world the distinction is *unobservable*. Whether `b`
got a copy of the value or a copy of the pointer, no program can tell, because nothing
can change either one. Sharing is a pure optimisation the runtime performs behind your
back, and you were right never to think about it.

Here the same sharing still happens, and it is now **observable**, because objects have
mutable state. The thing your language guaranteed you would never have to reason about
is the default, and the reasoning is now yours.

Three consequences that follow directly:

1. **`==` on a class compares addresses, not contents.** Two structurally identical
   `Player`s are not equal. Structural equality is what `record` restores —
   [classes-vs-records.md](classes-vs-records.md).
2. **There is no `with`-style copy for a class.** Handing an object to a function hands
   it the original, every time.
3. **Storing an object in two places stores one object twice.** Adding the same `Player`
   to two teams does not clone it; both rosters see every rating change.

---

## Coming from JavaScript

You already know this model — JavaScript makes exactly the same split, and you have been
relying on it for years:

```javascript
const a = { tag: "Faker", rating: 1847 };
const b = a;
b.rating += 25;
console.log(a.rating);   // 1872 — same object

let x = 1847;
let y = x;
y += 25;
console.log(x);          // 1847 — numbers are copied
```

| JavaScript | C# |
|---|---|
| number, string, boolean — copied | `int`, `double`, `bool`, `char` — copied |
| object, array, function — shared | every `class`, `record`, `List<T>`, array — shared |
| `Object.is(a, b)` | `ReferenceEquals(a, b)` |
| `{...team}` — a shallow copy | `_players.ToList()` — a shallow copy |
| `Object.freeze(arr)` — shallow, runtime | `IReadOnlyList<T>` — shallow, compile-time |

Two differences worth naming:

- **JavaScript strings are primitives; C# strings are objects.** The observable
  behaviour is identical — both immutable, both compared by content — but the reason
  differs, so the mental model has to rest on immutability, not on "primitive".
- **A C# `struct` has no JavaScript equivalent.** `DateTime`, `Guid` and any `struct`
  you write are objects in shape and values in behaviour: assigning one copies the whole
  thing, field by field. Nothing in JS does that.

This is also why React insists you never mutate state and always build a new object.
That rule exists *because* of aliasing: React compares by reference, so mutating in
place leaves the reference unchanged and the re-render never fires. You have been paying
aliasing tax already — just under a different name.

---

## How a senior thinks about it

The habit is one question, asked every time an object is passed somewhere or stored in a
second place:

> **Am I sharing this, or do I want a separate one?**

There is no default answer. There are three, and you pick:

1. **Share deliberately.** A `Player` on a roster *should* be the same object the
   tournament holds — one rating, everywhere. For entities, sharing is usually right.
2. **Copy defensively.** When the caller must not be able to reach your internals, hand
   them a copy instead of the original. That is *defensive copying*:
   ```csharp
   public List<Player> GetRoster() { return _players.ToList(); }  // new list,
                                                                  // same players
   ```
   Note what the copy covers and what it does not: the new list is yours, but the arrows
   inside it still point at the same `Player` objects. That is a **shallow copy**, and
   it is almost always what you want — a *deep* copy would clone the players too, so the
   roster would stop reflecting rating changes, which is usually a bug rather than a
   feature.
3. **Make it immutable instead.** If nothing can change the object, sharing is free and
   you can stop thinking about it. That is why `MatchScore` is a `record` and why
   `string` is the way it is. Cheapest of the three when the type qualifies.

When is defensive copying worth it? Three signals:

- The value crosses a **boundary you do not control** — a public API, a library, a
  method that takes a caller-supplied collection.
- The object is **stored, not just read**. A constructor that keeps a `List<T>` the
  caller passed in has handed the caller a permanent handle into its own state.
- The **cost is bounded and known**. Copying a five-player roster on demand is free.
  Copying ten thousand rows on every property read is a performance bug you wrote on
  purpose.

And what a senior does *not* do: copy everything to feel safe. Copies cost memory, and
they cost correctness — every copy is a place where two things that were meant to be one
can silently drift apart.

The smell to watch for: a class exposing a mutable collection. When you see
`public List<T>` on a type that has rules about that list, the rules are already broken.
You just have not met the bug yet.

---

## Where this reappears

**Day 10 — the database.** These objects become rows, and aliasing becomes Entity
Framework's **change tracker**. EF loads a row into a `Player` object and keeps its own
reference to that same object. Call `faker.RecordWin()` and you have mutated the object
EF is holding, so `SaveChanges()` emits an `UPDATE` — with no save call on the player
and no explicit "this changed" flag anywhere. That entire mechanism is `Player b = a` at
a larger scale, and it feels like magic right up until you remember what a variable
holds.

The same idea explains the two problems everyone meets there: loading the same row twice
hands you the *same object* back, and passing an entity out to a web layer that mutates
it writes to the database whether you meant it or not.

**Later, with `async`.** Two tasks holding one object mutate it at the same time. Every
threading hazard in the language starts from two names for one thing.

---

## Traps

- **`ReferenceEquals` on value types is always `False`.** Passing an `int` to a method
  taking `object` wraps it in a fresh object first (*boxing*), so each side gets its own
  wrapper. `ReferenceEquals(5, 5)` is `False`. The method is only meaningful on
  reference types.

- **`==` on a `class` compares addresses.** Two identical `Player`s are not equal — the
  single most common source of "why is my test failing".

- **A `record` is still a reference type.** Aliasing applies to a `record` exactly as it
  does to a `class`. Immutability is what makes it harmless, not the keyword.

- **`with` on a record is a shallow copy.** A record holding a `List<T>` shares that list
  with its copy, so both "immutable" records see the same mutations.

- **A `struct` copies on assignment, and surprises the other way.** Pass one to a method,
  set a field on it there, and the change lands on a copy and is silently discarded.

- **The same object in two collections is one object.** Add a `Player` to two teams and
  both rosters move when its rating changes. Sometimes exactly right, sometimes the bug.

- **Mutating a collection while `foreach`-ing it throws.**
  `InvalidOperationException: Collection was modified; enumeration operation may not
  execute.` Collect what you want to remove first, remove it after the loop.

- **`null` is an empty box.** A reference-type variable holding no address is `null`, and
  reaching through it throws `NullReferenceException`. A value type cannot be `null`
  unless written as `int?`.

---

*Introduced: [Day 2](../days/day-02-collections-and-references.md)*
