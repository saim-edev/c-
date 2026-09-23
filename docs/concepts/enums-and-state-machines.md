# Enums and state machines — a lifecycle the compiler helps you enforce

A match isn't "played or not played". It is **scheduled**, then **in progress**, then
**completed** — unless it is **forfeited**, or **cancelled**. Five situations, and not
every move between them makes sense.

This file is about two separate things that arrive together:

1. **An enum** — how you hold *which* of five situations you are in.
2. **A state machine** — how you say which *moves* between them are legal.

The first is a type. The second is a pattern you write by hand.

---

## Part 1 — Why an enum

### The problem

Before Day 4, `Match` answered exactly one question about its own progress:

```csharp
public bool HasBeenPlayed => Score != null;
```

A `bool` holds two values. A nullable field holds "something" or "nothing" — also two.
**Neither can hold five.** And that mattered, because nothing stopped a result being
recorded on a match that had never started.

So: how do you store one-of-five?

---

### Rejected alternative 1 — one bool per state

The first instinct, and the one that feels most obvious:

```csharp
public bool IsScheduled  { get; private set; }
public bool IsInProgress { get; private set; }
public bool IsCompleted  { get; private set; }
public bool IsForfeited  { get; private set; }
```

Four independent booleans. Each is `true` or `false`, and nothing ties them to each
other. So the number of things this can express is 2 × 2 × 2 × 2:

```
  FOUR BOOLS                          ONE ENUM
  ==========                          ========

  IsScheduled   true/false            State = exactly one of:
  IsInProgress  true/false                    Scheduled
  IsCompleted   true/false                    InProgress
  IsForfeited   true/false                    Completed
                                              Forfeited
  2 x 2 x 2 x 2 = 16 combinations             Cancelled

  Legal:      4                       Legal:      5
  Nonsense:  12  <-- nothing          Nonsense:  0  <-- cannot be
                     stops these                       written down
    IsCompleted + IsInProgress
    IsForfeited + IsScheduled
    all four false   (no state at all)
    all four true    (every state at once)
```

And the nonsense is not hypothetical. This compiles and runs:

```csharp
bool isInProgress = true;
bool isCompleted  = true;
Console.WriteLine($"InProgress={isInProgress} Completed={isCompleted}");
```

```
InProgress=True Completed=True
```

No error. No warning. A match that is simultaneously being played and already
finished. That is *illegal states being representable* — the exact failure mode Days
1–3 spent their time closing off.

---

### Rejected alternative 2 — a string

The second instinct. It reads nicely, which is the trap:

```csharp
public string State { get; private set; } = "scheduled";

if (match.State == "in progress") { ... }
```

Two failures, both silent.

**Typos compile.** `"inprogress"` is a perfectly good string. The compiler has no
opinion about it, because as far as it knows *any* text is a valid value for a
`string`. The `if` above simply never matches, and you find out in production.

**Case is content.** These are three different values:

```csharp
Console.WriteLine("Scheduled" == "scheduled");
```

```
False
```

A string field doesn't have five values. It has **infinitely many**, and you have
agreed by convention that five of them are meaningful. Conventions are not enforced.

---

### The answer — a fixed, named set

An `enum` declares a **new type whose complete list of values you write out**. The
whole file is [MatchState.cs](../../src/Esports.Console/MatchState.cs), and it is
short enough to quote entirely:

```csharp
// An enum is a fixed, named set of options.
//
// The alternative was one bool per state - but four bools give SIXTEEN
// combinations when only four are legal, so nothing would stop a match being
// Completed and InProgress at once. A string would allow typos to compile.
//
// With an enum there are exactly five possible values, the compiler knows all
// of them, and MatchState.InProgres (typo) is a build error rather than a bug
// you find in production.

public enum MatchState
{
    // Fixture exists, nobody has played yet.
    Scheduled,

    // Currently being played.
    InProgress,

    // Played to a finish; there is a score.
    Completed,

    // One team did not show up. Ends the match, but there is no real score.
    Forfeited,

    // Called off. Never played, never will be.
    Cancelled
}
```

Five names, one line each. That is the entire type.

