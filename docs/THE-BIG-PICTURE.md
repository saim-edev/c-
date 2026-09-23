# The big picture

**What this file is for:** every day teaches one small thing. This file is where those
small things are shown joining up. When something feels disconnected, read this.

Updated every day. Last updated: **Day 6**.

---

## Where we are right now

```
  DAY 0 ─────────── DAY 6                DAY 9 ──── DAY 18        DAY 19 ─── DAY 25
  ══════════════════════                 ═══════════════          ═══════════════
  you are here                           not started              not started

  Learn the language                     Turn it into a           Tools, live
  in a console app.                      real web API with        updates, background
  No web, no database,                   a real database.         work, and how
  no HTTP.                                                        senior devs work.

  Why this order: a database and an HTTP server are two more unfamiliar
  things. Learning the language with them in the way means debugging three
  unknowns at once. The console app runs in one second and prints text.
```

---

## What exists today

Eleven files. That is the whole program.

```
  F:\saim\
    src\Esports.Console\
      Program.cs                   <- the entry point. Builds teams, runs tournaments.

      Player.cs                    <- a person. Rating only changes via methods.
      Team.cs                      <- up to 5 players. Enforces that limit itself.
      Match.cs                     <- pairs two teams. A lifecycle it cannot cheat.
      MatchScore.cs                <- a result, e.g. 3-1. Knows if it was a draw.
      MatchState.cs                <- the five states a match can be in.
      Tournament.cs                <- holds teams + fixtures. Knows NO format.

      ITournamentFormat.cs         <- the contract: teams in, matches out.
      TournamentFormat.cs          <- abstract base. Shared validation, once.
      RoundRobinFormat.cs          <- everyone plays everyone.
      SingleEliminationFormat.cs   <- lose once, you are out.
```

### How they fit together

```
   ┌──────────────────────────────────────────────────────────┐
   │  Program.cs        the entry point - what runs            │
   └──────────────────────┬───────────────────────────────────┘
                          │ builds, and hands a format in
                          ▼
   ┌────────────────────────────────┐
   │  Tournament                    │
   │    Name                        │        ┌──────────────────────┐
   │    _teams    (private)         │        │  ITournamentFormat   │
   │    _matches  (private)         │        │   (an interface)     │
   │    _format ────────────────────┼───────►│                      │
   │                                │  knows │  Name                │
   │    Register()                  │  ONLY  │  GenerateMatches()   │
   │    GenerateMatches()           │  this  └──────────┬───────────┘
   │    Matches  (IReadOnlyList)    │                       │ implemented by
   └───────────────┬────────────────┘                       ▼
                   │ holds              ┌────────────────────────────────┐
                   ▼                    │  TournamentFormat  (abstract)  │
   ┌──────────────────────────┐         │    GenerateMatches()  <- shared │
   │  Match                   │         │      checks >= 2 teams, then    │
   │    HomeTeam ─────────────┼──────┐  │      calls BuildFixtures()      │
   │    AwayTeam ─────────────┼──────┤  │    BuildFixtures()   <- A HOLE  │
   │    State  ───────────────┼──┐   │  └───────────────┬────────────────┘
   │    Score? ────────┐      │  │   │           ┌──────┴───────┐
   │    Start()        │      │  │   │           ▼              ▼
   │    RecordResult() │      │  │   │     RoundRobin    SingleElimination
   │    Winner         │      │  │   │      Format            Format
   └───────────────────┼──────┘  │   │      6 matches      2 matches
                       │         │   │      (4 teams)      (4 teams)
   │    HomeTeam ─────────────┼──────┐
   │    AwayTeam ─────────────┼──────┤
   │    State  ───────────────┼──┐   │
   │    Score? ────────┐      │  │   │
   │    Start()        │      │  │   │
   │    RecordResult() │      │  │   │
   │    Winner         │      │  │   │
   └───────────────────┼──────┘  │   │
                       ▼         ▼   │
          ┌────────────────┐ ┌──────────────┐
          │  MatchScore    │ │  MatchState  │
          │  (a record)    │ │  (an enum)   │
          │  Home, Away    │ │  Scheduled   │
          │  IsDraw        │ │  InProgress  │
          │  Margin        │ │  Completed   │
          │  Create()      │ │  Forfeited   │
          └────────────────┘ │  Cancelled   │
                             └──────────────┘
                                     │
   ┌────────────────────────┐        │
   │  Team                  │◄───────┘ (two per match)
   │    Name, Tag           │
   │    _players (private)  │
   │    AddPlayer()         │
   │    AverageRating       │
   └───────────┬────────────┘
               │ holds up to 5
               ▼
   ┌────────────────────────┐
   │  Player                │
   │    GamerTag, Rating    │
   │    RecordWin()         │
   └────────────────────────┘
```

**The important arrow is the one from `Tournament` to `ITournamentFormat`.** It points
at a *contract*, not at a class. `Tournament` never learns whether it is running a
league or a knockout — and that is why adding a third format changes nothing in it.

---

## The order things were written, and why

This ordering is not arbitrary. **Each file could only be written once the one before
it existed.**

