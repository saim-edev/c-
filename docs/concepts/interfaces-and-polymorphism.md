# Interfaces and polymorphism

**The one-line version:** an interface is a contract with no code in it. Several classes
can fill the same contract in different ways, and a caller can use any of them without
knowing which one it has.

---

## The problem it solves

You have one way of doing something. Then you need a second way.

```csharp
public void GenerateMatches()
{
    if (Format == TournamentFormat.RoundRobin)
    {
        // ... 6 lines ...
    }
    else if (Format == TournamentFormat.SingleElimination)
    {
        // ... 4 lines ...
    }
}
```

This compiles. It works. It is still wrong, and the reasons are worth knowing because
they show up in every codebase:

1. **The class now knows about every variant that will ever exist.** Every new format
   means editing this file again.
2. **The `if` chain spreads.** Formats differ in more than one place — fixtures,
   standings, reporting. Soon "how knockout works" is smeared across five files, and no
   single file tells you the whole story.
3. **You cannot test one variant alone.** You need the whole containing object in the
   right state first.
4. **Adding a variant means editing working code.** The thing that worked yesterday is
   now in your diff.

**The smell to recognise:** an `if` or `switch` on "what kind of thing is this",
repeated in more than one place.

---

## The shape of the fix

Take the varying knowledge out of the class that was holding it.

[ITournamentFormat.cs](../../src/Esports.Console/ITournamentFormat.cs):

```csharp
public interface ITournamentFormat
{
    string Name { get; }
    List<Match> GenerateMatches(IReadOnlyList<Team> teams);
}
```

No bodies. No code. Just: *whatever you are, you must be able to do these things.*

