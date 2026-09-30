# Day 7 — A league table, and LINQ

> **Goal:** Count points and rank the teams. Then learn the toolkit C# has for exactly this.
> **Date:** 2026-09-23 · **Tag:** `day-07` · complete

**How this doc works:** the standings calculator gets built in versions. Each version
below is the whole method as it stood at that point, with every line explained. Later
versions improve on earlier ones, and the earlier ones stay here so the change is
visible.

---

## 0. Where we were

[Day 6](day-06-abstract-classes.md) finished the tournament formats. A league can now
generate its fixtures, and every match can be played:

```
=== LCK Spring (Round robin) ===
4 teams, 6 matches

--- results ---
  T1 vs GEN [Completed] 2-0
  T1 vs HLE [Completed] 0-2
  T1 vs DK [Completed] 0-1
  GEN vs HLE [Completed] 2-2
  GEN vs DK [Completed] 0-3
  HLE vs DK [Completed] 0-1
```

**What was missing:** results, but no table. Nobody knows who won.

A knockout has a winner by definition — last one standing. A round robin needs
**points counted and teams sorted**.

*Previous: [Day 6 — Abstract classes](day-06-abstract-classes.md)*

---

## 1. First: what does a table row hold?

```
  Team   Played  Won  Drawn  Lost  Points
  ────────────────────────────────────────
  DK          3    3      0     0       9
  HLE         3    1      1     1       4
```

Each row **is** its contents. Two rows with identical numbers are the same row. That
is the Day 3 question — identity or value — and the answer here is **value**, so it is
a `record`.

[TeamStanding.cs](../../src/Esports.Console/TeamStanding.cs):

```csharp
public record TeamStanding(
    Team Team,        // which team this row is about
    int Played,       // matches finished
    int Won,
    int Drawn,
    int Lost,
    int GamesWon,     // individual games won across all matches
    int GamesLost)
{
    // Football scoring. A computed property - no stored value, this
    // expression runs every time someone reads .Points.
    public int Points => (Won * 3) + Drawn;

    // The usual first tie-break when two teams are level on points.
    public int GameDifference => GamesWon - GamesLost;
}
```

**Why a snapshot and not a stored total:** the row is recalculated from the matches
every time it is asked for. Nothing increments a running `Points` field. That means the
table **cannot drift out of sync** with the results — a whole class of bug avoided,
for free.

---

## 2. Version 1 — plain loops

This is the version you would write knowing only what Days 1–6 covered. It works. It is
also 61 lines, and worth reading carefully because everything after it is a
simplification of exactly this.

```csharp
public static List<TeamStanding> Build(
    IReadOnlyList<Team> teams,
    IReadOnlyList<Match> matches)
{
    // The table we are going to fill in and hand back.
    List<TeamStanding> rows = new List<TeamStanding>();

    // Outer loop: one row per team.
    foreach (Team team in teams)
    {
        // Six counters, all starting at zero. These accumulate as we walk
        // the matches below. This is a fold with the accumulator spelled
        // out by hand.
        int played = 0;
        int won = 0;
        int drawn = 0;
        int lost = 0;
        int gamesWon = 0;
        int gamesLost = 0;

        // Inner loop: look at every match, for every team.
        foreach (Match match in matches)
        {
            // `continue` = skip the rest of this loop body and go to the
            // next match. Unplayed matches contribute nothing.
            if (match.State != MatchState.Completed || match.Score == null)
            {
                continue;
            }

            // Was THIS team playing in THIS match, and on which side?
            // ReferenceEquals, not ==, because two different Team objects
            // sharing a tag are different teams. (Day 2 and Day 3.)
            bool isHome = ReferenceEquals(match.HomeTeam, team);
            bool isAway = ReferenceEquals(match.AwayTeam, team);

            // Not our match. Skip it.
            if (!isHome && !isAway)
            {
                continue;
            }

            played = played + 1;

            // `condition ? a : b` is an inline if-else that produces a
            // VALUE. Read it as: "if isHome, take Home, otherwise Away".
            //
            // This flip is the crux of the whole method. After these two
            // lines the rest of the logic does not care which side the
            // team played on.
            int scoredByUs   = isHome ? match.Score.Home : match.Score.Away;
            int scoredByThem = isHome ? match.Score.Away : match.Score.Home;

            gamesWon  = gamesWon  + scoredByUs;
            gamesLost = gamesLost + scoredByThem;

            if (scoredByUs > scoredByThem)       { won = won + 1; }
            else if (scoredByUs == scoredByThem) { drawn = drawn + 1; }
            else                                 { lost = lost + 1; }
        }

        rows.Add(new TeamStanding(team, played, won, drawn, lost, gamesWon, gamesLost));
    }

    // Sort the table. See section 3 for why this part is the worst of it.
    rows.Sort((a, b) =>
    {
        if (a.Points != b.Points)
        {
            return b.Points - a.Points;
        }

        if (a.GameDifference != b.GameDifference)
        {
            return b.GameDifference - a.GameDifference;
        }

        return b.GamesWon - a.GamesWon;
    });

    return rows;
}
```