| # | File | Why it had to come first |
|---|---|---|
| 1 | [Player.cs](../src/Esports.Console/Player.cs) | The smallest thing with no dependencies. A team needs players to hold. |
| 2 | [Team.cs](../src/Esports.Console/Team.cs) | Needs `Player` to exist before it can hold a `List<Player>`. |
| 3 | [MatchScore.cs](../src/Esports.Console/MatchScore.cs) | Needs nothing, but a match needs it, so it came before `Match`. |
| 4 | [Match.cs](../src/Esports.Console/Match.cs) | Needs `Team` **and** `MatchScore`. Could not compile before both. |
| 5 | [MatchState.cs](../src/Esports.Console/MatchState.cs) | Added when `Match` outgrew a single true/false. `Match` cannot compile without it once it is referenced. |
| 6 | [Tournament.cs](../src/Esports.Console/Tournament.cs) | Needs `Team` and `Match`. Organises what already exists. |
| 7 | [ITournamentFormat.cs](../src/Esports.Console/ITournamentFormat.cs) | Extracted from `Tournament` **after** a second format was needed — not designed up front. |
| 8-9 | [RoundRobinFormat.cs](../src/Esports.Console/RoundRobinFormat.cs), [SingleEliminationFormat.cs](../src/Esports.Console/SingleEliminationFormat.cs) | Need the contract to exist before they can promise to fill it. |
| 10 | [TournamentFormat.cs](../src/Esports.Console/TournamentFormat.cs) | Extracted **after** both formats existed and the duplication was visible. Same rule as the interface: never designed up front. |

**The general rule, and it holds everywhere in backend work:** build from the inside
out. The thing with the fewest dependencies first. If file A mentions file B, B has to
exist.

The same rule will decide the order on Day 10: entities → database context →
migrations → services → controllers. Never the other way around.

---

## Where each concept came from and where it goes

Every small idea, traced from the day that introduced it to where it will matter again.

| Concept | Introduced | Why it was needed then | Where it comes back |
|---|---|---|---|
| **class** | [Day 1](days/day-01-types-classes-encapsulation.md) | Two players meant eight loose variables with nothing connecting them | Every entity. On Day 10-12 these become **database tables**. |
| **`private set`** | [Day 1](days/day-01-types-classes-encapsulation.md) | `RecordWin()` was a suggestion — anyone could still set `Rating = 999999` | The same instinct governs API design: expose the narrowest thing that works |
| **method = hidden first argument** | [Day 1](days/day-01-types-classes-encapsulation.md) | To understand what `faker.RecordWin()` even means | Underpins *everything* in OOP, including interfaces on Day 5-6 |
| **`List<T>`** | [Day 2](days/day-02-collections-and-references.md) | A team has five players | Becomes a one-to-many database relationship on Day 12 |
| **aliasing** | [Day 2](days/day-02-collections-and-references.md) | `Player b = a` then `b.RecordWin()` changed `a` | **Day 11:** EF Core's change tracker is built entirely on this idea |
| **record** | [Day 3](days/day-03-equality-records-and-nothing.md) | Two identical scores compared as *not equal*, which is nonsense | DTOs on Day 14 — the objects the API sends and receives |
| **nullable `?`** | [Day 3](days/day-03-equality-records-and-nothing.md) | An unplayed match has no score, and `0-0` is a lie | **Day 10:** values from a database bypass the compiler's checking entirely |
| **guards** | [Day 3](days/day-03-equality-records-and-nothing.md) | Validating at every call site means missing one | Becomes request validation on Day 14 — same idea, HTTP boundary |
| **enum** | [Day 4](days/day-04-enums-and-state-machines.md) | Five states; bools give sixteen combinations, strings allow typos | A database column on Day 12, with a real text-vs-number trade-off |
| **state machine** | [Day 4](days/day-04-enums-and-state-machines.md) | A result could be recorded on a match that never started | API endpoints refusing illegal operations, with proper HTTP status codes |
| **interface** | [Day 5](days/day-05-interfaces-and-polymorphism.md) | Two tournament formats; an `if` would make `Tournament` know every format forever | **Day 10:** the database is handed in exactly this way — that is dependency injection. Also the seam that makes testing possible. |
| **`IReadOnlyList<T>`** | [Day 5](days/day-05-interfaces-and-polymorphism.md) | Handing out the real `List<T>` let callers bypass the rules | Every API response — expose the narrowest thing that works |
| **abstract class** | [Day 6](days/day-06-abstract-classes.md) | Both formats duplicated the same validation, and nothing **forced** a new one to include it | Base controllers on Day 9; `DbContext` on Day 11 is itself a class you inherit from |

**Notice the pattern in the right-hand column.** Almost nothing is thrown away. The
console app is not a toy that gets deleted — it is the same code, later given a
database and an HTTP front door.

---

## The one idea underneath all of it

Every single day so far has been the same move in a different costume:

> **Make it impossible to get wrong, instead of remembering not to.**