Then classes fill it — [RoundRobinFormat.cs:7](../../src/Esports.Console/RoundRobinFormat.cs#L7):

```csharp
public class RoundRobinFormat : ITournamentFormat
{
    public string Name => "Round robin";

    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        // everyone plays everyone
    }
}
```

`: ITournamentFormat` is a promise the compiler checks. Delete `Name` and the file
stops compiling.

And the holder never learns which one it got —
[Tournament.cs:69](../../src/Esports.Console/Tournament.cs#L69):

```csharp
public void GenerateMatches()
{
    if (_matches.Count > 0) { throw new InvalidOperationException(...); }

    _matches = _format.GenerateMatches(_teams);
}
```

**The test that it worked:** `Tournament.cs` does not contain the word `RoundRobin`.

---

## How the dispatch actually happens

```
   Tournament "LCK Spring"          Tournament "LCK Cup"
   ┌─────────────────────┐          ┌─────────────────────┐
   │ _format ──┐         │          │ _format ──┐         │
   └───────────┼─────────┘          └───────────┼─────────┘
               ▼                                ▼
   ┌──────────────────────┐         ┌──────────────────────┐
   │ RoundRobinFormat     │         │ SingleElimination... │
   │  GenerateMatches()   │         │  GenerateMatches()   │
   │  → 6 matches         │         │  → 2 matches         │
   └──────────────────────┘         └──────────────────────┘

   ONE call site. Different object. Different answer.
```

`_format` holds an **address**. The variable's declared type (`ITournamentFormat`)
decides *what you are allowed to call*. The object at that address decides *what
actually runs*.

Every object carries a pointer to a table of its own methods. An interface call follows
that pointer. That is the entire mechanism — there is no more to polymorphism than this
picture.

**Consequence worth remembering:** the decision is made when the line runs, not when it
compiles. The compiler cannot tell you which implementation will execute.

---

## One collection, many classes

```csharp
ITournamentFormat[] formats = [new RoundRobinFormat(), new SingleEliminationFormat()];

foreach (ITournamentFormat format in formats)
{
    List<Match> fixtures = format.GenerateMatches(teams);
    Console.WriteLine($"  {format.Name,-20} {fixtures.Count} matches");
}
```

```
  Round robin          6 matches
  Single elimination   2 matches
```

Two different classes in one array. The loop never asks which is which.

---

## When to reach for one — and when not to

**Do:**

| Situation | Why |
|---|---|
| You have a **second** implementation | the actual reason interfaces exist |
| You need to swap the real thing for a fake in a test | a fake email sender, an in-memory database |
| You want to hand a dependency in from outside | this is dependency injection, by hand |
| A caller should not know which variant it holds | shrinks what has to change later |

**Don't:**

| Situation | Why not |
|---|---|
| There is only one implementation and no test needs a fake | `IFooService` with exactly one `FooService` behind it is ceremony |
| You are guessing at future variants | you cannot see the right shape with one example |
| The variants differ by *data*, not *behaviour* | an enum or a config value is simpler |

**The order matters.** Round robin was written as a plain loop **inside** `Tournament`
first, and only pulled out when a second format appeared. You cannot see the right
abstraction with one example. Write the concrete thing, then extract.

This is the same principle as *don't add folders for six files* — see
[thinking-like-a-backend-dev](thinking-like-a-backend-dev.md).

---

## Design notes worth copying

**Ask for the narrowest thing that works.**

```csharp
List<Match> GenerateMatches(IReadOnlyList<Team> teams);
//                          ^^^^^^^^^^^^^^^^^^^^^^^^
```

A format works out pairings. It has no business adding or removing teams, so it does
not ask for a type that would let it.

**Put validation where the knowledge is.** Each format checks its own team count:

```
refused : knockout with 3 teams -> Single elimination needs an even number of teams, got 3
allowed : round robin with 3 teams
```

Three teams is a fine league and a broken knockout. Only the format knows that, so only
the format checks it.

**Never ask what the object really is.** If you write this, you have undone the whole
thing:

```csharp
if (_format is RoundRobinFormat) { ... }   // the point is NOT knowing
```

The one place type-checking is defensible is code *outside* the hierarchy that has to
format or display things. Even then, prefer adding a member to the contract.

---

## Coming from functional programming

**An interface is a typeclass.** The mapping is close to exact:

| C# | What you know |
|---|---|
| `interface ITournamentFormat` | a typeclass declaration |
| `class RoundRobinFormat : ITournamentFormat` | an instance |
| `_format.GenerateMatches(teams)` | dispatch to the right instance |
| handing the format to a constructor | passing a record of functions |
| `ITournamentFormat[]` | a list of existentially-quantified values |

**Where it leaks — four places:**

1. **Dispatch is at runtime.** Your instances are normally resolved by the compiler from
   the types at the call site. C# attaches a method table to each **object** and looks
   it up when the call runs. Same idea — dictionary passing — but the dictionary travels
   *with the value* rather than alongside it.
2. **A class must declare it implements the interface.** No orphan instances. You cannot
   retrofit an interface onto a type someone else wrote. (Extension methods get part of
   the way; they cannot be dispatched on.)
3. **No higher-kinded types.** You cannot write an interface over "any container", so
   there is no `IMonad<T>`, no `IFunctor<F>`.
4. **An interface stores nothing.** `string Name { get; }` declares that a name is
   readable; it does not hold one. Each class supplies its own storage.

---

## Traps

- **The `I` prefix is convention, not syntax.** Nothing enforces it. Everyone uses it.
- **An interface with one implementation is usually ceremony.** Wait for the second.
- **`if (x is ConcreteType)` inside the holder undoes the design.** If you need it, the
  contract is probably missing a member.
- **Interfaces hold no state** — declaring a property does not store a value.
- **Default interface methods exist** (C# 8+) and let an interface carry code. They
  blur the line with abstract classes and are rarely the right first tool. Mentioned so
  the syntax is not a surprise in someone else's codebase.

---

## Where this goes next

**Abstract classes.** Both formats currently duplicate the same `if (teams.Count < 2)`
check. An interface cannot help — it holds no code. An abstract class can hold shared
code *and* demand that each subclass supply its own pairing logic. That is the next
topic, and the comparison between the two is the point.

**Dependency injection, Day 10.** The database connection gets handed in exactly the
way the format is handed in here — the code that saves a `Player` will take a contract,
not a concrete Postgres class. Writing it by hand first is the best preparation for the
automated version.

**Testing.** An interface is what lets you pass a fake in place of the real thing. The
day tests arrive, this is the seam that makes it possible.

---

*Introduced: [Day 5](../days/day-05-interfaces-and-polymorphism.md). Related:
[classes-vs-records](classes-vs-records.md),
[thinking-like-a-backend-dev](thinking-like-a-backend-dev.md),
[THE-BIG-PICTURE](../THE-BIG-PICTURE.md).*
