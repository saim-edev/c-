# Abstract classes, and when to use one instead of an interface

**The one-line version:** an interface says *"you must be able to do X"*. An abstract
class says *"you must be able to do X, and here is the part everybody does the same
way"*.

---

## The problem it solves

Two classes fill the same interface and both start with the same code:

```csharp
// in RoundRobinFormat
if (teams.Count < 2)
{
    throw new ArgumentException($"Round robin needs at least 2 teams, got {teams.Count}");
}

// in SingleEliminationFormat
if (teams.Count < 2)
{
    throw new ArgumentException($"Single elimination needs at least 2 teams, got {teams.Count}");
}
```

Duplication is the small problem. **The real problem is that nothing forces a third
implementation to include it.** Someone writes `SwissFormat`, forgets the check, and it
ships.

An interface cannot help. It holds no code — it is a shape.

---

## The fix

[TournamentFormat.cs](../../src/Esports.Console/TournamentFormat.cs):

```csharp
public abstract class TournamentFormat : ITournamentFormat
{
    public abstract string Name { get; }

    // shared, written once
    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2)
        {
            throw new ArgumentException(
                $"{Name} needs at least 2 teams, got {teams.Count}");
        }

        return BuildFixtures(teams);
    }

    // a hole - no body here
    protected abstract List<Match> BuildFixtures(IReadOnlyList<Team> teams);
}
```

```
   GenerateMatches(teams)          <- in the BASE, written once
   │
   ├── if (teams.Count < 2) throw  <- shared. Cannot be skipped.
   │
   └── BuildFixtures(teams)        <- A HOLE. No body at this level.
              │
       ┌──────┴──────┐
       ▼             ▼
   RoundRobin    SingleElimination
```

The base controls the **order**. The subclass fills the **gap**. The pattern is called
**template method** and it is everywhere once you can see it.

---

## What the compiler actually enforces

Verified by building each one, not assumed.

**You cannot instantiate an abstract class:**

```csharp
TournamentFormat f = new TournamentFormat();
```
```
error CS0144: Cannot create an instance of the abstract type or
              interface 'TournamentFormat'
```

**A subclass cannot skip an abstract member:**

```csharp
public class BadFormat : TournamentFormat { }
```
```
error CS0534: 'BadFormat' does not implement inherited abstract member
              'TournamentFormat.BuildFixtures(IReadOnlyList<Team>)'
error CS0534: 'BadFormat' does not implement inherited abstract member
              'TournamentFormat.Name.get'
```

**The hole is not reachable from outside:**

```csharp
new RoundRobinFormat().BuildFixtures(teams);
```
```
error CS0122: 'RoundRobinFormat.BuildFixtures(IReadOnlyList<Team>)' is
              inaccessible due to its protection level
```

That last one matters. If `BuildFixtures` were public, a caller could skip the
validation entirely — the same failure as
[`MatchScore.Create` being bypassable by `new`](debugging.md). **A guard is only as
good as its doors.**

---

## Interface or abstract class?

| | `interface` | `abstract class` |
|---|---|---|
| Can hold code | no | **yes** |
| Can hold data / fields | no | yes |
| Can have a constructor | no | yes |
| How many per class | **many** | **exactly one** |
| Says | "you must be able to do X" | "you must do X, and here is the shared part" |
| Relationship | *can-do* | *is-a-kind-of* |

**Choosing:**

- **No shared code?** Interface.
- **Shared code, and the implementations are genuinely the same kind of thing?**
  Abstract class.
- **Not sure?** Interface. It is the less committal choice, and a class can have many.

**The real cost of the abstract class** is the one-per-class limit. A class can
implement five interfaces and inherit from exactly one. Spend that slot carefully.

**Having both, as here, is common.** `Tournament` depends on the *interface*, not the
base class — depend on the narrowest thing that works. A future format could implement
`ITournamentFormat` without inheriting `TournamentFormat`.

Honest note: with only two formats, both inheriting the base, the interface is barely
earning its place. Keeping it costs one file and preserves an option. Deleting it is a
decision that can be made later with better information; un-deleting is harder.

---

## What goes in the base, and what does not

The deciding question: **is this true of every kind, or only this kind?**

```csharp
// BASE - true of every tournament
if (teams.Count < 2) { throw ...; }

// SUBCLASS - true only of knockouts. A three-team league is fine.
if (teams.Count % 2 != 0) { throw ...; }
```

Get this wrong in either direction and it hurts:

- Too little in the base → duplication comes back.
- Too much in the base → you constrain implementations that should not be constrained,
  and the next format has to fight the base class.

---

## Access modifiers, narrowest first

| | Who can see it |
|---|---|
| `private` | this class only |
| `protected` | this class **and** anything inheriting from it |
| `public` | everyone |

`protected` is the one that arrives with abstract classes. The reflex to build: **what
is the narrowest access that works?** `public` is the default people reach for and is
almost always too wide.

---

## Coming from functional programming

**This is the concept with no clean analogue**, and that is worth sitting with rather
than working around.

| C# | Closest thing |
|---|---|
| `interface` | a typeclass. Clean mapping. |
| `abstract class` | a signature + defaults + state. Loosely a module signature with defaults. Poor fit. |
| inheritance | nothing really |

**The leaks:**

1. **One parent only, forever.** Many interfaces, one base class.
2. **The base reaches into the subclass.** `GenerateMatches` calls `BuildFixtures`,
   which does not exist at that level. The base is written against a hole it trusts
   someone else to fill. No equivalent among plain functions.
3. **Deep hierarchies become unreadable.** Four levels means reading four files to
   understand one class, and a base change ripples through every descendant.

**Treat the missing analogue as a hint.** Inheritance is the part of OOP that ages
worst. **Prefer handing something in over inheriting from something** — `Tournament`
*takes* a format rather than *being* one. That is composition, and it stays flexible
where inheritance locks you in.

**One level is usually enough.**

---

## Traps

- **`override` is required.** Forgetting it does not silently work; the compiler warns
  you are hiding an inherited member.
- **Access must match.** `protected` in the base stays `protected` in the subclass.
  Widening to `public` reopens the door the base closed.
- **Never call an abstract method from a base constructor.** The subclass's own fields
  are not initialised yet, so it runs against half-built state. This is a genuine trap
  and the reason some codebases ban virtual calls in constructors outright.
- **Do not put a non-universal rule in the base.**
- **`virtual` vs `abstract`:** `abstract` = no body, must override. `virtual` = has a
  body, may override. Neither is used here yet; `virtual` arrives when a base wants a
  default that subclasses can replace.
- **`sealed`** on a class stops anyone inheriting from it. Worth knowing; it is how you
  say "this hierarchy ends here".

---

## Where you meet this next

You will spend most of your time on the **subclass** side of this arrangement:

- **Day 9:** API controllers inherit from `ControllerBase`, which supplies `Ok()`,
  `NotFound()`, `BadRequest()` and the request context. You write the holes.
- **Day 11:** `DbContext` is a class you inherit from. Your `EsportsDbContext` overrides
  `OnModelCreating` — an abstract hole in exactly this shape.

Knowing what a base class is doing for you is the difference between using a framework
and being confused by one.

---

*Introduced: [Day 6](../days/day-06-abstract-classes.md). Related:
[interfaces-and-polymorphism](interfaces-and-polymorphism.md),
[thinking-like-a-backend-dev](thinking-like-a-backend-dev.md),
[THE-BIG-PICTURE](../THE-BIG-PICTURE.md).*
