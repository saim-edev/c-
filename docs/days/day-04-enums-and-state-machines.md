# Day 4 — Enums, and a match that can't cheat

> **Goal:** Give a match a real lifecycle, where illegal moves between states are refused rather than silently allowed.
> **Date:** 2026-09-22 · **Tag:** `day-04`

---

## 0. Where we were

[Day 3](day-03-equality-records-and-nothing.md) built
[Match](../../src/Esports.Console/Match.cs), pairing two teams and holding a
`MatchScore?` — a score that might not exist yet. It could answer exactly one question
about its own progress:

```csharp
public bool HasBeenPlayed => Score != null;
```

**What was wrong with that:** `null` can only express two situations — played, or not
played. A real tournament match is **scheduled**, then **in progress**, then
**completed** — or it gets **forfeited**, or **cancelled**. That's five states, and one
nullable field cannot hold five states.

Worse, nothing stopped nonsense. A result could be recorded on a match that had never
started. A finished match could be finished again.

*Previous: [Day 3 — Equality, records, and nothing](day-03-equality-records-and-nothing.md)*

---

## 1. What we built today

- [MatchState.cs](../../src/Esports.Console/MatchState.cs) — an `enum` naming the five
  states a match can be in.
- [Match.cs](../../src/Esports.Console/Match.cs) — rewritten so every method enforces
  exactly one legal move between states. Illegal moves throw.

---

## 2. The flow — and the two wrong turns on the way

### Wrong turn 1 — one bool per state

The obvious first instinct:

```csharp
public bool IsScheduled  { get; private set; }
public bool IsInProgress { get; private set; }
public bool IsCompleted  { get; private set; }
public bool IsForfeited  { get; private set; }
```

Four booleans give **sixteen** combinations. Only four are legal. Nothing stops this:

```csharp
match.IsCompleted = true;
match.IsInProgress = true;    // completed AND in progress at the same time
```

```
Output: no error. Compiles and runs.
```

Straight back to *illegal states being representable* — the exact thing Days 1–3 spent
their time closing off.

### Wrong turn 2 — a string

```csharp
public string State { get; private set; } = "scheduled";

if (match.State == "in progress") { ... }
```

```
Output: no error — even when the value was set to "inprogress" somewhere else.
```

Typos compile. `"Scheduled"`, `"scheduled"` and `"SCHEDULED"` are three different
values. The compiler cannot help, because as far as it knows any text is valid.

### The answer — a fixed, named set

```csharp
public enum MatchState
{
    Scheduled,
    InProgress,
    Completed,
    Forfeited,
    Cancelled
}
```

Exactly five values. The compiler knows all of them. A typo is a build error:

```csharp
match.State = MatchState.InProgres;
```

```
error CS0117: 'MatchState' does not contain a definition for 'InProgres'
```

### Then: not every move is legal

Having five states isn't enough on its own — you also have to say which moves between
them are allowed.

```
            ┌──────────────► Cancelled
            │
  Scheduled ──► InProgress ──► Completed
            │         │
            └─────────┴──────► Forfeited
```

Each method in [Match.cs](../../src/Esports.Console/Match.cs) enforces exactly one
arrow. That's a **state machine** — a plain one, but that's the name for it.

---

## 3. How it works behind the scenes

### The lifecycle, run end to end

```csharp
Match final = new Match(t1, gen);
Console.WriteLine(final);

final.Start();
Console.WriteLine(final);

final.RecordResult(3, 1);
Console.WriteLine(final);
```

```
T1 vs GEN [Scheduled] not played yet
T1 vs GEN [InProgress] live now
T1 vs GEN [Completed] 3-1
Finished? True   Winner: T1
```

### Illegal moves are refused

```
refused : start a finished match -> Cannot start a match that is Completed
refused : cancel a finished match -> Cannot cancel a match that is Completed
refused : record a result twice -> Cannot record a result for a match that is Completed
refused : record a result before starting -> Cannot record a result for a match that is Scheduled
```

That last line is a whole bug class gone. Not documented, not "please don't" —
**impossible**.

### Why four bools fail, drawn

```
  FOUR BOOLS                          ONE ENUM
  ==========                          ========

  IsScheduled   true/false            State = one of:
  IsInProgress  true/false                    Scheduled
  IsCompleted   true/false                    InProgress
  IsForfeited   true/false                    Completed
                                              Forfeited
  2 x 2 x 2 x 2 = 16 combinations             Cancelled

  Legal:      4                       Legal:      5
  Nonsense:  12   <-- nothing          Nonsense:  0   <-- cannot be
                      stops these                        expressed
    IsCompleted + IsInProgress
    IsForfeited + IsScheduled
    all four false
    all four true ...
```

