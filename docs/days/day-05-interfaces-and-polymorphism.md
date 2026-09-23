# Day 5 — Interfaces: one call, two different answers

> **Goal:** Support two completely different tournament formats without `Tournament` knowing either of them exists.
> **Date:** 2026-09-23 · **Tag:** `day-05`

---

## 0. Where we were

[Day 4](day-04-enums-and-state-machines.md) finished
[Match](../../src/Esports.Console/Match.cs) — a fixture with a lifecycle it could not
cheat. But matches were still being made **by hand**, one line at a time:

```csharp
Match final = new Match(t1, gen);
final.Start();
final.RecordResult(3, 1);
```

**What was missing:** for eight teams you would write out every pairing yourself, and
you would have to know which pairings to write. Nothing answered the question a
tournament exists to answer — **who plays who?**

*Previous: [Day 4 — Enums and state machines](day-04-enums-and-state-machines.md)*

---

## 1. What we built today

- [Tournament.cs](../../src/Esports.Console/Tournament.cs) — holds teams, holds
  fixtures, and hands the "who plays who" question to somebody else.
- [ITournamentFormat.cs](../../src/Esports.Console/ITournamentFormat.cs) — a contract:
  *turn a list of teams into a list of matches*.
- [RoundRobinFormat.cs](../../src/Esports.Console/RoundRobinFormat.cs) — everyone plays
  everyone.
- [SingleEliminationFormat.cs](../../src/Esports.Console/SingleEliminationFormat.cs) —
  lose once and you are out.

---

## 2. The flow

### Step 1 — a tournament that generates its own fixtures

First version had the round-robin loop written directly inside
`Tournament.GenerateMatches()`. It worked:

```
4 teams registered
6 matches generated

  T1 vs GEN     T1 vs HLE     T1 vs DK
  GEN vs HLE    GEN vs DK     HLE vs DK
```

Four teams, six matches, every pair once.

**The trick is one character.** The inner loop starts at `i + 1`, not `0`:

```csharp
for (int i = 0; i < teams.Count; i++)
{
    for (int j = i + 1; j < teams.Count; j++)
    {
        matches.Add(new Match(teams[i], teams[j]));
    }
}
```

```
        j=0  j=1  j=2  j=3
   i=0    -   X    X    X       X = a match
   i=1    -   -    X    X       - = skipped
   i=2    -   -    -    X
   i=3    -   -    -    -
```

Starting `j` at `0` gives the whole grid: both "T1 vs GEN" **and** "GEN vs T1", plus
"T1 vs T1" down the diagonal. Starting at `i + 1` takes the upper triangle, which is
each pair exactly once.

### Step 2 — then a second format was needed, and the obvious fix was a trap

A knockout works completely differently. The obvious way to add it:

```csharp
public void GenerateMatches()
{
    if (Format == TournamentFormat.RoundRobin)
    {
        for (int i = 0; i < _teams.Count; i++)
            for (int j = i + 1; j < _teams.Count; j++)
                _matches.Add(new Match(_teams[i], _teams[j]));
    }
    else if (Format == TournamentFormat.SingleElimination)
    {
        for (int i = 0; i < _teams.Count / 2; i++)
            _matches.Add(new Match(_teams[i], _teams[_teams.Count - 1 - i]));
    }
}
```

This compiles and works. It is still the wrong answer, for four reasons:

1. **`Tournament` must know every format that will ever exist.** Swiss, double
   elimination, group stage — each means editing this file again. A class you edit
   every time you add a feature is a class you will be editing forever.
2. **The `if` chain spreads.** Formats differ in more than fixtures — round robin ranks
   by points, knockout has no table. So a second `if` appears in the standings code, a
   third in reporting. The knowledge of "how knockout works" ends up smeared across
   five files.
3. **You cannot test one format alone.** Testing knockout needs a whole `Tournament`
   in the right state.
4. **Adding a format means editing working code.** Round robin worked yesterday. Now
   you are in the same method and might break it.

### Step 3 — take the knowledge out of `Tournament`

The problem is that *how to generate fixtures* lives **inside** `Tournament`. So make
it a thing of its own — [ITournamentFormat.cs](../../src/Esports.Console/ITournamentFormat.cs):

```csharp
public interface ITournamentFormat
{
    string Name { get; }
    List<Match> GenerateMatches(IReadOnlyList<Team> teams);
}
```