**It produces the right answer:**

```
--- standings ---
  #  Team   P   W   D   L   Games    GD  Pts
  ------------------------------------------
  1  DK     3   3   0   0    5-0      5    9
  2  HLE    3   1   1   1    4-3      1    4
  3  T1     3   1   0   2    2-3     -1    3
  4  GEN    3   0   1   2    2-7     -5    1

Champion: Dplus on 9 points
```

---

## 3. What is wrong with version 1

Nothing, functionally. It is correct. But three things are worth naming, because each
one is a place a bug can hide.

**The six counters are six chances to make a typo.** `won = won + 1` in the wrong
branch, or `gamesWon` where `gamesLost` belongs, produces a table that is quietly
wrong. Nothing crashes. You find out when someone asks why the numbers do not add up.

**The sort is the worst part.**

```csharp
rows.Sort((a, b) =>
{
    if (a.Points != b.Points)
    {
        return b.Points - a.Points;
    }
    ...
});
```

To read that you have to know a convention: **return a negative number and `a` sorts
first; positive and `b` sorts first.** Which means `b.Points - a.Points` sorts
descending and `a.Points - b.Points` sorts ascending, and they look almost identical.
Swap them by accident and the whole table is upside down, silently.

**It says HOW, not WHAT.** Read the method and you learn the mechanics of walking two
lists and incrementing counters. You have to reconstruct the *intent* — "count the wins,
sort by points" — from the machinery.

---

## 4. The toolkit: LINQ

C# has a set of methods for exactly this work. They are the same operations you already
know, renamed.

| C# | What you call it |
|---|---|
| `Where` | `filter` |
| `Select` | `map` |
| `OrderBy` / `ThenBy` | `sortBy` |
| `Count` | `length` |
| `Sum` / `Max` / `Min` | the obvious ones |
| `Aggregate` | `fold` / `reduce` |
| `Any` / `All` | `any` / `all` |
| `First` / `FirstOrDefault` | `head` / `headOption` |
| `SelectMany` | `bind` / `flatMap` |

Chaining with `.` is the pipe operator — same left-to-right reading order.

Each piece below was run on its own before being used in the real code.

### 4.1 `Where` — keep only what passes

```csharp
int[] numbers = [2, 7, 4, 9, 1, 8];

var bigOnes = numbers.Where(n => n > 5);
```

```
all of them:
 2 7 4 9 1 8
only the ones bigger than 5:
 7 9 8
```

`n => n > 5` is a **lambda** — a function with no name, written inline. Identical to a
JavaScript arrow function:

```
C#          n => n > 5
JavaScript  n => n > 5
```

The original array is untouched. `Where` produces a **new** sequence.

### 4.2 `Select` — transform every item

```csharp
var bigOnes = numbers.Where(n => n > 5);    // keeps some. Values unchanged.
var doubled = numbers.Select(n => n * 2);   // keeps all.  Values changed.
```

```
original : 2 7 4 9 1 8
Where >5 : 7 9 8
Select*2 : 4 14 8 18 2 16
```

