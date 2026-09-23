# Day 3 — Equality, records, and how to say "nothing"

> **Goal:** Know when two objects count as "the same", pick `class` vs `record` deliberately, and represent a match that hasn't been played yet.
> **Date:** 2026-09-22 · **Tag:** `day-03`

---

## 0. Where we were

[Day 2](day-02-collections-and-references.md) added
[Team](../../src/Esports.Console/Team.cs), holding its roster in a `List<Player>` with
the five-player limit enforced inside the class. It also delivered the biggest surprise
so far: **two variables can point at the same object**, so `Player b = a;` followed by
`b.RecordWin()` changes `a` too.

**What was missing:** two teams need something to play. A match needs a result — and
that immediately raises a question Day 2 could not answer: are two identical results
"the same" result? And what does a match hold as its score *before* it has been played?

*Previous: [Day 2 — Collections and references](day-02-collections-and-references.md)*

---

## 1. What we built today

- [MatchScore.cs](../../src/Esports.Console/MatchScore.cs) — a `record` holding a
  result, that knows things about itself (`IsDraw`, `Margin`, `HomeWon`) and refuses
  to be built with negative numbers.
- [Match.cs](../../src/Esports.Console/Match.cs) — pairs two teams, holds a score that
  **might not exist yet**, and guards three rules in its constructor and methods.

---

## 2. The flow — each step forced by the last

This is the order the problems actually arrived in. Read it as a story, not a list.

### Step 1 — Two identical players are not equal

```csharp
Player p1 = new Player("Faker", 1847);
Player p2 = new Player("Faker", 1847);

Console.WriteLine(p1 == p2);
```

```
False
```

Same tag. Same rating. `False`.

But with a string, the same shape gives the opposite answer:

```csharp
string s1 = "Faker";
string s2 = "Faker";
Console.WriteLine(s1 == s2);
```

```
True
```

### Step 2 — Why: `==` compares the arrows

Straight on from Day 2. `p1` and `p2` each hold an **address**, and `new` ran twice,
so there are genuinely two objects at two addresses.

```
 p1 [ →──┐              p2 [ →──┐
         ▼                      ▼
   ┌──────────┐           ┌──────────┐
   │ Faker    │           │ Faker    │     two objects
   │ 1847     │           │ 1847     │     two addresses
   └──────────┘           └──────────┘     p1 == p2  →  False
```

`==` on a class you wrote never looks at `GamerTag` or `Rating` at all. It compares
addresses and stops.

`string` is the exception because C# **overrides** `==` for it specifically. That is a
special case written into the language, not the default behaviour.

### Step 3 — The question C# is really asking

**Is this thing an identity, or is it a value?**

- Two players both called "Faker" at 1847 — the same player? **No.** Two different
  people who happen to share stats. A player keeps being himself when his rating
  changes. Players have **identity**.
- Two scores of `3-1` — the same score? **Yes, obviously.** A score is nothing but its
  contents. Scores are **values**.

C#'s default is right for the first and wrong for the second. The functional default
(structural equality) is right for the second and wrong for the first. So you pick,
**per type**.

### Step 4 — `record` is the value case

```csharp
public record MatchScore(int Home, int Away);
```

```csharp
MatchScore a = new MatchScore(3, 1);
MatchScore b = new MatchScore(3, 1);

Console.WriteLine($"a == b       : {a == b}");
Console.WriteLine($"same object? : {ReferenceEquals(a, b)}");
```

```
a == b       : True
same object? : False
```

**Two separate objects that count as equal.** That is structural equality — the
functional default, available on request.

### Step 5 — The question that came up: why a type at all?

> *"What's the point of making this class? Couldn't this be a method?"*

Fair, and worth answering properly.

A **method** is a verb — it does something. A **type** is a noun — a kind of thing that
can exist. You can't swap one for the other. The real question is: *do I need a type
here, or can I just pass two `int`s around?*

**Without the type**, every place that looks at a result writes this out:

```csharp
if (homeScore == awayScore)      { verdict = "draw"; }
else if (homeScore > awayScore)  { verdict = "home win"; }
else                             { verdict = "away win"; }

int margin = Math.Abs(homeScore - awayScore);
```

Standings code, bracket code, reporting code, the API later — the same five lines
copied into each. Someone writes `>=` instead of `>` and you have a bug that only
appears on draws.