That is a **contract**. It says: whatever you are, you must have a `Name`, and you must
be able to turn a list of teams into a list of matches. **It contains no code.** It is
a shape, not an implementation.

### Step 4 — two classes fill the shape differently

[RoundRobinFormat.cs:7](../../src/Esports.Console/RoundRobinFormat.cs#L7):

```csharp
public class RoundRobinFormat : ITournamentFormat
{
    public string Name => "Round robin";

    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2)
        {
            throw new ArgumentException(
                $"Round robin needs at least 2 teams, got {teams.Count}");
        }

        List<Match> matches = new List<Match>();

        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                matches.Add(new Match(teams[i], teams[j]));
            }
        }

        return matches;
    }
}
```

[SingleEliminationFormat.cs:6](../../src/Esports.Console/SingleEliminationFormat.cs#L6):

```csharp
public class SingleEliminationFormat : ITournamentFormat
{
    public string Name => "Single elimination";

    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2) { throw new ArgumentException(...); }

        // Odd count means somebody has nobody to play.
        if (teams.Count % 2 != 0)
        {
            throw new ArgumentException(
                $"Single elimination needs an even number of teams, got {teams.Count}");
        }

        List<Match> matches = new List<Match>();

        // Seeded: strongest plays weakest, so the best teams stay apart
        // until later rounds. Pairing 0 v 1 would knock out a finalist in
        // round one.
        int half = teams.Count / 2;

        for (int i = 0; i < half; i++)
        {
            matches.Add(new Match(teams[i], teams[teams.Count - 1 - i]));
        }

        return matches;
    }
}
```

`: ITournamentFormat` means "this class fills that contract". The compiler now checks
that `Name` and `GenerateMatches` both exist with exactly the right shape. Delete
either one and the file stops compiling.

### Step 5 — `Tournament` holds "some format" and never learns which

[Tournament.cs:16](../../src/Esports.Console/Tournament.cs#L16) and
[Tournament.cs:23](../../src/Esports.Console/Tournament.cs#L23):

```csharp
private readonly ITournamentFormat _format;

public Tournament(string name, ITournamentFormat format)
{
    Name = name;
    _format = format;
}
```

And [Tournament.cs:69](../../src/Esports.Console/Tournament.cs#L69) — **the whole point
of the day**:

```csharp
public void GenerateMatches()
{
    if (_matches.Count > 0)
    {
        throw new InvalidOperationException($"{Name} fixtures already generated");
    }

    _matches = _format.GenerateMatches(_teams);
}
```

**No `if`. No mention of round robin or knockout anywhere in the file.** Search
`Tournament.cs` for "RoundRobin" and it is not there. That is the test of whether this
worked.

---

## 3. How it works behind the scenes

### The result

```csharp
Tournament league = new Tournament("LCK Spring", new RoundRobinFormat());
Tournament cup    = new Tournament("LCK Cup",    new SingleEliminationFormat());
```

The only difference between those two lines is the object handed in.

```
=== LCK Spring (Round robin) ===
4 teams, 6 matches
  T1 vs GEN [Scheduled] not played yet
  T1 vs HLE [Scheduled] not played yet
  T1 vs DK [Scheduled] not played yet
  GEN vs HLE [Scheduled] not played yet
  GEN vs DK [Scheduled] not played yet
  HLE vs DK [Scheduled] not played yet

=== LCK Cup (Single elimination) ===
4 teams, 2 matches
  T1 vs DK [Scheduled] not played yet
  GEN vs HLE [Scheduled] not played yet
```

### What actually happens at that call

`_format` holds an **address**. At that address sits an object. The variable's
*declared type* is the contract; the *actual object* is one of the two classes.

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

   ONE call site: _format.GenerateMatches(_teams)
   Different object. Different answer.
```

When that line runs, the runtime looks at **the object**, not the variable, to decide
whose code to run. Each object carries a pointer to its own table of methods, and the
call follows that pointer.

**That is the whole mechanism.** It is called polymorphism and there is nothing more
to it than the diagram above.

### One loop, two classes

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

One array holding objects of two different classes. The loop never asks which is which
— it only uses what the contract promises.

### Each format owns its own rules

```
refused : knockout with 3 teams -> Single elimination needs an even number of teams, got 3
allowed : round robin with 3 teams
```

Three teams is fine for a league and broken for a knockout. The validation lives in the
format because **the format is the thing that knows**.

---

## 4. Why it's needed

| | With `if` inside `Tournament` | With the interface |
|---|---|---|
| Add a third format | edit `Tournament`, risk breaking the other two | write one new class, touch nothing |
| Where knockout rules live | spread across every file with an `if` | one file |
| Test one format alone | need a whole `Tournament` in the right state | call `GenerateMatches` directly |
| `Tournament` knows about | every format that will ever exist | nothing but the contract |

**The deeper reason:** `Tournament`'s real job is holding teams and fixtures and
enforcing registration rules. Working out pairings was never its job — it was just
sitting there because there was nowhere else to put it.

**The test that it worked:** `Tournament.cs` does not contain the word `RoundRobin`.

---

## 5. Where it fits

```
              Program.cs
                  │  hands a format in
                  ▼
            ┌───────────────┐
            │  Tournament   │  holds teams + fixtures
            │               │  enforces registration rules
            │   _format ────┼──────┐  knows only the CONTRACT
            └───────┬───────┘      │
                    │ holds        ▼
                    ▼        ┌──────────────────────┐
              List<Match>    │  ITournamentFormat   │ (an interface)
                    │        └──────────┬───────────┘
                    ▼            ┌──────┴───────┐
              ┌──────────┐       ▼              ▼
              │  Match   │  RoundRobin    SingleElimination
              └────┬─────┘   Format           Format
                   │ pairs
                   ▼
              ┌──────────┐
              │   Team   │
              └────┬─────┘
                   │ holds
                   ▼
              ┌──────────┐
              │  Player  │
              └──────────┘
```

**This same shape is everywhere in backend work.** On Day 10 the database connection
will be handed in the same way — the code that saves a `Player` will take a contract,
not a concrete Postgres class. That is what makes it swappable and testable, and it is
called **dependency injection**. You have now written it by hand, which is the best
possible preparation for meeting the automated version.

See [THE-BIG-PICTURE.md](../THE-BIG-PICTURE.md) for how this sits in the whole system.

---

## 6. Coming from functional programming

**This is a typeclass.** The mapping is exact:

| C# | What you know |
|---|---|
| `interface ITournamentFormat` | a typeclass declaration |
| `class RoundRobinFormat : ITournamentFormat` | an instance of that typeclass |
| `_format.GenerateMatches(teams)` | dispatch to the right instance |
| handing the format to the constructor | passing a record of functions |
| `ITournamentFormat[]` | a list of existentially-quantified values |

**Where it leaks:**

1. **Dispatch happens at runtime, not compile time.** Your instances are usually
   resolved by the compiler from the types at the call site. C# attaches a table of
   methods **to each object** and looks it up when the call runs. Same idea —
   dictionary passing — but the dictionary travels *with the value* instead of
   alongside it.
2. **A class must declare that it implements an interface.** You cannot add an instance
   for a type someone else wrote. No orphan instances, and no retrofitting an interface
   onto a third-party class.
3. **No higher-kinded types.** You cannot write an interface over "any container".
   There is no `IMonad<T>`.
4. **An interface holds no state.** It declares properties, but nothing is stored. Each
   implementing class supplies its own storage.

---

## 7. New C# syntax I met today

| Syntax | Means |
|---|---|
| `public interface ITournamentFormat { }` | declares a **contract** — members with no bodies |
| `class RoundRobinFormat : ITournamentFormat` | "this class fills that contract"; the compiler checks it |
| `private readonly ITournamentFormat _format;` | `readonly` = assignable only in the constructor |
| `ITournamentFormat[] formats = [a, b]` | collection expression — an array holding two different classes |
| `teams.Count % 2 != 0` | `%` is remainder; this is the "is it odd" check |
| `$"{format.Name,-20}"` | `,-20` pads to 20 characters, left-aligned |
| `params int[] ratings` | the caller can pass any number of arguments |
| `new Random(42)` | fixed seed, so every run produces the same "random" numbers |

---

## 8. Traps and gotchas

- **The `I` prefix is a convention, not a rule.** Nothing enforces it. Every C#
  codebase you meet will use it.

- **An interface cannot hold state.** Declaring `string Name { get; }` in the interface
  does not store anything. Each class supplies its own.

- **Asking "which class is this really?" defeats the whole thing.** If `Tournament`
  ever contains `if (_format is RoundRobinFormat)`, the day's work is undone. The point
  is *not knowing*.

- **The interface should be the narrowest thing that works.**
  `GenerateMatches(IReadOnlyList<Team>)` takes a read-only list on purpose — a format
  works out pairings, it has no business adding or removing teams.

- **Single elimination only generates round one.** Who plays in the semi-final depends
  on who wins the quarter-final, and none of them have been played. Advancing winners
  is not built yet, and the file says so rather than pretending.

- **`new Random()` without a seed gives different results every run**, which makes
  output impossible to compare. `new Random(42)` fixes it.

---

## 9. How a senior would think here

**"How many places change when I add a format?"** That is the question that drives the
whole design. With `if`: `Tournament`, plus the standings code, plus reporting. With
the interface: one new file. Same question as `private set` on Day 1, scaled up —
*shrink the number of places a change has to touch*.

**They would not have built the interface first.** Round robin was written as a plain
loop inside `Tournament`, and only pulled out when a **second** format appeared. An
interface with one implementation is usually just ceremony — you cannot see the right
shape for an abstraction until you have two things to compare. Write the concrete
thing, then extract.

**They would notice the duplication that is now there.** Both formats start with the
same `if (teams.Count < 2)` check. Two copies is not yet a problem. Three would be.
That is what tomorrow is about.

**They would put validation in the format, not the caller.** Each format knows which
team counts are valid for it; nothing outside does. Validation belongs with the
knowledge.

---

## 10. Checkpoint questions

1. **Q:** What one word in `Tournament.cs` would prove this refactor failed?
   **A:** `RoundRobinFormat` or `SingleEliminationFormat`. If `Tournament` names a
   concrete format, it still knows about them.

2. **Q:** `_format.GenerateMatches(_teams)` is one line. How does it run two different
   pieces of code?
   **A:** `_format` holds an address. The runtime looks at the **object** at that
   address, follows its table of methods, and calls that class's version. The variable's
   declared type decides what you are allowed to call; the object decides what runs.

3. **Q:** Why does `GenerateMatches` take `IReadOnlyList<Team>` rather than `List<Team>`?
   **A:** A format works out who plays who. It has no business adding or removing teams.
   Ask for the narrowest thing that does the job.

4. **Q:** Three teams — why does round robin accept it and knockout refuse?
   **A:** A league with three teams is a normal league. A knockout with three leaves
   someone with nobody to play. Each format owns the rules it knows about.

5. **Q:** You add `SwissFormat`. Which existing files change?
   **A:** None. You write one new class and pass it in.

6. **Q:** Why was the interface not written first, before round robin existed?
   **A:** You cannot see the right shape for an abstraction with only one example. An
   interface with one implementation is usually ceremony. Write the concrete thing,
   then extract when a second one shows up.

---

## 11. Commands I ran

```powershell
dotnet run --project src/Esports.Console
```

---

## 12. What's next

**Working and committed:**
- [Tournament.cs](../../src/Esports.Console/Tournament.cs) — teams, fixtures,
  registration rules, and no knowledge of any format
- [ITournamentFormat.cs](../../src/Esports.Console/ITournamentFormat.cs) — the contract
- Two formats producing different fixtures from the same teams
- Collections exposed as `IReadOnlyList<T>` — **the loose end open since Day 2 is now
  closed**

**Next, and why it follows:** look at the two format classes side by side. Both open
with the same check:

```csharp
if (teams.Count < 2)
{
    throw new ArgumentException($"... needs at least 2 teams, got {teams.Count}");
}
```

Two copies. A third format makes three. An interface cannot help — it holds no code,
only a shape.

What you want is a place to put **shared code** that all formats inherit, while each
still supplies its own pairing logic. That is an **abstract class**, and the difference
between it and an interface is the next topic.

**Loose ends carried forward:**
- Single elimination generates round one only. Advancing winners into later rounds
  needs results first.
- Nothing ranks teams yet — a league with fixtures but no table.
- Deferred from Day 1: what the compiled `.dll` actually contains.

---

*Concept file: [interfaces-and-polymorphism](../concepts/interfaces-and-polymorphism.md)*