```
  original   2  7  4  9  1  8      6 items
  Where      -  7  -  9  -  8      3 items  <- fewer, same values
  Select     4 14  8 18  2 16      6 items  <- same count, new values
```

| | Does what | Count |
|---|---|---|
| `Where` | keeps items that pass a test | can shrink |
| `Select` | turns each item into something else | always the same |

### 4.3 Chaining

```csharp
var result = numbers
    .Where(n => n > 5)      // first: keep the big ones
    .Select(n => n * 10);   // then: multiply what survived
```

```
start        : 2 7 4 9 1 8
after Where  : 7 9 8
after Select : 70 90 80
```

```
   2 7 4 9 1 8
        |
        v  .Where(n => n > 5)
      7 9 8
        |
        v  .Select(n => n * 10)
    70 90 80
```

**Order matters.** Swap the lines and you multiply first, so every number is over 5 and
nothing gets dropped.

### 4.4 `ToList` — and why LINQ is lazy

`Where` does **not** do the filtering when you write it. Proof — the `WriteLine` inside
the test shows exactly when it runs:

```csharp
int[] numbers = [2, 7, 4];

Console.WriteLine("STEP 1 - building the query");
var bigOnes = numbers.Where(n =>
{
    Console.WriteLine($"        ...checking {n}");
    return n > 5;
});
Console.WriteLine("STEP 2 - query built. Did any checking happen above?");

Console.WriteLine("STEP 3 - looping it");
foreach (int n in bigOnes) { }

Console.WriteLine("STEP 4 - looping the SAME query again");
foreach (int n in bigOnes) { }
```

```
STEP 1 - building the query
STEP 2 - query built. Did any checking happen above?
STEP 3 - looping it
        ...checking 2
        ...checking 7
        ...checking 4
STEP 4 - looping the SAME query again
        ...checking 2
        ...checking 7
        ...checking 4
```

Two facts in that output:

1. **Nothing ran between STEP 1 and STEP 2.** `Where` returned a *description* of work,
   not a result.
2. **STEP 4 did it all again.** One query, looped twice, filtered twice.

`.ToList()` says **do it now, once, and keep the answers**. After it, you hold a real
`List<T>` with real values in it.

**Rule of thumb: if the result will be used more than once, `ToList()` it.**

**The functional bridge, and its leak:** laziness itself is familiar. What is different
is that the underlying data is mutable and the test can have side effects, so
re-enumerating can redo real work or even produce different answers.

### 4.5 `Count` and `Sum` — what replaces the six counters

```csharp
int[] numbers = [2, 7, 4, 9, 1, 8];

Console.WriteLine($"how many altogether : {numbers.Count()}");
Console.WriteLine($"how many over 5     : {numbers.Count(n => n > 5)}");
Console.WriteLine($"how many under 5    : {numbers.Count(n => n < 5)}");
Console.WriteLine($"total of all        : {numbers.Sum(n => n)}");
Console.WriteLine($"total if doubled    : {numbers.Sum(n => n * 2)}");
```

```
how many altogether : 6
how many over 5     : 3
how many under 5    : 3
total of all        : 31
total if doubled    : 62
```

`Count(test)` is `Where` followed by "how many" — these are the same:

```csharp
numbers.Where(n => n > 5).Count()
numbers.Count(n => n > 5)
```

`Sum(selector)` picks a number out of each item and adds them. Here the items *are*
numbers so `n => n` means "use the item itself". In real code the items are objects, so
it becomes `r => r.Us` — "add up the Us field of each".

**A trap:** `results.Count` has no brackets, `results.Count(...)` does.

- `Count` — a property on `List<T>`. It already knows its size. Free.
- `Count()` — a method that walks the sequence.

On a `List` the difference is small. On a lazy query, `Count()` runs the whole query.

### 4.6 `OrderByDescending` and `ThenByDescending`

```csharp
Row[] table =
[
    new Row("T1",  4, -1),
    new Row("GEN", 9,  5),
    new Row("HLE", 4,  3),    // T1 and HLE are tied on 4 points
    new Row("DK",  1, -5)
];

table.OrderByDescending(r => r.Points)
     .ThenByDescending(r => r.Diff)
```

