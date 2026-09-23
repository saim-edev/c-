# Day 6 — Abstract classes: shared code that cannot be skipped

> **Goal:** Write the part every format does the same way once, and make the compiler force every new format to fill in the rest.
> **Date:** 2026-09-23 · **Tag:** `day-06`

---

## 0. Where we were

[Day 5](day-05-interfaces-and-polymorphism.md) pulled "how to work out fixtures" out of
`Tournament` and into an interface. Two formats filled the same contract and produced
different answers from the same teams.

**What was wrong:** both formats opened with the same check.

[RoundRobinFormat.cs](../../src/Esports.Console/RoundRobinFormat.cs), before today:

```csharp
if (teams.Count < 2)
{
    throw new ArgumentException(
        $"Round robin needs at least 2 teams, got {teams.Count}");
}
```

[SingleEliminationFormat.cs](../../src/Esports.Console/SingleEliminationFormat.cs),
before today:

```csharp
if (teams.Count < 2)
{
    throw new ArgumentException(
        $"Single elimination needs at least 2 teams, got {teams.Count}");
}
```

Two copies. A third format would make three. And worse than the typing: **nothing
forced a new format to include it.** Someone writes `SwissFormat`, forgets the check,
and it ships.

*Previous: [Day 5 — Interfaces and polymorphism](day-05-interfaces-and-polymorphism.md)*

---

## 1. What we built today

[TournamentFormat.cs](../../src/Esports.Console/TournamentFormat.cs) — an **abstract
class** sitting between the interface and the two formats. It holds the shared
validation once, and demands that each format supply its own pairing logic.

Both format classes now inherit from it instead of implementing the interface directly.

---

## 2. The flow

### Why the interface could not fix this

```csharp
public interface ITournamentFormat
{
    string Name { get; }
    List<Match> GenerateMatches(IReadOnlyList<Team> teams);
}
```

An interface holds **no code**. It is a shape. It can say *"you must have this method"*.
It cannot say *"and here is the part everybody does the same way"*.

That is the whole reason abstract classes exist.

### What was needed

Somewhere to put shared code, that **also forces** each format to supply the part only
it knows. [TournamentFormat.cs](../../src/Esports.Console/TournamentFormat.cs):

```csharp
public abstract class TournamentFormat : ITournamentFormat
{
    // No body, and every subclass MUST supply one.
    public abstract string Name { get; }

    // The shared part, written ONCE.
    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2)
        {
            throw new ArgumentException(
                $"{Name} needs at least 2 teams, got {teams.Count}");
        }

        return BuildFixtures(teams);
    }

    // A HOLE. No body here. Each format fills it.
    protected abstract List<Match> BuildFixtures(IReadOnlyList<Team> teams);
}
```

**Read the shape of `GenerateMatches` carefully.** It does the common work, then calls
`BuildFixtures` — which has no body at this level. The base class controls the **order**;
each subclass fills in the **gap**.

```
   GenerateMatches(teams)          <- in the BASE class, written once
   │
   ├── if (teams.Count < 2) throw  <- shared. Cannot be skipped.
   │
   └── BuildFixtures(teams)        <- A HOLE. The base has no body for it.
              │
       ┌──────┴──────┐
       ▼             ▼
   RoundRobin    SingleElimination
   fills the     fills the
   hole          hole
```

This pattern has a name — **template method** — and it turns up constantly.

### What a subclass looks like now

