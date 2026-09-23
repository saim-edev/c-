# Saying "nothing", and refusing invalid objects

Two halves of one habit.

> **Part 1** — how a type admits that a value might not be there.
> **Part 2** — how a type refuses to exist at all when the values are wrong.

The first is about *absence you expect*. The second is about *nonsense you forbid*.

---

# Part 1 — saying "nothing"

## The problem: there is no number that means "hasn't happened"

A `Match` holds a score. But a match that has not been played yet **has no score**.

The obvious move is to store `0-0` until it's played:

```csharp
public MatchScore Score { get; private set; } = new MatchScore(0, 0);
```

That's a lie, and a bad one. `0-0` is a **real result** — a draw. So now standings
code cannot tell "scheduled for Friday" apart from "they played and drew", and every
unplayed match quietly adds a draw to both teams' records.

Pick any other sentinel and it fails the same way. `-1-0`? Now `MatchScore.Margin`
returns 1 for a match nobody played. Every number in the range means something.

**There is no value of `MatchScore` that means "no `MatchScore`."** You need a way to
say *nothing*, outside the set of real values.

## `?` — "this might be nothing"

[Match.cs:20](../../src/Esports.Console/Match.cs#L20):

```csharp
// The `?` means "this might be nothing". A match that has not been played
// has no score, and 0-0 would be a lie - that is a real result, a draw.
public MatchScore? Score { get; private set; }
```

That single character changes what the compiler will let you write. Without the `?`,
assigning `null` to `Score` is a warning. With the `?`, assigning `null` is fine — but
**reading `Score.Margin` without checking first** becomes the warning instead.

The compiler tracks, statement by statement, whether it currently believes a variable
could be null. Here is the actual warning, from a real build:

```csharp
Team? maybeWinner = null;
Console.WriteLine(maybeWinner.Name);
```

```
Program.cs(3,19): warning CS8602: Dereference of a possibly null reference.
```

"Dereference" means *follow the arrow to the object and read something off it*. The
compiler is saying: you are about to follow an arrow that might not point anywhere.

---

## The critical point: `?` is erased at runtime

**This is the single most important fact on this page, and the biggest difference from
the `Option` you already know.**

`MatchScore?` and `MatchScore` compile to **byte-identical** code.

- It allocates nothing.
- It wraps nothing.
- There is no `Some` and no `None`.
- It **does not exist** when the program runs.

`?` on a reference type is an *annotation*. Its entire job is to make the compiler
analyse your code and emit warnings. Then it is thrown away.

Here is the proof, and it is a good one. `typeof(X)` asks the runtime for the type
object of `X`. Ask it for `int?` and you get a real answer. Ask it for `string?` and
the code **will not compile**, because there is no such type to ask about:

```csharp
Console.WriteLine(typeof(int?));      // fine
Console.WriteLine(typeof(string?));   // does not compile
```

```
Program.cs(9,43): error CS8639: The typeof operator cannot be used on
                  a nullable reference type
```

```
typeof(int?)     : System.Nullable`1[System.Int32]
```

`int?` is a real type with a real name. `string?` is not a type at all — it is `string`
with a sticky note on it that the compiler peels off before emitting.

### What follows from that

A `null` can still arrive at runtime, from places the compiler never saw:

```
   COMPILER SEES THIS                RUNTIME GETS THIS
   ==================                =================

   your .cs files      ──────────►   checked, warned about   ✓

   a library built                   no annotations at all,
   before C# 8         ──────────►   so anything can be null ✗

   JSON from the wire  ──────────►   a missing field is null ✗

   a database column   ──────────►   NULL is a real value    ✗
```

**`?` is a very good linter. It is not a proof.**

Contrast with what you already have. `Option<MatchScore>` is a *value*. It exists. It
has a runtime representation. You cannot accidentally treat a `None` as a
`MatchScore`, because they are different types all the way down, and nothing outside
your program can hand you a fake one.

C#'s `?` gives you most of the ergonomics of that and **none of the guarantee**.

| | `Option<T>` (your world) | `T?` (C#) |
|---|---|---|
| Exists at runtime | yes, a real value | no, erased |
| Costs an allocation | usually yes | never |
| Can be bypassed | no | yes — old libraries, JSON, DB |
| Enforced by | the type system | a warning you can ignore |
| Off switch | none | `<Nullable>disable</Nullable>` |

It's switched on for this whole repo in
[Directory.Build.props](../../Directory.Build.props) — one line, inherited by every
project, so no `.csproj` can drift out of sync.

---

## `?.` and `??` — reading a maybe

These two are **exactly** the JavaScript operators of the same name, with the same
meaning. `a?.b` and `a ?? b` behave identically in both languages. You already own
this.

From [Program.cs](../../src/Esports.Console/Program.cs):

```csharp
final.Winner?.Name ?? "nobody yet"
```

Read it left to right:

- `final.Winner` → might be a `Team`, might be nothing
- `?.Name` → *if* there's a team, take its name. **If not, stop the whole chain here**
  and produce nothing
- `?? "nobody yet"` → if what came out was nothing, use this instead

```
   final.Winner   ?.Name    ??  "nobody yet"
        │            │            │
        ▼            │            │
   ┌─────────┐       │            │
   │ a Team? │       │            │
   └────┬────┘       │            │
        │            ▼            │
   has a team ──► .Name ──────► "T1"        result: "T1"
        │
   is nothing ──► SKIPPED ───► nothing ──► "nobody yet"
                  (short-circuits: never touches .Name,
                   so there is no crash to catch)
```

**`?.` short-circuits the rest of the chain, not just the next step.** In
`a?.b.c.d`, if `a` is null then `b`, `c` and `d` are all skipped and the whole
expression is null. It is not "skip one dot" — it is "abandon the chain".

That is precisely **mapping over an option**. `winner?.Name` is
`map (\t -> t.Name) winner`. And `?? "nobody yet"` is `fromMaybe "nobody yet"` /
`getOrElse`.

`??` also works on `int?`, which is where its `HasValue` behaviour shows:

```csharp
int? seed = null;
Console.WriteLine($"seed ?? 99       : {seed ?? 99}");
```

```
seed ?? 99       : 99
```

**The difference from JavaScript:** JS has `?.` and `??` but nothing like the `?`
*annotation*. In JS, every value might be null and nothing tells you which ones. In C#,
the compiler has an opinion, checks it, and warns you. The operators are the same; the
static analysis is new.

---

## `!` — the one to be afraid of

`!` after an expression is the **null-forgiving operator**. It means "compiler, I know
better, be quiet."

**It generates no code. It checks nothing. It protects nothing.** It removes a warning
and that is the entire feature.

```csharp
Team? notReally = null;
Console.WriteLine(notReally!.Name);
```

That compiles clean — no warning at all. And then:

```
notReally!.Name  : NullReferenceException
```

The `!` did not make it safe. It made the compiler stop telling you it wasn't.

This is `unsafeFromJust` / `Option.get` / `fromJust`, and it deserves exactly the same
fear. In your world you'd never reach for `fromJust` casually — the name is a warning
label. C#'s spelling is a single friendly-looking character, which is worse, because it
doesn't *look* dangerous.

**The rule for this repo:** every `!` carries a comment saying why it cannot be null.
If you can't write that comment, you don't know it can't be null, and you need a real
check instead. There are currently **zero** null-forgiving `!` operators anywhere in
[src/Esports.Console](../../src/Esports.Console) — that is the intended baseline.
(The `!` in `!ReferenceEquals(...)` at
[Match.cs:88](../../src/Esports.Console/Match.cs#L88) is ordinary logical *not* — a
different operator that happens to share the character.)

The honest alternative, almost always:

```csharp
if (winner is null) { return "nobody yet"; }   // handle it
return winner.Name;                            // compiler now knows it's fine
```

After that `if`, the compiler's null-tracking updates and `winner.Name` needs no `!`.
Handling the case is usually *shorter* than silencing it.

---

## `int?` is a completely different feature wearing the same syntax

Say this plainly, because the syntax is a trap:

| | `string?`, `Team?`, `MatchScore?` | `int?`, `bool?`, `DateTime?` |
|---|---|---|
| Kind | reference type | value type |
| What `?` is | a compile-time annotation | a real generic type, `Nullable<int>` |
| At runtime | **gone** | **a struct with two fields** |
| Members | none — it's just the type | `.HasValue`, `.Value`, `.GetValueOrDefault()` |
| `typeof` it | compile error | ``System.Nullable`1[System.Int32]`` |
| Costs anything | no | yes, it's a bigger value |

```csharp
int? seed = null;
Console.WriteLine($"typeof(int?)     : {typeof(int?)}");
Console.WriteLine($"seed.HasValue    : {seed.HasValue}");
```

```
typeof(int?)     : System.Nullable`1[System.Int32]
seed.HasValue    : False
```

`Nullable<int>` really is the `Option<int>` you know — a wrapper holding a `bool` and
an `int`, present at runtime, with `HasValue` as its `isSome`.

```
  int?  =  Nullable<int>            string?  =  string
           ┌──────────────┐                      ┌──────────┐
           │ HasValue: F  │                      │ (nothing │
           │ Value:    0  │                      │  added)  │
           └──────────────┘                      └──────────┘
           a real struct                         an annotation
           on the stack                          the compiler
                                                 deletes
```

**Why two different things share one spelling:** a reference variable already has a
representation for "nothing" — an address of zero. So the language only needed to *track*
it. A value type has no spare bit patterns — every `int` means a number — so for those
C# had to build a real wrapper. Same problem, two different solutions, one syntax.

The practical consequence: `.HasValue` compiles on `int?` and does not exist on
`string?`. If you find yourself reaching for it and it isn't there, this is why. Use
`is null` / `is not null`, which works on both.

---

## When `null` is a legitimate answer

Nullable is not a failure to think. Sometimes absence is the honest truth.

[Match.cs:117](../../src/Esports.Console/Match.cs#L117):

```csharp
// Who won. Null is a real answer here: not played yet, a draw, cancelled.
public Team? Winner
{
    get
    {
        if (State == MatchState.Forfeited)
        {
            return ForfeitWinner;
        }

        if (State != MatchState.Completed || Score == null || Score.IsDraw)
        {
            return null;
        }

        return Score.HomeWon ? HomeTeam : AwayTeam;
    }
}
```

There are **three** separate legitimate reasons this returns nothing — not played, a
draw, cancelled — and none of them is an error. A draw is a perfectly good match with
no winner. The type says so:

```
  --- a draw ---
  T1 vs GEN [Completed] 1-1
  Winner: nobody - it was a draw
```

The test to apply:

> **Is "nothing" a real answer to the question this property asks?**
> Yes → nullable is correct. No → nullable is laziness.

`Match.Winner` — yes, a drawn match genuinely has no winner. Nullable.
`Match.HomeTeam` — no. A match with no home team is not a match. **Not** nullable, and
[the constructor](../../src/Esports.Console/Match.cs#L27) makes sure of it.

Notice the shape of [Match.cs](../../src/Esports.Console/Match.cs): `HomeTeam` and
`AwayTeam` are non-nullable, `Score` and `ForfeitWinner` are nullable. That is not an
accident — it is the class stating which four things are always true and which two are
sometimes absent. **Read a class's `?`s as its summary of what it promises.**

---

# Part 2 — refusing invalid objects

## The problem: if you check everywhere, you will miss one

Suppose `Match` accepted anything, and the rules lived at the call sites instead:

```csharp
// in the fixture generator
if (!ReferenceEquals(home, away)) { schedule.Add(new Match(home, away)); }

// in the bracket builder
if (home != away) { bracket.Add(new Match(home, away)); }

// in the CSV importer
bracket.Add(new Match(home, away));     // whoever wrote this forgot
```

Three call sites, three chances, and the third one is the bug. Worse, the second one
uses `!=` on a class — which compares **addresses**, so it happens to work here, but
only by luck, and it would break the moment `Team` became a `record`. (Straight back to
[classes vs records](classes-vs-records.md).)

The count of places to get this wrong only grows: the API on Day 9, the seeding code,
the admin tool. Every new caller is a new chance to forget.

## The pattern: check once, at construction

**Validate where the object is born, throw if it's wrong, and an invalid object can
never exist anywhere.** Not in a field. Not in a list. Not even briefly in a local
variable someone forgot about.

```
  CHECK AT EVERY USE                CHECK AT CONSTRUCTION
  ==================                =====================

   caller ──┐                        caller ──┐
   caller ──┼──► [check] ──► obj     caller ──┼──► [ CHECK ] ──► obj
   caller ──┘     (×3, one            caller ──┘     (×1)
                   forgotten)
                                     every object that exists
   some objects are invalid          is valid, by construction
```

This is a **smart constructor**, exactly as you already write one: don't export the raw
constructor, export a function that can refuse. Same instinct as `private set` from
Day 2 — make illegal states unrepresentable, not merely discouraged.

## The three real guards in this codebase

### 1. A team cannot play itself

[Match.cs:31](../../src/Esports.Console/Match.cs#L31):

```csharp
public Match(Team homeTeam, Team awayTeam)
{
    // A team cannot play itself. Checking here means a nonsensical
    // Match can never exist, rather than being caught later somewhere.
    if (ReferenceEquals(homeTeam, awayTeam))
    {
        throw new ArgumentException("A team cannot play itself");
    }
    ...
```

```
  refused : new Match(t1, t1) -> ArgumentException: A team cannot play itself
```

`ReferenceEquals` rather than `==` on purpose: the rule is "the same team object", a
question about **identity**, and `ReferenceEquals` asks that question directly and can
never be changed out from under you by someone overriding `==`.

### 2. A result cannot be recorded out of state

[Match.cs:66](../../src/Esports.Console/Match.cs#L66):

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

```
  refused : record a result twice -> Cannot record a result for a match that is Completed
  refused : record a result before starting -> Cannot record a result for a match that is Scheduled
```

Two different mistakes, one guard, and the message names the state it actually found.
See [Match.cs:42](../../src/Esports.Console/Match.cs#L42) for the full legal state
diagram — each transition method enforces one arrow.

### 3. A score cannot be negative

[MatchScore.cs:30](../../src/Esports.Console/MatchScore.cs#L30):

```csharp
// A guard: a score cannot be negative. Throwing here means an invalid
// MatchScore can never exist anywhere in the program, not even briefly.
public static MatchScore Create(int home, int away)
{
    if (home < 0 || away < 0)
    {
        throw new ArgumentException($"Scores cannot be negative: {home}-{away}");
    }

    return new MatchScore(home, away);
}
```

```
  refused : MatchScore.Create(-1, 3) -> ArgumentException: Scores cannot be negative: -1-3
```

There is a fourth, at [Match.cs:88](../../src/Esports.Console/Match.cs#L88) — you
cannot forfeit to a team that isn't in the match:

```
  refused : m.Forfeit(someOtherTeam) -> ArgumentException: DRX is not playing in this match
```

### What the guards bought

| Was possible before | Now impossible | Enforced at |
|---|---|---|
| A match where a team plays itself | ✓ | [Match.cs:31](../../src/Esports.Console/Match.cs#L31) |
| Recording a result twice | ✓ | [Match.cs:68](../../src/Esports.Console/Match.cs#L68) |
| Recording a result before kickoff | ✓ | [Match.cs:68](../../src/Esports.Console/Match.cs#L68) |
| A negative score | ✓ (via `Create`) | [MatchScore.cs:32](../../src/Esports.Console/MatchScore.cs#L32) |
| Forfeiting to an uninvolved team | ✓ | [Match.cs:88](../../src/Esports.Console/Match.cs#L88) |

---

## `ArgumentException` vs `InvalidOperationException`

Both stop the program. Choosing between them is not pedantry — it is a message to
whoever is reading the stack trace at 2am with the site down.

| | `ArgumentException` | `InvalidOperationException` |
|---|---|---|
| Means | **that input was wrong** | **you can't do that right now** |
| The fault is in | the values passed in | the *timing* / the object's state |
| Fix by | passing different arguments | doing something else first |
| Would retrying help? | no, never | maybe — the state may change |
| Example here | `new Match(t1, t1)` | `RecordResult` on a `Completed` match |

```
   Something went wrong at the door.
                 │
     ┌───────────┴────────────┐
     │                        │
  Would ANY caller,      Would this SAME call
  at ANY time, be        have worked a moment
  wrong to pass this?    earlier / later?
     │                        │
     ▼                        ▼
  ArgumentException      InvalidOperationException
  "your arguments"       "your timing"
```

Why it matters at 2am: the two point you at completely different bugs.

- `ArgumentException` → **someone built a bad value.** Look upstream at where the data
  came from: a parser, a form, an import, a caller doing arithmetic wrong. The fix is
  in the data path.
- `InvalidOperationException` → **the values were fine, the order was wrong.** Look at
  the sequence of calls: a race, a retry that ran twice, a workflow step skipped, a
  webhook that arrived out of order. The fix is in the control flow.

Getting it backwards sends the on-call engineer to the wrong half of the system. The
exception type is the first line of the diagnosis, and it costs nothing to get right.

There is also `ArgumentNullException` (a specialisation of `ArgumentException`, for the
specific case of a required argument being null) and `ArgumentOutOfRangeException` (for
a value that's the right kind of thing but outside the allowed range). `Create`'s
negative check is arguably the latter; `ArgumentException` is not wrong, just less
specific.

---

## Why a static factory (`MatchScore.Create`) instead of a constructor?

`MatchScore` is a positional `record`
([MatchScore.cs:11](../../src/Esports.Console/MatchScore.cs#L11)), so its constructor
is generated by the compiler — there is no constructor body to put a check in without
giving up the one-line declaration. That's the immediate reason. But a factory earns
its place even where a constructor body exists:

1. **It has a name.** `MatchScore.Create(3, 1)` says what it does. A constructor is
   always named after the type, so five ways of building one are five overloads that
   differ only by argument types. `Create`, `FromSeries`, `Forfeit` read at the call
   site.
2. **It can refuse before anything is allocated.** The check runs first; `new` only
   happens if it passes.
3. **It can return something other than a brand-new object** — a cached instance, a
   shared "0-0", a subtype. A constructor must always return a fresh object of exactly
   that type.
4. **It reads as a smart constructor**, which is exactly what it is.

### The honest cost, and it is a real one

**A factory does not remove the constructor. It only adds a second way to build the
type.** The `record`'s generated constructor is still public, and it still skips the
check:

```csharp
MatchScore bad = new MatchScore(-1, 3);
Console.WriteLine(bad);
```

```
built -1-3   ALLOWED : new MatchScore(-1, 3)
```

**That is a genuine hole in the current code.** `MatchScore.Create` is a guard you can
walk around, and nothing in the type stops you. It is a convention, not a constraint —
the exact thing this whole page argues against.

```
   WHAT WE WANTED                  WHAT WE HAVE
   ==============                  ============

   caller ──► Create ──► [✓] ──► obj   caller ──► Create ──► [✓] ──► obj
                                      caller ──► new ─────────────► obj
   one door, guarded                             unguarded side door
```

> **TODO:** close this. The fix is to stop using a positional record for a type that
> has an invariant — declare the properties in the body with `private init` accessors,
> or use a plain record with a private constructor, so `Create` is the only way in.
> Until then, the negative-score rule holds by discipline alone.

Contrast [Match.cs:27](../../src/Esports.Console/Match.cs#L27), which has **no** hole:
the guard is in the only constructor, so `new Match(t1, t1)` genuinely cannot produce
an object. That is what the factory is trying to imitate, and not quite achieving.

**How a senior reads this:** the difference between "checked" and "uncheckable" is the
whole point. A guard that a caller can route around is a lint rule with extra steps.
Worth shipping as-is while learning — worth a `TODO` so it doesn't silently become
permanent.

---

## Why not a `Result<T>` type instead of throwing?

This is the obvious question from where you're standing. `Either String MatchScore`
puts failure **in the signature**, where the compiler forces every caller to deal with
it. Throwing hides it. Why would anyone give that up?

**Because C# never built the surrounding machinery, and half of it can't be
retrofitted.**

- **There is no `Result` in the standard library.** No `Either`, no `Validation`,
  nothing. You'd write your own, and then every library you use has its own, and they
  don't compose.
- **There are no checked exceptions.** A C# signature says nothing about what can
  throw. Java tried this and the ecosystem hated it hard enough that C# deliberately
  left it out.
- **There is no `do` notation and no monad support in the language.** In your world you
  chain twenty fallible steps and the plumbing is invisible. In C# you'd write the
  unwrapping by hand at every step, at every layer, all the way up. It is a lot of
  code, and it is the code everyone gives up writing by month three.
- **The whole platform throws.** `int.Parse`, file I/O, every database driver, every
  HTTP client. You cannot opt out of `try`/`catch`; you can only add a second error
  mechanism on top of it.

### The convention C# actually settled on

```
   Is this failure EXPECTED in normal operation?
                      │
        ┌─────────────┴──────────────┐
       YES                          NO
        │                            │
        ▼                            ▼
   return T?                    throw
   or bool TryX(out T)          ArgumentException /
                                InvalidOperationException
        │                            │
   "the match has no winner"    "a team cannot play itself"
   "no player with that tag"    "the caller has a bug"
```

- **Expected miss** → return a **nullable**, or use the `TryX` pattern
  (`int.TryParse(s, out int n)` returns `bool` and never throws). Both put the
  possibility of failure in the type.
- **Invalid or exceptional** → **throw**. The caller has a bug, or the world is broken.
  There is nothing sensible for them to do except fail loudly.

`Match.Winner` returning `Team?` is the first branch. `new Match(t1, t1)` throwing is
the second. Both appear in the same file, deliberately.

### What is genuinely lost

**Failure is invisible in the signature.** `public void RecordResult(int, int)` does
not mention that it can throw. Nothing forces a caller to handle it. Nothing stops a
refactor from introducing a new throw into a method nobody wraps. Compared to
`Either`/`Result`, where the compiler is on your side, this is a real and permanent
loss — not a trade you'd make from scratch, but the one the language made.

The partial recoveries, in order of how much they help:

1. **Nullable returns and `TryX` for anything routinely absent.** This covers most of
   what you'd have used `Option` for anyway.
2. **`<summary>` doc comments naming what a method throws.** Documentation, not
   enforcement, but IntelliSense shows it.
3. **Guards at construction**, so there are fewer states from which anything *can*
   throw. This is the one that does the real work: if invalid objects can't exist, most
   of the exceptions you'd have needed never come up.

That third point is why Part 1 and Part 2 are on the same page. Making illegal states
unconstructable is how you buy back some of what you lost by not having `Result`.

---

# Coming from functional programming

| C# | What you already know | Where it leaks |
|---|---|---|
| `MatchScore?` | `Option<MatchScore>` | **erased at runtime — a linter, not a type** |
| `?.` | `map` over an option | same, no leak |
| `??` | `fromMaybe` / `getOrElse` | same, no leak |
| `!` | `unsafeFromJust` | same danger, far friendlier-looking |
| `int?` | `Option<int>` — genuinely | none. `Nullable<int>` is real |
| `is null` | `isNothing` | works on both kinds of `?` |
| `MatchScore.Create` | a smart constructor | the raw constructor is still public |
| guards in a constructor | make illegal states unrepresentable | enforced by throwing, not by types |
| `throw` | `Left e` / a partial function | invisible in the signature |

**The one to internalise:** you have spent years trusting that `Option` *is* the
guarantee. In C#, `?` is the *request* for a guarantee, and the compiler does its best.
The gap between those two is where production bugs live, and it is widest exactly at
the edges of your program — which is Part 1's whole warning.

---

# Coming from JavaScript

You know `?.` and `??` already, and **they mean the same thing in C#** — same
short-circuiting, same "only null triggers the fallback, not falsy values". In JS,
`0 ?? 5` is `0` while `0 || 5` is `5` — and C# agrees about the first: given
`int? n = 0`, `n ?? 5` is `0`. Nothing new to learn there. (C# has no falsy-value
trap at all, because `||` only accepts `bool` — `0 || 5` is a compile error.)

The genuinely new thing is the **`?` annotation and the compile-time analysis behind
it**. JS has no equivalent — every value might be `null` and nothing tells you which.
TypeScript's `string | null` with `strictNullChecks` is the close cousin, and it leaks
in the same way for the same reason: it's a compile-time story erased before the code
runs, so a `null` from an API response walks straight through it. If you've been bitten
by that in TypeScript, you already understand C#'s nullability exactly.

| JS / TS | C# |
|---|---|
| `a?.b` | `a?.b` — identical |
| `a ?? b` | `a ?? b` — identical |
| `a!.b` (TS) | `a!.b` — identical, equally unsafe |
| `string \| null` + `strictNullChecks` | `string?` + `<Nullable>enable</Nullable>` |
| `if (!x) throw new Error(...)` | `if (x is null) throw new ArgumentException(...)` |

---

# How a senior thinks about this

- **Validate at the boundary, then trust inward.** Check once, where data enters — a
  constructor, a factory, an API endpoint. Everything deeper assumes the object is
  valid, because it provably is. A codebase that re-checks the same rule at every layer
  is a codebase where nobody trusts any layer.

- **Prefer "cannot be constructed" over "is checked for".** A check can be skipped; a
  type that cannot hold the bad value cannot. Every time you're about to write the same
  `if` twice, that's a type asking to be born.

- **Be suspicious of every `!`.** When reviewing, treat it as a claim needing evidence.
  Nine times out of ten the author meant "I couldn't be bothered", and the tenth time
  they were right but should have written why. It should be rare enough to be
  remarkable.

- **Read a nullable field as a question the type is asking you.** `MatchScore? Score`
  is asking *what should happen when there is no score?* Every place you touch it, you
  answer. If the answer is always the same, that's a hint the absence belongs somewhere
  else — which is exactly why
  [MatchState](../../src/Esports.Console/MatchState.cs) arrived on Day 4: five states
  cannot be expressed by one nullable field.

- **A nullable that's never null is a lie, and so is a non-nullable that is.** Both
  erode trust in every other `?` in the file. If a field is genuinely always set after
  construction, make it non-nullable and set it in the constructor.

- **Pick the exception type for the reader, not for yourself.** You will never see the
  stack trace. Someone tired will.

- **Leave a `TODO` when a guard is bypassable.** Known-and-written-down beats
  known-and-forgotten. See the `MatchScore.Create` note above.

---

# Where this reappears

- **Day 9 — the API.** Request bodies arrive as JSON. A field the client omitted
  deserialises to `null` regardless of what the C# type says, so every incoming model
  needs validation at the boundary. Same guard pattern, new door.

- **Day 10 — the database.** This is the one to remember. **Values loaded from a
  database bypass the compiler's nullability analysis entirely.** The ORM constructs
  objects by reflection, a `NULL` column lands in a non-nullable property, and nothing
  warns you — the exact situation where `?` being erased stops protecting you. The
  C# `?` and the SQL `NULL`/`NOT NULL` are two separate declarations that you have to
  keep in sync **by hand**, and when they disagree the compiler is confidently wrong.

- **Day 14 — services.** Business rules move out of entities into service classes, and
  the question "should this rule be a constructor guard or a service check?" becomes a
  real design decision rather than a default.

---

# Traps

- **`?` is erased.** It warns. It does not prevent. A `null` can arrive at runtime from
  any code the compiler didn't analyse.

- **A warning is not an error here.** `TreatWarningsAsErrors` is deliberately `false`
  in [Directory.Build.props](../../Directory.Build.props) while learning, so CS8602
  scrolls past in the build log and the program runs anyway. Read the warnings.

- **`!` compiles away to nothing.** `notReally!.Name` threw a `NullReferenceException`
  with no warning at all. Silencing the compiler does not silence the runtime.

- **`int?` and `string?` are different features.** `.HasValue` exists on one and not the
  other. `typeof(string?)` is a compile error. Use `is null` / `is not null`, which
  works on both and reads the same.

- **`?.` short-circuits the *whole* chain.** `a?.b.c.d` skips `b`, `c` *and* `d`. It is
  not "skip one step".

- **`?.` on a method call that returns `void` still works** and does nothing if the
  target is null — which means `thing?.DoImportantWork()` can silently skip the
  important work. The compiler will not mention it.

- **The raw constructor survives a factory.** `new MatchScore(-1, 3)` built `-1-3`
  successfully. A factory is only the *preferred* door unless you close the other one.

- **`ReferenceEquals`, not `==`, for identity guards.** `==` can be overridden; on a
  `record` it compares contents. The "a team cannot play itself" rule means *the same
  object*, so it must ask about the address.

- **Guarding in a constructor doesn't help if a property has a public setter.** The
  object is valid for exactly as long as it takes someone to assign to it. `private
  set` throughout [Match.cs](../../src/Esports.Console/Match.cs) is what keeps the
  constructor's guarantee true afterwards.

- **A guard that throws in a constructor leaves no object behind** — the `new`
  expression never produces a value, so there is nothing half-built to clean up. That
  is the whole reason this placement is safe.

---

*Introduced: [Day 3](../days/day-03-equality-records-and-nothing.md)*