Now the typo is a **build error**, not a runtime surprise:

```csharp
MatchState s = MatchState.InProgres;
```

```
error CS0117: 'MatchState' does not contain a definition for 'InProgres'
```

The sentence that holds all three cases together:

> **An enum is exactly as big as the list you wrote. Bools multiply. Strings are
> infinite.**

`Match` uses it at [Match.cs:16](../../src/Esports.Console/Match.cs#L16):

```csharp
public MatchState State { get; private set; }
```

`private set` from [Day 2](../days/day-02-collections-and-references.md) is still doing its
job here: outside code can *read* the state, but only `Match.cs` may change it — which
is what makes Part 2 possible at all.

---

## Part 2 — State machines

### Having five states is not enough

Suppose the enum is in place and `State` is public to write. Nothing yet prevents:

```csharp
match.State = MatchState.Completed;   // it was never started
```

You have fixed *"which situation am I in"*. You have not fixed *"how did I get here"*.
A match should not be able to jump from `Scheduled` straight to `Completed`, because
a result exists only for a match that was actually played.

So you also have to write down which **moves** are allowed:

```
            ┌──────────────► Cancelled
            │
  Scheduled ──► InProgress ──► Completed
            │         │
            └─────────┴──────► Forfeited
```

Five states and five arrows. Anything not drawn is illegal. That shape has a name —
a **state machine**. This one is plain, but that is the name for it.

Read the diagram out loud:

- A match starts at `Scheduled`. Every match begins there —
  [Match.cs:38](../../src/Esports.Console/Match.cs#L38).
- `Scheduled` can start, be cancelled, or be forfeited.
- `InProgress` can finish with a result, or be forfeited.
- `Completed`, `Forfeited` and `Cancelled` have **no arrows out**. They are terminal;
  once you are there you stay there.

That last fact is exactly what `IsFinished` reports at
[Match.cs:111](../../src/Esports.Console/Match.cs#L111):

```csharp
public bool IsFinished =>
    State == MatchState.Completed
    || State == MatchState.Forfeited
    || State == MatchState.Cancelled;
```

---

### The guard pattern, once per arrow

Every transition method has the identical three-part shape:

1. Check the current state.
2. Refuse — `throw` — if the move is not drawn on the diagram.
3. Otherwise, do the work and move.

`Start` — [Match.cs:55](../../src/Esports.Console/Match.cs#L55), the arrow
`Scheduled → InProgress`:

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

`RecordResult` — [Match.cs:66](../../src/Esports.Console/Match.cs#L66), the arrow
`InProgress → Completed`:

```csharp
public void RecordResult(int homeScore, int awayScore)
{
    if (State != MatchState.InProgress)
    {
        throw new InvalidOperationException(
            $"Cannot record a result for a match that is {State}");
    }

    // Create() rather than `new` - it rejects negative numbers, so an
    // invalid score cannot get in here.
    Score = MatchScore.Create(homeScore, awayScore);
    State = MatchState.Completed;
}
```

Note the order inside the method: **the score is set first, then the state moves.**
There is no instant at which `State == Completed` but `Score` is still `null`.

`Forfeit` — [Match.cs:80](../../src/Esports.Console/Match.cs#L80), the only method with
*two* legal source states, and the only one with a second, different guard:

```csharp
public void Forfeit(Team winner)
{
    if (State != MatchState.Scheduled && State != MatchState.InProgress)
    {
        throw new InvalidOperationException(
            $"Cannot forfeit a match that is {State}");
    }

    if (!ReferenceEquals(winner, HomeTeam) && !ReferenceEquals(winner, AwayTeam))
    {
        throw new ArgumentException($"{winner.Tag} is not playing in this match");
    }

    ForfeitWinner = winner;
    State = MatchState.Forfeited;
}
```

The two guards are deliberately different exception types, and the difference is
meaningful:

| Exception | Means | Here |
|---|---|---|
| `InvalidOperationException` | "you can't do that **now**" | the match is already finished |
| `ArgumentException` | "that **input** was wrong" | that team isn't even playing |

Picking the right one is what makes the error useful to whoever reads it at 2am.

`Cancel` — [Match.cs:97](../../src/Esports.Console/Match.cs#L97), the arrow
`Scheduled → Cancelled`:

```csharp
public void Cancel()
{
    if (State != MatchState.Scheduled)
    {
        throw new InvalidOperationException(
            $"Cannot cancel a match that is {State}");
    }

    State = MatchState.Cancelled;
}
```

---

### The key line

Read the `if` at the top of each of those four methods and you have read the diagram.

> **The diagram is not documentation of the code. The `if` at the top of each method
> IS the diagram.**

This is why the pattern is worth naming. A comment describing the lifecycle rots the
first time someone edits a method. A guard cannot rot, because it is the thing that
runs. If you want to know the legal moves in an unfamiliar codebase, you don't read the
comments — you read the guards.

---

### What the refusals actually look like

Running the console project, the illegal moves all fail with a message that names the
state it was actually in:

```
--- the happy path ---
T1 vs GEN [Scheduled] not played yet
T1 vs GEN [InProgress] live now
T1 vs GEN [Completed] 3-1
Finished? True   Winner: T1

--- illegal transitions ---
  refused : start a finished match -> Cannot start a match that is Completed
  refused : cancel a finished match -> Cannot cancel a match that is Completed
  refused : record a result twice -> Cannot record a result for a match that is Completed
  refused : record a result before starting -> Cannot record a result for a match that is Scheduled
```

The last line is a whole bug class gone. Not documented, not "please don't" —
**impossible**.

Two details worth copying into your own code:

- Every message interpolates `{State}`, so the error says what the match *was*, not
  just that something went wrong. `"Cannot start a match"` would be useless;
  `"Cannot start a match that is Completed"` tells you the bug.
- The messages are written from the caller's point of view, describing the attempted
  operation — not from the object's point of view describing its internals.

---

## Part 3 — The switch expression

`ToString()` needs a different piece of text per state. That is
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

```
T1 vs GEN [Scheduled] not played yet
T1 vs GEN [InProgress] live now
T1 vs GEN [Completed] 3-1
T1 vs GEN [Forfeited] forfeit, T1 advances
T1 vs GEN [Cancelled] cancelled
```

Three things to notice.

**It is pattern matching.** `value switch { pattern => result, ... }`. Arms are read
top to bottom and the first match wins.

**`_` is the catch-all** — the arm that matches anything not matched above. Part 4
explains why it is not optional.

**It is an expression, not a statement.** It *produces* a value, so it can be assigned
directly to `string detail`. That distinction matters more in C# than it would in your
world, because most of C# is **statement-oriented** — `if` produces nothing, so this
does not compile:

```csharp
string detail = if (State == MatchState.Scheduled) "not played yet" else "...";
```

```
error CS1525: Invalid expression term 'if'
error CS1002: ; expected
```

You'd have to declare `detail` first and assign it inside branches. The switch
expression is one of the few **expression-oriented islands** in the language, which is
exactly why it feels familiar. C# has a handful of these — the conditional operator
`a ? b : c` is the other one you have already seen, at
[Match.cs:131](../../src/Esports.Console/Match.cs#L131):

```csharp
return Score.HomeWon ? HomeTeam : AwayTeam;
```

---

## Part 4 — Where the analogy leaks

**The analogy first, because it is genuinely useful:** a C# `enum` looks like a sum type
whose cases carry no payload, and a `switch` expression looks like pattern matching over
it. Both are true enough to get you started.

**But a C# enum is not a sum type, and the gap is bigger than it looks.** Three
specific failures, in increasing order of how much trouble they cause.

---

### Leak 1 — no payload

`MatchState.Completed` cannot carry the score. The case is just a name. So the data
that belongs to each state has to live **beside** the state, in separate fields —
[Match.cs:20](../../src/Esports.Console/Match.cs#L20) and
[Match.cs:23](../../src/Esports.Console/Match.cs#L23):

```csharp
public MatchState State { get; private set; }
public MatchScore? Score { get; private set; }
public Team? ForfeitWinner { get; private set; }
```

Compare the two shapes:

```
  WHAT YOU'D WRITE (sum type)      WHAT C# GIVES YOU
  ===========================      =================

  Scheduled                        State  = one of five names
  InProgress                              +
  Completed  of Score              Score         : MatchScore?   <- may be null
  Forfeited  of Team                              in ANY state
  Cancelled                        ForfeitWinner : Team?         <- may be null
                                                  in ANY state
  The data lives INSIDE
  the case. Impossible for         Three independent fields.
  Completed to have no score.      NOTHING in the type system
                                   stops Cancelled + a Score.
```

`State = Cancelled` together with `Score = 3-1` is a perfectly legal object as far as
the compiler is concerned. **What keeps them consistent is not the type — it is the
guards.** `RecordResult` is the only method that writes `Score`, and it also writes
`State = Completed` on the next line, so the two cannot drift apart.

This is why the two fields are `private set`. If they were public setters, the
consistency rule would have no place to live. It is the same instinct as `Rating` on
`Player`: **encapsulation routes mutation, it does not remove it.**

And it is why `Winner` has to re-derive the answer defensively at
[Match.cs:117](../../src/Esports.Console/Match.cs#L117) — checking the state *and*
the null-ness of `Score`, because the language will not promise that the two agree:

```csharp
if (State == MatchState.Forfeited)
{
    return ForfeitWinner;
}

if (State != MatchState.Completed || Score == null || Score.IsDraw)
{
    return null;
}
```

In your world `Score == null` would be unreachable by construction. Here it is a real
branch you have to write.

---

### Leak 2 — exhaustiveness is a warning, not a guarantee

This one has a nuance worth getting exactly right, because "C# has no exhaustiveness
checking" is the version you'll hear, and it is not quite true.

**Drop an arm and the compiler does warn.** With `Cancelled` missing and no `_`:

```csharp
string detail = s switch
{
    MatchState.Scheduled  => "not played yet",
    MatchState.InProgress => "live now",
    MatchState.Completed  => "done",
    MatchState.Forfeited  => "forfeit"
};
```

```
warning CS8509: The switch expression does not handle all possible values of its
input type (it is not exhaustive). For example, the pattern 'MatchState.Cancelled'
is not covered.
```

A **warning**. It builds anyway. And at runtime:

```
Unhandled exception. System.Runtime.CompilerServices.SwitchExpressionException:
Non-exhaustive switch expression failed to match its input.
Unmatched value was Cancelled.
```

**Now write all five arms and still leave out `_`.** You'd expect silence. You get:

```
warning CS8524: The switch expression does not handle some values of its input
type (it is not exhaustive) involving an unnamed enum value. For example, the
pattern '(MatchState)5' is not covered.
```

The compiler will not call a switch exhaustive *even when every named value is
covered*, because of Leak 3 below — an enum can hold values you never named. So the
only way to silence it is a `_` arm.

**And here is the trap.** Once `_` is there, the warning is gone forever. Add a sixth
state to the enum and the switch keeps compiling — silently:

```csharp
public enum MatchState
{
    Scheduled, InProgress, Completed, Forfeited, Cancelled, Postponed
}
```

```
Postponed printed as: unknown
```

No warning. No error. The new state quietly falls into the catch-all and prints
`unknown`.

```
  ADD A SIXTH STATE
  =================

  switch WITHOUT _  ->  warning CS8509 at build
                        + throws at runtime      loud -- but then you
                                                 can't handle (MatchState)99

  switch WITH _     ->  nothing at all           <-- the real situation
                        falls into "unknown"         in Match.cs
```

So the honest statement is: **C# forces you to choose between the two protections.**
The `_` arm defends against unnamed values arriving from outside; the price is that it
also swallows named values you forgot to handle. In your world the compiler would give
you both, and adding a case would light up every match site in the codebase. Here,
**after adding an enum value, finding every switch over it is a manual job** —
search for the enum's name and read each hit.

---

### Leak 3 — it is an `int` underneath

The named values are not the only values the type can hold. Each name is a constant
number — `Scheduled` is 0, `InProgress` is 1, and so on up:

```csharp
Console.WriteLine((int)MatchState.Cancelled);
```

```
4
```

Which means any `int` can be forced into the type:

```csharp
MatchState nonsense = (MatchState)99;
Console.WriteLine(nonsense);
Console.WriteLine(nonsense == MatchState.Completed);
Console.WriteLine(Enum.IsDefined(typeof(MatchState), nonsense));
```

```
99
False
False
```

It compiles. It runs. It prints `99` — because there is no name for 99, so
`ToString()` falls back to the number. You now hold a `MatchState` that is none of the
five options.

**Where this actually happens:** not from code like the above, which nobody writes on
purpose, but from values crossing a boundary into your program:

```
  database column  ─┐
  JSON request body ├──►  cast to MatchState  ──►  may be anything
  query string      ─┘                             an int can be
```

A row written by an older version of the app. A hand-edited row. A JSON body from a
client that sent `"state": 7`. All of these produce an enum value that passes every
`==` check by failing it, and lands in `_`.

That is the second reason the `_` arm exists — and `Enum.IsDefined` above is the tool
for checking a value at the boundary, before it gets in.

---

### So what would you use for a real sum type in C#?

Two options, both arriving later in the curriculum, and neither as clean as a native
ADT. Sketched here only so you know they exist:

**1. A class hierarchy** — a base type with one subclass per case, each subclass
carrying its own data:

```csharp
public abstract class MatchOutcome { }
public sealed class Completed : MatchOutcome { public MatchScore Score { get; } }
public sealed class Forfeited : MatchOutcome { public Team Winner { get; } }
```

Now `Completed` cannot exist without a score. This needs inheritance, which is Day 5.

**2. Records plus type patterns** — the same shape, less ceremony, and the switch
matches on *type* rather than value:

```csharp
string detail = outcome switch
{
    Completed c => $"{c.Score}",
    Forfeited f => $"forfeit, {f.Winner.Tag} advances",
    _           => "unknown"
};
```

**Why neither is used here, honestly:**

- Both are far more code than five lines of `enum` for a case where only one of the
  five states carries data worth modelling.
- Neither is exhaustiveness-checked either. A `sealed` hierarchy gets closer, but the
  compiler still will not force you to handle every subclass.
- Neither maps cleanly to a single database column, and `MatchState` becomes one.

The enum is the right call *here*. The hierarchy becomes the right call when several
states carry different data and the mismatches start causing real bugs.

---

## Part 5 — Practical

### Storing an enum

`MatchState` does not stay in memory. Around Day 10–12 it becomes a database column,
and there is a genuine choice with a genuine trade-off:

| Stored as | Pro | Con |
|---|---|---|
| **Text** (`'InProgress'`) | readable in the database; survives reordering the enum | bigger; a rename breaks old rows |
| **Number** (`1`) | small; fast to index | meaningless when you read the table; **silently wrong if the enum is reordered** |

The "silently wrong" case is the one to fear. Numbers are assigned by **position**, so
inserting a state in the middle shifts everything below it:

```
  BEFORE                 AFTER inserting Postponed in second place
  ======                 ========================================

  Scheduled   = 0        Scheduled   = 0
  InProgress  = 1        Postponed   = 1   <-- new
  Completed   = 2        InProgress  = 2   <-- was 1
  Forfeited   = 3        Completed   = 3   <-- was 2
  Cancelled   = 4        Forfeited   = 4   <-- was 3
                         Cancelled   = 5   <-- was 4

  Every row in the database storing 2 now means Completed
  instead of InProgress. No error. No migration. Just wrong.
```

The defence, if you store numbers, is to **number them explicitly** so the value is
attached to the name rather than to the position:

```csharp
public enum MatchState
{
    Scheduled  = 1,
    InProgress = 2,
    Completed  = 3,
    Forfeited  = 4,
    Cancelled  = 5
}
```

Now inserting `Postponed = 6` at any position in the file changes nothing about the
existing values. `MatchState.cs` doesn't do this yet, because nothing persists it yet —
**don't add structure before the pain arrives.** When the database column lands, this
is the first thing to revisit.

---

### `[Flags]` — a different use of the same keyword

Sometimes you want a **set** of options rather than one of them: a player's roles, the
notification channels enabled. `[Flags]` marks an enum whose values are powers of two
so they can be combined bit by bit:

```csharp
[Flags]
public enum Notify
{
    None  = 0,
    Email = 1,
    Sms   = 2,
    Push  = 4
}

Notify n = Notify.Email | Notify.Push;   // both
```

This is the opposite problem from `MatchState`. A match is in exactly **one** state, so
combining values would be nonsense. Not needed here, and mentioned only so that seeing
`[Flags]` in someone else's code doesn't read as a mystery.

---

### How a senior thinks here

- **Model the lifecycle before writing any method.** Draw the states and the arrows
  first. The methods then fall out of the diagram — one per arrow — instead of being
  invented one at a time and guarded inconsistently.
- **One method enforces exactly one transition.** A `SetState(MatchState newState)`
  method would put all five rules in one place and tempt every caller to pass whatever
  they like. Separate methods mean each guard is small enough to read in one glance.
- **Always write the `_` arm.** Not for tidiness — because Leak 3 means an enum value
  arriving from outside may be none of the named ones.
- **Be suspicious of an enum crossing a boundary.** Database, JSON, HTTP query string.
  Inside your program the value came from your own code and is one of the five. Coming
  in from outside it is an `int` wearing a costume — validate it at the edge
  (`Enum.IsDefined`), not in the middle.
- **When state and data must stay consistent, put both behind methods.** Public setters
  on `State` and `Score` would make the invariant unenforceable, because there'd be no
  single place to enforce it. `private set` + transition methods gives the rule
  somewhere to live.
- **Terminal states are a design decision, say it out loud.** `Completed` has no arrows
  out. If a tournament ever needs a result overturned, that is a *new arrow*, added
  deliberately with its own method and its own guard — not a public setter added
  quietly because someone needed it.

---

### Where this reappears

| Where | What happens to it |
|---|---|
| **Day 5** — inheritance | the "real sum type" sketch above becomes a class hierarchy you actually write |
| **Day 10–12** — database | `MatchState` becomes a column, and the text-vs-number decision above becomes real |
| **Tournament** | gets its own state machine — registration open → seeded → running → finished — with the same guard pattern |
| **Day 9+** — API endpoints | the same guards refuse illegal operations, and `InvalidOperationException` becomes an HTTP 409 Conflict rather than a crash |

The pattern outlives the language. Anything with a lifecycle — an order, a payment,
a subscription, a deployment — is this exact shape.

---

## Traps

- **An enum is an `int`, so `(MatchState)99` is legal, runs, and prints `99`.** Always
  have a `_` arm. Validate enums arriving from outside the program.

- **The `_` arm silences the exhaustiveness warning permanently.** Adding a sixth state
  will not break, or even warn about, any switch that has a `_`. After adding a value,
  search for the enum's name and check every switch by hand.

- **Without `_`, a missed case throws `SwitchExpressionException` at runtime** — it does
  not return null or a default. The build only warns (CS8509 / CS8524).

- **The enum holds the state but not the data that belongs to it.** `Completed` needs a
  `Score`; `Forfeited` needs a `ForfeitWinner`. Nothing in the language ties them
  together. The guards in the transition methods are the only thing keeping them
  consistent — so never expose a public setter on either.

- **Set the data before you move the state.** `RecordResult` writes `Score` and *then*
  `State`. Reversed, there would be a moment where `State == Completed` and `Score` is
  still `null` — which matters as soon as anything else can observe the object.

- **Reordering enum values changes their numbers.** Harmless in memory, silently
  destructive once the numbers are stored anywhere. Number them explicitly before they
  are persisted.

- **`InvalidOperationException` vs `ArgumentException`.** "You can't do that *now*"
  versus "that input was wrong". Both appear in `Forfeit`, deliberately. Choosing right
  is the difference between a useful error and a useless one.

- **Comparing enums uses `==` and `!=` and compares the underlying number.** No
  reference semantics here — an enum is a value type, so none of the aliasing lessons
  from [Day 2](../days/day-02-collections-and-references.md) apply to it.

---

*Introduced: [Day 4](../days/day-04-enums-and-state-machines.md)*