Worse: nothing stops you swapping the arguments. `Complete(1, 3)` and `Complete(3, 1)`
both compile.

**With the type**, that knowledge lives once and everywhere else asks:

```csharp
foreach (MatchScore score in results)
{
    if (score.IsDraw)        { verdict = "draw"; }
    else if (score.HomeWon)  { verdict = "home win"; }
    else                     { verdict = "away win"; }

    Console.WriteLine($"{score}  {verdict}  margin {score.Margin}");
}
```

```
3-1  home win  margin 2
2-2  draw      margin 0
0-3  away win  margin 3
```

**The honest test:** *does this type know something, or is it just a bag?* For two
numbers with no behaviour and no rules, a type is barely worth it. The moment it has
behaviour that would otherwise be duplicated, or a rule about what's valid, it clearly
earns its place.

### Step 6 — A match with no score yet

A match pairs two teams and has a score. **But a match that hasn't been played has no
score at all.**

`0-0` is wrong — that's a real result, a draw. You need to say *"nothing here yet"*:

```csharp
public MatchScore? Score { get; private set; }
                 ^
```

That `?` means **"this might be nothing"**, and the compiler then tracks it.

---

## 3. How it works behind the scenes

### What `record` generates for you

[MatchScore.cs:11](../../src/Esports.Console/MatchScore.cs#L11) is one line of
declaration. From it the compiler writes:

- a constructor taking `Home` and `Away`
- `==` and `!=` comparing contents
- `Equals()` and `GetHashCode()`
- a `ToString()` listing every property
- properties that are **read-only after construction**
- the `with` expression

Compare with `Player` — 48 hand-written lines, and it still prints as just `Player`.

```
  ONE LINE OF SOURCE                    WHAT THE COMPILER WRITES
  ==================                    ========================

  public record MatchScore(             constructor(int, int)
      int Home,                         Home  { get; init; }
      int Away);                        Away  { get; init; }
                                        operator ==   (compares CONTENTS)
                                        operator !=
                                        Equals(object)
                                        Equals(MatchScore)
                                        GetHashCode()
                                        ToString()
                                        Deconstruct(out int, out int)
                                        <Clone>$        (powers `with`)

  public class Player { ... }           nothing. 48 hand-written lines,
                                        and == still compares addresses.
```

### The choice, as a decision

```
              Does this thing have an identity that
              survives its values changing?
                          │
             ┌────────────┴────────────┐
            YES                        NO
             │                          │
             ▼                          ▼
          class                      record
             │                          │
   Player - Faker is still     MatchScore - any 3-1 is
   Faker after a rating        any other 3-1. Nothing
   change. Two people can      else to it.
   share stats and still
   be two people.
             │                          │
   == compares ADDRESSES       == compares CONTENTS
```

### `with` makes a copy, it does not mutate

```csharp
MatchScore a = new MatchScore(3, 1);
MatchScore flipped = a with { Home = 1, Away = 3 };

Console.WriteLine($"a is still : {a}");
Console.WriteLine($"flipped is : {flipped}");
```

```
a is still : 3-1
flipped is : 1-3
```

`a` is untouched. That's record-update syntax — the same thing you've written for
years, spelled `with`.

**The catch:** `with` is a **shallow** copy. If a record held a `List<T>`, `with` would
copy the *arrow* to that list, not the list. Straight back to Day 2.

### `?` is erased at runtime — this one matters

`MatchScore?` and `MatchScore` compile to **byte-identical** code.

The `?` does not create a wrapper, does not allocate anything, does not exist when the
program runs. All it does is make the compiler analyse your code and warn you. A `null`
can still arrive at runtime from an older library, from JSON, from a database.

So it's a very good linter, **not a proof**. Treat it as a strong safety net whose
confidence is only as good as the code compiled with the feature switched on.

### The full `Match` code

[Match.cs](../../src/Esports.Console/Match.cs) as it stood at the end of Day 3 (Day 4
replaces the `HasBeenPlayed` part with a proper state machine):

```csharp
public class Match
{
    // These hold ARROWS to Team objects, not copies.
    public Team HomeTeam { get; private set; }
    public Team AwayTeam { get; private set; }

    // `?` = this might be nothing. An unplayed match has no score, and
    // 0-0 would be a lie because that is a real result - a draw.
    public MatchScore? Score { get; private set; }

    public Match(Team homeTeam, Team awayTeam)
    {
        // Guard in the constructor: a nonsensical Match can never exist.
        if (ReferenceEquals(homeTeam, awayTeam))
        {
            throw new ArgumentException("A team cannot play itself");
        }

        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        Score = null;          // explicitly: not played yet
    }

    public bool HasBeenPlayed => Score != null;

    public void RecordResult(int homeScore, int awayScore)
    {
        if (HasBeenPlayed)
        {
            throw new InvalidOperationException("already finished");
        }

        // Create(), not `new` - it rejects negative numbers.
        Score = MatchScore.Create(homeScore, awayScore);
    }

    // Team? because there may be no winner: not played, or a draw.
    public Team? Winner
    {
        get
        {
            if (Score == null || Score.IsDraw) { return null; }

            return Score.HomeWon ? HomeTeam : AwayTeam;
        }
    }
}
```

```
T1 vs GEN (not played)
Played? False
Winner: nobody yet

T1 3-1 GEN
Played? True
Winner: T1
Margin: 2

refused: T1 vs GEN already finished 3-1
refused: A team cannot play itself
```

### `?.` and `??` read left to right

```csharp
final.Winner?.Name ?? "nobody yet"
```

- `final.Winner` → might be a `Team`, might be nothing
- `?.Name` → *if* there's a team, take its name; **if not, stop here, produce nothing**
- `?? "nobody yet"` → if what came out was nothing, use this instead

```
   final.Winner  ?.Name   ??  "nobody yet"
        │           │           │
        ▼           │           │
   ┌─────────┐      │           │
   │ a Team? │      │           │
   └────┬────┘      │           │
        │           ▼           │
    has a team ──► .Name ──► "T1"          done, "T1"
        │
    is nothing ──► SKIPPED ──► nothing ──► "nobody yet"
                   (short-circuits:
                    never touches .Name,
                    so no crash)
```

---

## 4. Why it's needed

**Why `record` for a score:** without it, `==` on two identical scores would be
`False`, which is nonsense for a value. And every comparison, every "was it a draw",
every margin calculation would be duplicated at each call site.

**Why `class` for a player:** with a record, two different people called "Faker" at
1847 would compare as equal, and a rating change would make a player "a different
player". Both wrong.

**Why `?` on the score:** every other value is a lie. `0-0` is a draw. There is no
number that means "hasn't happened".

**Why the constructor guards:** this pattern now appears three times, and it is the
single most valuable habit from today.

| Now impossible | Enforced at |
|---|---|
| A match where a team plays itself | [Match.cs:29](../../src/Esports.Console/Match.cs#L29) — constructor |
| Recording a result twice | [Match.cs:46](../../src/Esports.Console/Match.cs#L46) — `RecordResult` |
| A negative score | [MatchScore.cs:31](../../src/Esports.Console/MatchScore.cs#L31) — `Create` |

```
refused: T1 vs GEN already finished 3-1
refused: A team cannot play itself
refused: Scores cannot be negative: -1-3
```

**Check in the constructor and an invalid object can never exist** — not even briefly,
not even in a variable someone forgot about. Same discipline as a smart constructor.

---

## 5. Where it fits

`Match` is the third entity. `Tournament` will hold many matches and decide who plays
whom.

`MatchScore` being a `record` matters more later: on Day 10 these become database rows,
and values versus identities is exactly the distinction a database draws between a
column group and a row with a primary key.

---

## 6. Coming from functional programming

**The analogies:**

| C# | What you already know |
|---|---|
| `record` | a product type with structural equality |
| `with` | record-update syntax |
| `MatchScore?` | `Option<MatchScore>` |
| `?.` | mapping over an option, short-circuiting |
| `??` | `fromMaybe` / `getOrElse` |
| `MatchScore.Create()` | a smart constructor |
| `ReferenceEquals` | physical equality |

**Where they leak:**

1. **`?` is erased.** `Option` is a real value in a real type that exists at runtime.
   C#'s `?` vanishes at compile time. This is the biggest leak on the list.
2. **`with` is shallow.** No structural sharing, no persistence. A nested mutable
   collection leaks between "copies".
3. **A record's equality includes every member.** Add a property later and equality
   silently changes behaviour everywhere.
4. **You must choose identity vs value per type.** In a world where everything is an
   immutable value, the question rarely comes up. Here it is a design decision with
   consequences, and getting it wrong is quiet rather than loud.

---

## 7. New C# syntax I met today

| Syntax | Means |
|---|---|
| `public record MatchScore(int Home, int Away);` | a whole type in one line — value equality, constructor, `ToString`, `with` |
| `a with { Home = 1 }` | a modified **copy**; the original is untouched |
| `public bool IsDraw => Home == Away;` | computed property, expression-bodied |
| `public override string ToString()` | replace the version inherited from `object` |
| `public static MatchScore Create(...)` | `static` = belongs to the type, not an instance. Called as `MatchScore.Create(...)` |
| `throw new ArgumentException("...")` | stop now, this input is invalid |
| `try { } catch (ArgumentException ex) { }` | run this, and if that exception comes out, handle it |
| `MatchScore? Score` | might be nothing |
| `Score?.Margin` | if it exists, read `Margin`; otherwise produce nothing |
| `x ?? "fallback"` | if `x` is nothing, use this instead |
| `Score.HomeWon ? HomeTeam : AwayTeam` | inline if-else that produces a **value** |
| `Math.Abs(n)` | strip the minus sign |

---

## 8. Traps and gotchas

- **`==` on your own class compares addresses.** It will happily say two identical
  objects are different. If you want content comparison, use a `record` or write
  `Equals` yourself.

- **A record's auto `ToString()` includes every public property — including computed
  ones.** `MatchScore` printed cleanly until `IsDraw`/`Margin`/`GamesPlayed` were
  added, then it became:
  ```
  MatchScore { Home = 3, Away = 1, IsDraw = False, HomeWon = True, AwayWon = False, Margin = 2, GamesPlayed = 4 }
  ```
  Fixed by writing `public override string ToString() => $"{Home}-{Away}";`

- **`?` protects nothing at runtime.** It is a compile-time analysis only.

- **`with` is shallow.** Nested reference types are shared between the original and
  the copy.

- **Integer division still lurks.** `Margin` works because `Math.Abs` returns an `int`
  and we want an `int`. The moment an average is involved, cast first (Day 2).

---

## 9. Checkpoint questions

1. **Q:** Two `Player` objects with identical tag and rating. `p1 == p2` is `False`.
   Why, in terms of what the variables hold?
   **A:** Each holds an address, and `new` ran twice, so there are two objects at two
   addresses. `==` on a class compares addresses and never inspects the contents.

2. **Q:** `a == b` is `True` for two `MatchScore` values but `ReferenceEquals(a, b)` is
   `False`. How can both be true at once?
   **A:** They are genuinely two separate objects (different addresses), but `record`
   generates an `==` that compares contents instead of addresses.

3. **Q:** Why is `Player` a `class` and `MatchScore` a `record`?
   **A:** A player has an identity that survives its values changing — two people
   called "Faker" are still two people. A score *is* its contents — any 3-1 is any
   other 3-1.

4. **Q:** Why can't an unplayed match just store `0-0`?
   **A:** `0-0` is a real result — a draw. There is no number meaning "hasn't
   happened", which is what `MatchScore?` is for.

5. **Q:** `MatchScore?` versus `MatchScore` — what is different in the compiled
   program?
   **A:** Nothing. The code is byte-identical. `?` is a compile-time annotation that
   drives warnings and is erased.

6. **Q:** Read `final.Winner?.Name ?? "nobody yet"` aloud, left to right.
   **A:** Get the winner; if there is one take its name, otherwise stop and produce
   nothing; if the result was nothing, use `"nobody yet"`.

---

## 10. Commands I ran

```powershell
dotnet run --project src/Esports.Console
```

---

## 11. What's next

Working and committed:
- [MatchScore.cs](../../src/Esports.Console/MatchScore.cs) — record with behaviour and a guard
- [Match.cs](../../src/Esports.Console/Match.cs) — two teams, optional score, three guards

**The loose end:** `HasBeenPlayed` is just `Score != null`. That covers *played /
not played*, but a real tournament match is also **scheduled**, **in progress**,
**forfeited**, **cancelled**. Null cannot express five states.

That is the problem an **enum** solves — next.

**Also still open from Day 2:** `Team._players` is completely hidden, so a team cannot
be enumerated at all. It should be exposed as a read-only view.