| Day | What was possible before | What made it impossible |
|---|---|---|
| 1 | `faker.Rating = 999999` from anywhere | `private set` |
| 2 | Adding a 6th player to a 5-player team | the limit inside `AddPlayer` |
| 3 | A negative score; a team playing itself | guards in `Create` and the constructor |
| 4 | Recording a result on a match that never started | the state machine |
| 5 | A caller `.Add()`-ing straight into a tournament's fixture list | `IReadOnlyList<T>` |
| 6 | A new format forgetting the shared validation | `abstract` — the build fails |

A senior developer reaches for this instinctively. The question is never *"will I
remember to check?"* — it is **"where do I put this so nobody can skip it?"**

Usually the answer is: **at the boundary where the thing is created.** Validate once,
at construction, and everything downstream can trust it.

---

## How to find your way around this repo

If you came back to this in six months, this is the order to open things.

```
   1. README.md              what is this, how do I run it
   2. docs/INDEX.md          all 25 days at a glance
   3. THIS FILE              how the pieces fit
   4. docs/DECISIONS.md      why things are the way they are
   5. src/.../Program.cs     the entry point - ALWAYS start here in any repo
   6. follow the code        Program.cs uses Match, Match uses Team, ...
```

**Step 5 is the transferable skill.** In any codebase, in any language, find the entry
point and follow what it calls. In a .NET web app that entry point is also called
`Program.cs`, and it is the single most information-dense file in the project — every
feature and every setting is registered there, in order.

Day 24 covers this properly: the ten files to open, in order, when handed an unfamiliar
repo.

---

## What the folder structure will become

Right now there are no folders inside the console project, deliberately — **six files
is not a problem to solve.** Structure invented before the pain arrives is usually the
wrong structure.

Here is where it is heading, so the shape is not a surprise:

```
  src\
    Esports.Console\        ← the playground. Retired after Day 9, kept for reference.

    Esports.Core\           ← THE DOMAIN. Day 5.
      Entities\               Player, Team, Match, Tournament
      ValueObjects\           MatchScore
      Enums\                  MatchState
      Tournaments\            the two tournament formats (Day 5-6)
        References NOTHING. No database, no web. Just the rules of the game.

    Esports.Infrastructure\ ← TALKING TO THE OUTSIDE. Day 10.
      Persistence\            the database context, migrations, configuration
        References Core. Knows how to SAVE a Player. Core does not know it exists.

    Esports.Api\            ← THE FRONT DOOR. Day 9.
      Controllers\            receives HTTP, returns JSON
      Contracts\              the shapes sent over the wire (DTOs)
      Services\               orchestration
        References Core and Infrastructure.

  client\                   ← React. Day 17.
```

**Why three projects instead of three folders:** the compiler enforces the arrows.
`Esports.Core` has no reference to the database library, so it is *physically
impossible* to accidentally write database code in the domain. A folder could not stop
you; a missing project reference can.

```
     Api ──────► Infrastructure ──────► Core
      │                                  ▲
      └──────────────────────────────────┘

     Arrows point INWARD. Core knows nothing about
     the others. That is the whole point.
```

This is the same instinct as `private set`, scaled up to whole projects: **shrink the
number of places a rule can be broken.**

---

## Where this is all going

By Day 25 the same `Player`, `Team` and `Match` classes will be:

```
  Browser (React)
       │  clicks "submit result"
       ▼
  HTTP request over the network
       │
       ▼
  Esports.Api
       │  a Controller receives it        ← Day 9
       │  checks you are allowed to        ← Day 16
       │  validates the input              ← Day 14
       ▼
  A Service applies the rules             ← Day 14
       │  ...calling Match.RecordResult()  ← YOU WROTE THIS ON DAY 4
       ▼
  Esports.Infrastructure
       │  turns the object into SQL        ← Day 11-13
       ▼
  Postgres database in the cloud          ← Day 10
       │
       ▼  and on the way back out:
  a live update pushed to every           ← Day 21
  spectator's browser
```

**The line in the middle is the point.** The method you wrote on Day 4 does not change
when all of that is wrapped around it. The rules of the game are the stable core;
everything else is delivery.

That is what "strong foundations" actually means here — not that you have memorised C#
syntax, but that you can tell which part of a system is the *thing* and which parts are
*plumbing around the thing*.

---

## Concept files

The day notes are a journal — what happened, in order. These are the reference, written
to be read cold:

- [Classes vs records](concepts/classes-vs-records.md) — identity or value?
- [Memory and references](concepts/memory-and-references.md) — what a variable holds, aliasing
- [Collections](concepts/collections.md) — `List<T>` and choosing a collection
- [Nullability and guards](concepts/nullability-and-guards.md) — saying "nothing", refusing invalid objects
- [Enums and state machines](concepts/enums-and-state-machines.md) — modelling a lifecycle
- [Interfaces and polymorphism](concepts/interfaces-and-polymorphism.md) — one call, many answers
- [Abstract classes vs interfaces](concepts/abstract-classes.md) — shared code, and the one-parent limit
- [Debugging](concepts/debugging.md) — the bug log and the method

Plus [GLOSSARY.md](GLOSSARY.md) for terms, [CHEATSHEET.md](CHEATSHEET.md) for C# beside
its functional equivalent, and [DECISIONS.md](DECISIONS.md) for every "why X not Y".