[RoundRobinFormat.cs:9](../../src/Esports.Console/RoundRobinFormat.cs#L9):

```csharp
public class RoundRobinFormat : TournamentFormat
{
    public override string Name => "Round robin";

    protected override List<Match> BuildFixtures(IReadOnlyList<Team> teams)
    {
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

The validation is gone from this file. It happens in the base, before `BuildFixtures`
is ever called.

`override` means *"I am supplying the body the base class left empty"*.

### A rule that belongs to one format only

[SingleEliminationFormat.cs:10](../../src/Esports.Console/SingleEliminationFormat.cs#L10):

```csharp
protected override List<Match> BuildFixtures(IReadOnlyList<Team> teams)
{
    // The "at least 2 teams" check already ran, in the base class.
    // This rule belongs HERE and not in the base, because it is not true
    // of tournaments generally - a league with 3 teams is fine. Only a
    // knockout leaves somebody with nobody to play.
    if (teams.Count % 2 != 0)
    {
        throw new ArgumentException(
            $"{Name} needs an even number of teams, got {teams.Count}");
    }

    // ... seeded pairing ...
}
```

**Shared rules go in the base. Rules only one format knows go in that format.** The
question to ask is: *is this true of every tournament, or only this kind?*

---

## 3. How it works behind the scenes

### Same output as yesterday, less duplication

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

--- limits ---
  refused : knockout with 3 teams -> Single elimination needs an even number of teams, got 3
  allowed : round robin with 3 teams
```

### Three guarantees, verified by the compiler

Not convention, not code review. These were checked by actually building them.

**1. You cannot create an abstract class.**

```csharp
TournamentFormat f = new TournamentFormat();
```

```
error CS0144: Cannot create an instance of the abstract type or
              interface 'TournamentFormat'
```

`TournamentFormat` is half a class waiting to be finished. There is no such thing as
"a tournament format" in the abstract — only a round robin or a knockout.

**2. A subclass cannot skip an abstract member.**

```csharp
public class BadFormat : TournamentFormat { }
```

```
error CS0534: 'BadFormat' does not implement inherited abstract member
              'TournamentFormat.BuildFixtures(IReadOnlyList<Team>)'
error CS0534: 'BadFormat' does not implement inherited abstract member
              'TournamentFormat.Name.get'
```

**This is the important one.** A new format *cannot* forget the shared validation,
because it cannot exist without going through `GenerateMatches`. Not documented, not
reviewed for — the build fails.

**3. The hole cannot be reached from outside.**

```csharp
var rr = new RoundRobinFormat();
rr.BuildFixtures(teams);
```

```
error CS0122: 'RoundRobinFormat.BuildFixtures(IReadOnlyList<Team>)' is
              inaccessible due to its protection level
```

`protected` means *visible to this class and anything inheriting from it, nothing else*.
If `BuildFixtures` were public, a caller could skip straight past the validation.

That is the same lesson as the [MatchScore bug](../concepts/debugging.md): **a guard is
only as good as its doors.** Closing the front door and leaving the side door open is
not closing the door.

---

## 4. Why it's needed

| Before | Now |
|---|---|
| The check written twice | Written once |
| A third format writes it a third time | A third format inherits it |
| A new format could forget it | **Cannot compile without it** |
| Nothing said which rules are universal | Base = universal, subclass = specific |

**The strongest argument is the third row.** Removing duplication is nice. Making it
*impossible to omit* is the real win, and it is the same instinct as `private set` on
Day 1 and the state machine on Day 4: shrink the number of ways to get it wrong.

---

## 5. Where it fits

```
                    Tournament
                        │  depends on the narrowest thing
                        ▼
                ITournamentFormat            (an interface - a shape, no code)
                        │
                        │  implemented by
                        ▼
                TournamentFormat             (abstract - shared code + a hole)
                 ├─ GenerateMatches()          shared, cannot be skipped
                 └─ BuildFixtures()            abstract: a hole
                        │
              ┌─────────┴──────────┐
              ▼                    ▼
       RoundRobinFormat    SingleEliminationFormat
        fills the hole        fills the hole
                              + its own even-number rule
```

`Tournament` still depends on the **interface**, not the abstract class. That is
deliberate — depend on the narrowest thing that works. Someone could write a format
that implements `ITournamentFormat` without inheriting `TournamentFormat`, and
`Tournament` would not care.

**Honest counterpoint:** with only two formats, both inheriting the base, the interface
is not earning much right now. It is a judgement call, not a rule. If a third format
never appears that skips the base, deleting the interface would be reasonable.

**Where inheritance shows up later:** on Day 9, API controllers inherit from a base
`ControllerBase` that supplies `Ok()`, `NotFound()` and the rest. On Day 11, `DbContext`
is a class you inherit from. You will be on the *subclass* side of this arrangement
constantly, so knowing what the base is doing matters.

See [THE-BIG-PICTURE.md](../THE-BIG-PICTURE.md).

---

## 6. Coming from functional programming

**This is the one concept so far with no clean analogue.**

| C# | Closest thing you know |
|---|---|
| `interface` | a typeclass. Clean mapping. |
| `abstract class` | a signature **plus** default implementations **plus** state. Loosely, a module signature with defaults. The fit is poor. |
| inheritance itself | nothing really |

**Treat that as a hint, not a gap in your knowledge.** Inheritance is the part of OOP
that ages worst, and the fact that it has no functional equivalent is a reason to be
sparing with it.

**The leaks:**

1. **One parent only.** A class can implement five interfaces but inherit from exactly
   one class. That slot is spent forever. This is the real cost of choosing an abstract
   class over an interface.
2. **The base can reach into the subclass.** `GenerateMatches` calls `BuildFixtures`,
   which does not exist yet at that level. The base is written against a hole it trusts
   someone else to fill. Nothing like this exists in a world of plain functions.
3. **Deep hierarchies become unreadable.** `Format` → `EliminationFormat` →
   `SingleElimination` → `SeededSingleElimination` means reading four files to
   understand one class, and a change in the base ripples through every descendant.
   **One level, like here, is usually enough.**

**The rule worth carrying:** prefer **handing something in** over **inheriting from
something**. `Tournament` *takes* a format rather than *being* a format. That is
composition, and it stays flexible where inheritance locks you in.

---

## 7. New C# syntax I met today

| Syntax | Means |
|---|---|
| `public abstract class TournamentFormat` | cannot be instantiated; exists only to be inherited from |
| `public abstract string Name { get; }` | no body; every subclass **must** supply one |
| `protected abstract List<Match> BuildFixtures(...)` | a hole, visible only to subclasses |
| `class RoundRobinFormat : TournamentFormat` | inherit from that class (vs `: ISomething`, which only promises) |
| `public override string Name => "Round robin"` | "I am supplying the body the base left empty" |
| `protected` | visible to this class and anything inheriting from it — nothing else |

**Access modifiers so far, narrowest first:**

| | Who can see it |
|---|---|
| `private` | this class only |
| `protected` | this class **and** anything inheriting from it |
| `public` | everyone |

---

## 8. Traps and gotchas

- **`override` is required and easy to forget.** Leaving it off does not silently do
  the right thing — the compiler complains that you are hiding an inherited member.

- **Access has to match.** `BuildFixtures` is `protected` in the base, so it must be
  `protected` in the subclass too. Widening it to `public` would reopen the door the
  base closed.

- **One parent, forever.** Choosing an abstract class spends the only inheritance slot
  that class has. If it might later need to be several unrelated things, an interface
  keeps that option open.

- **Do not build a deep hierarchy because it feels tidy.** One level is usually enough.
  Four levels means reading four files to understand one class.

- **Do not put a rule in the base that is not universal.** The even-number check is in
  `SingleEliminationFormat`, not the base, because a three-team league is perfectly
  fine. Ask: *is this true of every kind, or only this kind?*

- **The base calling into the subclass is the point, and also the risk.** It works here
  because `BuildFixtures` is called after validation. Calling a subclass method from a
  base *constructor* is a real trap — the subclass's own fields are not set up yet.

---

## 9. How a senior would think here

**They would not have written the base class on Day 5.** With one format there is
nothing to share, and with two there is exactly one duplicated line. The base class was
extracted *after* the duplication was visible — same as the interface. **Write the
concrete thing; extract when the pattern shows itself.** You cannot see the right shape
for shared code before you have something to compare.

**They would ask "which rules are universal?" before writing a line of it.** That one
question decides everything that goes in the base and everything that stays out. Get it
wrong and you either duplicate (too little in the base) or you constrain formats that
should not be constrained (too much).

**They would make the hole `protected`, not `public`, without thinking about it.** The
reflex is: *what is the narrowest access that works?* Public is the default people
reach for and is almost always too wide.

**They would notice the interface is now barely earning its place** and leave it, with
a note, rather than delete it. It costs one file and keeps an option open. Deleting it
is a decision that can be made later with more information; un-deleting is harder.

**They would stop at one level of inheritance** and reach for composition next time.

---

## 10. Checkpoint questions

1. **Q:** Why could the interface not solve the duplication?
   **A:** An interface holds no code. It can demand a method exists; it cannot supply
   a shared body.

2. **Q:** What stops someone writing a new format that forgets the "at least 2 teams"
   check?
   **A:** The compiler. `BuildFixtures` is abstract, so a new format must inherit and
   fill that hole, and the only public way in is `GenerateMatches`, which validates
   first. `error CS0534` if they try to skip it.

3. **Q:** Why is `BuildFixtures` `protected` rather than `public`?
   **A:** If it were public, a caller could invoke it directly and skip the validation
   in `GenerateMatches`. Same problem as `MatchScore.Create` being bypassable by `new`.

4. **Q:** Why is the even-number check in `SingleEliminationFormat` and not the base?
   **A:** It is not true of tournaments generally. A three-team league is fine. Only a
   knockout leaves someone with nobody to play. Shared rules go in the base; specific
   rules stay with the format that knows them.

5. **Q:** The real cost of choosing an abstract class over an interface?
   **A:** A class can implement many interfaces but inherit from exactly one class.
   That slot is spent.

6. **Q:** Why does `Tournament` still depend on `ITournamentFormat` rather than
   `TournamentFormat`?
   **A:** Depend on the narrowest thing that works. A future format could implement the
   interface without inheriting the base, and `Tournament` would not need changing.

---

## 11. Commands I ran

```powershell
dotnet run --project src/Esports.Console
```

---

## 12. What's next

**Working and committed:**
- [TournamentFormat.cs](../../src/Esports.Console/TournamentFormat.cs) — shared
  validation written once, and a hole each format must fill
- Both formats inherit it; the duplication is gone
- Three guarantees verified against the compiler, not assumed

**Next, and why it follows:** the tournament runs its matches and then nothing happens.
Six matches get played and there is no table, no ranking, nobody knows who won the
league. A knockout has a winner by definition; a round robin needs **points counted and
teams sorted**.

Counting and sorting a list is where C# has a whole toolkit that will feel like coming
home: `Select`, `Where`, `OrderBy`, `Sum`. They are `map`, `filter`, `sortBy` and `fold`
with different names. **That day should be the easiest one so far.**

**Loose ends carried forward:**
- Single elimination generates round one only. Advancing winners needs results first.
- Nothing ranks teams yet — fixtures but no table.
- Deferred from Day 1: what the compiled `.dll` actually contains.

---

*Concept file: [abstract-classes](../concepts/abstract-classes.md). Related:
[interfaces-and-polymorphism](../concepts/interfaces-and-polymorphism.md),
[thinking-like-a-backend-dev](../concepts/thinking-like-a-backend-dev.md).*