An enum is **exactly as big as the list you wrote**. Bools multiply. Strings are
infinite.

### The whole file

[MatchState.cs](../../src/Esports.Console/MatchState.cs):

```csharp
public enum MatchState
{
    Scheduled,    // fixture exists, nobody has played yet
    InProgress,   // currently being played
    Completed,    // played to a finish; there is a score
    Forfeited,    // one team did not show up; no real score
    Cancelled     // called off; never played, never will be
}
```

That is the entire type. Five names, one line each.

### The guard pattern, repeated

Every transition method has the same shape — [Match.cs:55](../../src/Esports.Console/Match.cs#L55):

```csharp
public void Start()
{
    if (State != MatchState.Scheduled)
    {
        throw new InvalidOperationException(
            $"Cannot start a match that is {State}");
    }

    State = MatchState.InProgress;
}
```

Check the current state, refuse if it's wrong, otherwise move. One arrow per method.

Here is every transition side by side, from
[Match.cs](../../src/Esports.Console/Match.cs):

```csharp
public void Start()            // Scheduled -> InProgress
{
    if (State != MatchState.Scheduled) { throw ...; }
    State = MatchState.InProgress;
}

public void RecordResult(int homeScore, int awayScore)   // InProgress -> Completed
{
    if (State != MatchState.InProgress) { throw ...; }
    Score = MatchScore.Create(homeScore, awayScore);
    State = MatchState.Completed;
}

public void Forfeit(Team winner)    // Scheduled or InProgress -> Forfeited
{
    if (State != MatchState.Scheduled && State != MatchState.InProgress) { throw ...; }
    if (!ReferenceEquals(winner, HomeTeam)
        && !ReferenceEquals(winner, AwayTeam)) { throw ...; }
    ForfeitWinner = winner;
    State = MatchState.Forfeited;
}

public void Cancel()           // Scheduled -> Cancelled
{
    if (State != MatchState.Scheduled) { throw ...; }
    State = MatchState.Cancelled;
}
```

Read the `if` at the top of each one and you have read the diagram above. **The
diagram is not documentation of the code — the code is the diagram.**

### The switch expression

[Match.cs:139](../../src/Esports.Console/Match.cs#L139):

```csharp
string detail = State switch
{
    MatchState.Scheduled  => "not played yet",
    MatchState.InProgress => "live now",
    MatchState.Completed  => $"{Score}",
    MatchState.Forfeited  => $"forfeit, {ForfeitWinner?.Tag} advances",
    MatchState.Cancelled  => "cancelled",
    _                     => "unknown"
};
```

This is pattern matching, and it is an **expression** — it produces a value, so it can
be assigned directly. Most of C# is statement-oriented (`if` is not an expression), so
`switch` expressions are a welcome island of the familiar. `_` is the catch-all arm.

---

## 4. Why it's needed

| Before | Now |
|---|---|
| `Score != null` — two states | Five named states |
| A result could be recorded on an unstarted match | Refused |
| A finished match could be restarted | Refused |
| A forfeit had nowhere to live | Its own state, with its own winner |
| Illegal combinations representable | Exactly one state, always |

The deeper reason: **an enum makes the set of possibilities finite and known to the
compiler.** Bools multiply (four bools → sixteen combinations). Strings are infinite.
An enum is exactly as big as the list you wrote.

---

## 5. Where it fits

`Match` is now complete enough to be organised. Next comes `Tournament`, which holds
many matches and decides who plays whom.

`MatchState` also survives to the database: on Day 10 it becomes a column, and the
choice between storing it as text or as a number is a real decision with a real
trade-off.

---

## 6. Coming from functional programming

**The analogy:** an enum looks like a sum type with no data constructors carrying
payloads. A `switch` expression looks like pattern matching. Both are true enough to be
useful.

**Where it leaks — three ways, all worth knowing:**

**1. No payload.** `MatchState.Completed` cannot carry the score. That's why
[Match.cs:20](../../src/Esports.Console/Match.cs#L20) still needs a separate `Score?`
field, and `Forfeited` needs its own `ForfeitWinner?`. In your world the state *would*
carry its data and the two could never disagree. Here they sit alongside each other
and keeping them consistent is your job.

**2. Exhaustiveness checking exists, but you have to give it up to be safe.** This is
subtler than "there is none", and the real behaviour is worth knowing exactly. Three
cases, all verified by building them:

```csharp
// (a) one arm missing, no `_`
string r = s switch { Scheduled => "a", InProgress => "b",
                      Completed => "c", Forfeited => "d" };
```
```
warning CS8509: The switch expression does not handle all possible values of its
input type (it is not exhaustive). For example, the pattern 'MatchState.Cancelled'
is not covered.

...and at runtime:
System.Runtime.CompilerServices.SwitchExpressionException:
Non-exhaustive switch expression failed to match its input.
```

```csharp
// (b) ALL five named arms, still no `_`
string r = s switch { Scheduled => "a", InProgress => "b", Completed => "c",
                      Forfeited => "d", Cancelled => "e" };
```
```
warning CS8524: The switch expression does not handle some values of its input
type (it is not exhaustive) involving an unnamed enum value. For example, the
pattern '(MatchState)5' is not covered.
```

The compiler refuses to call it exhaustive **precisely because of the `(MatchState)99`
problem in point 3.**

```csharp
// (c) all five arms PLUS `_`
```
```
no warning at all
```

**So C# forces a choice between two protections:**

```
   WITHOUT `_`                          WITH `_`
   ═══════════                          ════════
   ✓ warns about a missing case         ✗ missing case falls silently
                                          into the catch-all
   ✗ throws at runtime on an            ✓ handles a cast value safely
     unnamed value from a DB/JSON

   You cannot have both.
```

The `_` arm you need to survive a value cast in from a database is the same thing that
permanently silences the missing-case warning. **After adding a sixth enum value,
finding every switch that should handle it is a manual search.** That is the real cost
compared to a language where the compiler simply tells you.

**3. It's an `int` underneath.** This compiles and runs:

```csharp
MatchState nonsense = (MatchState)99;
Console.WriteLine(nonsense);
```

```
99
```

An enum value that is none of the five named options. It arrives by casting — usually
from a database or from JSON. That's the other reason for the `_` arm.

---

## 7. New C# syntax I met today

| Syntax | Means |
|---|---|
| `public enum MatchState { A, B, C }` | a fixed, named set of options — a new type |
| `MatchState.Scheduled` | one of those options |
| `State != MatchState.Scheduled` | enums compare with `==` and `!=` |
| `x switch { A => 1, B => 2, _ => 0 }` | pattern matching **as an expression** — produces a value |
| `_ =>` | the catch-all arm of a switch expression |
| `throw new InvalidOperationException(...)` | "this operation isn't valid right now" — as opposed to `ArgumentException`, which means "that input was wrong" |
| `!ReferenceEquals(a, b)` | `!` negates — "not the same object" |
| `a && b` / `a \|\| b` | and / or |
| `static void TryThis(string s, Action a)` | `Action` is "something runnable that returns nothing" — a function passed as a value |

---

## 8. Traps and gotchas

- **An enum is an `int`, so `(MatchState)99` is legal.** Always have a `_` arm, and be
  suspicious of enum values arriving from outside your program.

- **No exhaustiveness checking.** Adding a state to the enum will not break any
  `switch` that fails to handle it. You have to find them yourself. This is the single
  biggest difference from a real sum type.

- **The enum holds the state, but not the data that belongs to it.** `Completed` needs
  a `Score`; `Forfeited` needs a `ForfeitWinner`. Nothing in the language ties them
  together — the guards in the transition methods are what keep them consistent.

- **`InvalidOperationException` vs `ArgumentException`.** "You can't do that *now*"
  versus "that input was wrong". Using the right one makes the error message useful to
  whoever reads it at 2am.

- **`dotnet build` can hang** if a `dotnet run` from the same session still holds a
  file lock. Symptom: the command sits there forever. It is not a code problem.

---

## 9. Questions that came up

**Q: Why are there two project folders?**
[Esports.Console](../../src/Esports.Console/) is a playground — it prints text, runs in
about a second, and needs no browser or server. [Esports.Api](../../src/Esports.Api/)
is the real web app, still holding its untouched Day 0 template files. It stays idle
until Day 9. Learning the language shouldn't require a web server in the way.

**Q: Have we only made classes so far?**
Yes — four types plus one enum, about 424 lines:

```
Player.cs       50 lines   class
Team.cs         69 lines   class
Match.cs       151 lines   class
MatchScore.cs   45 lines   record
MatchState.cs   27 lines   enum
Program.cs      82 lines   the thing that runs
```

**Q: Why are there no folders inside the console project?**
Because six files isn't a problem yet. Folders solve *"I can't find anything"*. With
six files there is nothing to find. They get added when it hurts, around 15–20 files.
**Structure invented before the pain arrives is usually the wrong structure.**

**Q: NestJS has controllers, services and modules. Where are ours?**

Those exist to handle **web requests**, and we have none yet.

| NestJS | Its job | What we have |
|---|---|---|
| Controller | receives HTTP, returns JSON | nothing yet — **Day 9** |
| Service | the business rules | **this is what we've been writing** |
| Module | groups things, wires them up | nothing yet — **Day 10** |
| Entity | the data shape | `Player`, `Team`, `Match` |

Everything built so far is the part a NestJS *service* would hold — plus more, because
the rules live **inside** the entities (`RecordWin()` is on `Player`, not in a
`PlayerService`).

That difference has a name: **rich** domain models versus **anemic** ones. Much NestJS
code leaves entities as dumb data bags with all logic in services; this project does
the opposite. Both styles exist in C#. The service style arrives on Day 14.

**Q: Why no `this.` everywhere, like in NestJS?**

In TypeScript `this.` is mandatory. In C# it is optional and almost always omitted:

```csharp
Rating += 25;         // these two
this.Rating += 25;    // mean exactly the same thing
```

In JS, `this` is a runtime thing that can change depending on how a function was
called. In C# the compiler knows at compile time exactly which members the type has, so
a bare `Rating` can only mean one thing.

C# devs write `this.` in one situation — a name collision:

```csharp
public Player(string gamerTag, int rating)
{
    GamerTag = gamerTag;   // no clash: property is PascalCase, parameter is camelCase
}
```

The naming convention exists partly *so that* the collision never happens. That's why
you barely see `this.` in real C#.

---

## 10. Checkpoint questions

1. **Q:** Why not four booleans instead of an enum?
   **A:** Four bools have sixteen combinations and only four are legal, so nothing
   prevents `IsCompleted` and `IsInProgress` both being true. Illegal states become
   representable again.

2. **Q:** Why not a string?
   **A:** Typos compile. The compiler cannot check a value it considers to be "any
   text", so `"inprogress"` sails through and fails at runtime.

3. **Q:** What stops a result being recorded on a match that never started?
   **A:** [`RecordResult`](../../src/Esports.Console/Match.cs#L66) throws unless
   `State == MatchState.InProgress`.

4. **Q:** `(MatchState)99` compiles and prints `99`. What does that tell you about what
   an enum really is?
   **A:** It's an `int` with named constants layered on top. The named values are not
   the only values it can hold.

5. **Q:** You add a sixth state to the enum. What warns you that existing `switch`
   expressions don't handle it?
   **A:** It depends, and this is the subtle part. A switch **without** a `_` arm warns
   (`CS8509`) and throws at runtime if it hits the unhandled value. A switch **with** a
   `_` arm gives no warning at all — the new state silently falls into the catch-all.
   Since you need `_` to survive values cast in from a database, in practice you get no
   warning and must find the switches yourself.

6. **Q:** Why does `Completed` need a separate `Score?` field?
   **A:** A C# enum carries no payload. The state and its data are stored separately,
   and only the guards in the transition methods keep them consistent.

---

## 11. Commands I ran

```powershell
dotnet run --project src/Esports.Console
```

---

## 12. What's next

**Working and committed:**
- [MatchState.cs](../../src/Esports.Console/MatchState.cs) — five named states
- [Match.cs](../../src/Esports.Console/Match.cs) — a guarded lifecycle; illegal moves throw
- Four types and one enum, all with rules that cannot be bypassed from outside

**Next, and why it follows:** a `Tournament` that holds many matches. Once matches are
organised into a competition, the interesting question appears immediately — **there is
more than one way to run a tournament.** Single-elimination and round-robin take the
same list of teams and produce completely different sets of matches.

That is where **inheritance and interfaces** finally have a real reason to exist:
one thing calling two different algorithms through one shape. Not a textbook
`Animal`/`Dog` example — an actual problem that needs solving.

**Loose ends carried forward:**
- [Team._players](../../src/Esports.Console/Team.cs#L17) is completely hidden, so a
  team's roster cannot be listed at all. It needs a read-only view. *(open since Day 2)*
- Deferred from Day 1: what the compiled `.dll` actually contains. Worth revisiting
  once there is enough C# for it to mean something.