```
unsorted:
   T1   pts=4  diff=-1
   GEN  pts=9  diff=5
   HLE  pts=4  diff=3
   DK   pts=1  diff=-5

OrderByDescending(points):
   GEN  pts=9  diff=5
   T1   pts=4  diff=-1     <-- tied on 4, left in the order they were in
   HLE  pts=4  diff=3
   DK   pts=1  diff=-5

...then ThenByDescending(diff) to break the 4-4 tie:
   GEN  pts=9  diff=5
   HLE  pts=4  diff=3      <-- HLE now above T1: same points, better diff
   T1   pts=4  diff=-1
   DK   pts=1  diff=-5
```

The lambda picks **what to sort by**. `ThenByDescending` only affects ties — GEN and DK
never moved, because points already settled them.

| | |
|---|---|
| `OrderBy` | smallest first |
| `OrderByDescending` | biggest first |
| `ThenBy` | break ties, smallest first |
| `ThenByDescending` | break ties, biggest first |

Chain as many `ThenBy` as there are tie-breakers.

---

## 5. Version 2 — the same calculation in LINQ

[StandingsTable.cs](../../src/Esports.Console/StandingsTable.cs):

```csharp
public static List<TeamStanding> Build(
    IReadOnlyList<Team> teams,
    IReadOnlyList<Match> matches)
{
    // Work out the finished matches ONCE. Without .ToList() this filter
    // would re-run for every team - four teams, four passes. Still the
    // right answer, which is why the bug is easy to miss.
    List<Match> finished = matches
        .Where(m => m.State == MatchState.Completed && m.Score != null)
        .ToList();

    // One row per team, then sorted. Reads as the rule itself.
    return teams
        .Select(team => BuildRow(team, finished))
        .OrderByDescending(row => row.Points)
        .ThenByDescending(row => row.GameDifference)
        .ThenByDescending(row => row.GamesWon)
        .ToList();
}

private static TeamStanding BuildRow(Team team, List<Match> finished)
{
    // Every finished match this team played in, with the score already
    // flipped so "us" and "them" are from THIS team's point of view.
    // Flipping once here is what keeps every line below it simple.
    List<(int Us, int Them)> results = finished
        .Where(m => ReferenceEquals(m.HomeTeam, team)
                 || ReferenceEquals(m.AwayTeam, team))
        .Select(m => ReferenceEquals(m.HomeTeam, team)
            ? (Us: m.Score!.Home, Them: m.Score!.Away)
            : (Us: m.Score!.Away, Them: m.Score!.Home))
        .ToList();

    return new TeamStanding(
        Team:      team,
        Played:    results.Count,
        Won:       results.Count(r => r.Us > r.Them),
        Drawn:     results.Count(r => r.Us == r.Them),
        Lost:      results.Count(r => r.Us < r.Them),
        GamesWon:  results.Sum(r => r.Us),
        GamesLost: results.Sum(r => r.Them));
}
```

**Identical output to version 1.** 61 lines became 33.

### Two bits of syntax not yet covered

**Tuples** — `(int Us, int Them)` is an anonymous pair with named parts. No class
needed, no file. Useful for a short-lived shape that only this method cares about.

**Named arguments** — `Played: results.Count` labels which parameter a value is for.
With seven `int` parameters in a row, `new TeamStanding(team, 3, 1, 1, 1, 4, 3)` is
unreadable and one transposition away from a wrong table. The labels make a swap
visible.

**`!` — and an honest note.** `m.Score!.Home` uses the null-forgiving operator, which
[nullability-and-guards](../concepts/nullability-and-guards.md) says to treat with
suspicion. It is here because the `Where` above already filtered out every match with a
null `Score`, but the compiler cannot follow that reasoning across two chained calls.
**This is a real weakness in the current code** — a `TODO` worth revisiting, not an
endorsement.

---

## 6. What it bought

| | Version 1 | Version 2 |
|---|---|---|
| Lines | 61 | 33 |
| Counters to get wrong | 6 | 0 |
| Sorting | a comparator with a hidden convention | reads as the rule |
| Says | *how* to do it | *what* it is |

**The sort is the biggest win.** Version 1 required knowing that a negative return means
"a comes first", so `b.Points - a.Points` sorts descending and `a.Points - b.Points`
sorts ascending. They look nearly identical, and swapping them inverts the whole table
silently. Version 2 cannot have that bug.

---

## 7. Coming from functional programming

**This is home turf.** LINQ is your standard library with different names, and the
translation table in section 4 is the whole of it.

**The leaks:**

1. **The source is mutable.** `filter` over an immutable list always gives the same
   answer. `Where` over a `List<T>` gives whatever the list holds at the moment you
   enumerate it — and enumeration is deferred, so that may not be when you wrote it.
2. **Laziness costs differently.** Re-enumerating is not free here; it redoes the work
   against live data. `ToList()` is the fence.
3. **`Count` vs `Count()`** — a property and a method with the same name doing the same
   job at different costs.
4. **No pipe operator.** The `.` does the job, which is why chained calls are written
   one per line — it reads as a pipeline that way.

---

## 8. How a senior would think here

**They would write version 1 first too** — or at least would not be embarrassed by it.
The loop version is correct and readable. LINQ is not "better code" in the abstract; it
is better *here* because this is a filter-count-sort problem, which is exactly what LINQ
is shaped for.

**They would reach for `ToList()` deliberately, not by habit.** Sprinkling it everywhere
forces work that laziness would have avoided. The question is always: *is this used more
than once?*

**They would be suspicious of the `!`.** Every null-forgiving operator is a place where
you told the compiler to trust you. Worth a `TODO` and a second look.

**They would notice the table is recalculated, not stored** — and approve. A stored
`Points` field that gets incremented can drift out of sync with the matches. A snapshot
computed from the source of truth cannot.

---

## 9. Checkpoint questions

1. **Q:** What is the difference between `Where` and `Select`?
   **A:** `Where` keeps or drops items — the count can shrink, values are unchanged.
   `Select` transforms each item — the count is the same, values change.

2. **Q:** `numbers.Where(n => n > 5)` — has any filtering happened?
   **A:** No. LINQ is lazy. It built a description of work. Nothing runs until something
   enumerates it.

3. **Q:** Why does `finished` end with `.ToList()`?
   **A:** It is used once per team. Without it the filter re-runs for every team — four
   teams, four passes over all the matches.

4. **Q:** `results.Count` and `results.Count(...)` — why are they different?
   **A:** `Count` is a property that already knows the size. `Count(test)` is a method
   that walks the sequence counting matches.

5. **Q:** In version 1, what exactly made the sort dangerous?
   **A:** It relied on knowing that a negative return means "a sorts first". So
   `b.Points - a.Points` and `a.Points - b.Points` look almost identical but sort in
   opposite directions, and getting it backwards inverts the table with no error.

6. **Q:** Why is `TeamStanding` a record rather than a class?
   **A:** A table row *is* its contents. Two rows with the same numbers are the same
   row. Same identity-vs-value question as Day 3.

---

## 10. Commands I ran

```powershell
dotnet run --project src/Esports.Console
```

---

## 11. What's next

**Working and committed:**
- [TeamStanding.cs](../../src/Esports.Console/TeamStanding.cs) — one row of the table
- [StandingsTable.cs](../../src/Esports.Console/StandingsTable.cs) — builds and sorts it
- LINQ: `Where`, `Select`, `ToList`, `Count`, `Sum`, `OrderByDescending`, `ThenByDescending`

**Next:** the console app has now covered the language. What is missing is everything
around it — a way for something other than a terminal to ask for the table. That is an
HTTP API, and it is where [Esports.Api](../../src/Esports.Api/) finally stops being an
untouched template.

**Loose ends carried forward:**
- The `!` in `BuildRow` is a `TODO`. The `Where` above guarantees `Score` is not null,
  but the compiler cannot see that.
- Single elimination generates round one only. Advancing winners needs results first.
- Deferred from Day 1: what the compiled `.dll` actually contains.
