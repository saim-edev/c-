# Concurrency: what happens when many users hit your API at once

**Why this file exists:** a console app runs one thing at a time. A web API runs many.
Every object you have written so far is safe in the first world and dangerous in the
second, and nothing about the code changes — only how many threads are inside it.

---

## 0. Why this feels alien coming from JavaScript

**Node genuinely does not have this problem.** JavaScript runs your code on **one
thread**. Two requests never execute your code at the same instant — they take turns at
`await` points. Shared state cannot be corrupted half way through an operation, because
nothing else is running.

C# gives you **real parallelism**. Many threads, actually simultaneous. That is faster,
and this file is the price.

---

## 1. The problem, demonstrated

```csharp
// One shared list. Exactly like Tournament._matches or Team._players.
List<int> shared = new List<int>();

// 100 "requests" arriving at once, each adding 100 items.
// Expected total: 10,000
Parallel.For(0, 100, i =>
{
    for (int j = 0; j < 100; j++)
    {
        shared.Add(j);
    }
});

Console.WriteLine($"actual : {shared.Count}");
```

It never printed anything. It crashed:

```
System.ArgumentException: Source array was not long enough.
   at System.Array.Copy(...)
   at System.Collections.Generic.List`1.set_Capacity(Int32 value)
   at System.Collections.Generic.List`1.AddWithResize(T item)
```

### Why — this is [collections](collections.md) coming back

A `List<T>` is a **growable array**. When it fills, `Add` does three steps:

```
  1. allocate a new array, double the size
  2. copy everything across
  3. swap the old array for the new one
```

Two threads hit that at once:

```
  Thread A            Thread B
  ────────            ────────
  Add() -> full
  allocate new[8]
  copying...          Add() -> full
  copying...          allocate new[8]
  copying...          copy + swap    <-- A's target is now stale
  CRASH
```

**Sometimes it crashes. Sometimes it silently loses items** and you get 9,847 instead of
10,000. The silent version is worse.

---

## 2. The machine: cores, threads, and the pool

Real numbers from this machine:

```
hardware threads the OS sees : 12
thread pool starts with      : 12 worker threads
thread pool may grow to      : 32767 worker threads
```

6 cores, each juggling 2 instruction streams, so the OS sees **12**.

```
   ┌─────────────────── CPU ───────────────────┐
   │  core1   core2   core3   core4  core5  core6 │
   │  [T][T]  [T][T]  [T][T]  [T][T] [T][T] [T][T]│   12 slots
   └───────────────────────────────────────────┘
```

**Exactly 12 things execute at the same instant. Not 13.** More requests than that and
the OS swaps between them every few milliseconds, which is why it *feels* simultaneous.

.NET keeps a **pool of ready-made threads** because creating one is expensive — roughly
1 MB of address space and a trip into the OS. Reusing 12 threads for thousands of
requests is far cheaper than a thread per request.

**One thread handles one request at a time**, start to finish. Threads are reused across
requests, never shared within one.

```
  Thread 1:  [req A]  [req D]  [req F] ...     one after another
  Thread 2:  [req B]     [req E]     ...
  Thread 3:     [req C]        [req G] ...
             ───────────────────────────► time
                  3 requests in flight at once
```

---

## 3. Worked example: 3 users, 8 requests each

24 requests land at once. The dashboard endpoint does:

```
   1 ms   work out what was asked for
  40 ms   ask the database, WAIT for the answer
   1 ms   turn the answer into JSON
```

**40 of those 42 ms are spent doing nothing** — waiting for another computer.

### Blocking — the naive way

```
   T1  [1ms]████████████████████████████████████[1ms]   42ms
            ^                                  ^
            └────── thread is STUCK here ──────┘
                    not running, not reusable

   T1..T12 all stuck the same way.
   Requests 13-24 wait for a free thread.

   |<------- 42ms ------->|<------- 42ms ------->|
    requests 1-12           requests 13-24
                                                 ~84ms
```

Twelve threads frozen doing nothing, twelve requests queued behind them, **CPU nearly
idle the whole time**.

### Async

`await` on the database call makes the thread **let go and return to the pool**.

```
   T1  [1ms]  ....................................  [1ms]
              ^                                  ^
              thread LEFT here                   a thread (maybe a
              and served other requests          different one) resumes

   All 24 requests in flight at once.
   Only 24ms of real CPU work, spread over 12 threads.

   |<-- 2ms -->|<------- 40ms waiting ------->|<-- 2ms -->|
                                                          ~44ms
```

**Same hardware. 84ms becomes 44ms.** Nothing got faster — the database still takes
40ms. **Waiting stopped consuming a thread.**

At scale the difference stops being 2x:

```
   500 requests, blocking : needs ~500 threads. The pool adds new ones at
                            roughly 1-2 per second. Requests time out.
   500 requests, async    : still 12 threads. Fine.
```

---

## 4. RAM: two separate pools

```
  ┌────────────────────────────────────────────────────────┐
  │  PER THREAD — the stack                                 │
  │  ~1 MB of address space reserved per thread.            │
  │  Holds: which method you are in, locals, return address.│
  │                                                         │
  │  24 threads = 24 MB RESERVED, but barely any of it      │
  │  actually used. A typical thread touches a few KB.      │
  └────────────────────────────────────────────────────────┘

  ┌────────────────────────────────────────────────────────┐
  │  SHARED — the heap                                      │
  │  Every object. All threads can reach it.                │
  │  Your Tournament, Team, Match, the rows, the JSON.      │
  │  Mostly short-lived per-request garbage.                │
  └────────────────────────────────────────────────────────┘
```

**The honest version:** blocked threads are **not mainly a RAM problem**. That 1 MB is
reserved address space, not committed memory. The real cost of blocking is **running out
of threads**, which shows up as requests queueing and timing out while the CPU sits
at 5%.

---

## 5. Are my objects shared between users?

**No.** Not between users, and **not even between two requests from the same user**.

```
  user A, request 1  ──►  Team obj  ──► response ──► GONE
  user A, request 2  ──►  Team obj  ──► response ──► GONE   (a different object)
  user B, request 1  ──►  Team obj  ──► response ──► GONE   (another one)
```

Each request builds its own objects, uses them, throws them away. The server remembers
nothing between requests.

### Then what IS shared? The database.

```
                    ┌──────────────────┐
   user A  ────────►│                  │
   user B  ────────►│    DATABASE      │   <-- the ONLY shared thing
   user C  ────────►│  teams, matches  │
                    └──────────────────┘
                             │
        each request reads it and builds ITS OWN objects
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
          Team obj       Team obj       Team obj
          (A's copy)     (B's copy)     (C's copy)
```

One request's whole life:

```
  1. request arrives
  2. read rows from the database
  3. turn those rows into Team / Match objects   <-- fresh, yours alone
  4. do the work (build the standings table)
  5. turn the answer into JSON
  6. send it
  7. every object from step 3 becomes garbage
```

Those objects live about **42 milliseconds**.

### "Isn't making new objects every time expensive?"

Measured:

```
own instance   : 1000 requests, 0 got a wrong answer
shared         : CRASHED, and the list has 4713 items in it
cost of 1000   : 0.04 ms, 73544 bytes total
```

**1000 objects: 0.04 ms and 73 KB.** Then gone.

[memory-and-references](memory-and-references.md) explains why: allocating is a
**pointer bump**, and short-lived objects are the cheapest kind to collect. The garbage
collector is built on the assumption that most objects die young — which is exactly what
per-request objects do.

**So the instinct "creating things is expensive, share them" is backwards.** Sharing is
the expensive choice: you pay in locks, bugs, and 3am debugging.

---

## 6. NestJS, for comparison

A question that came up, and the answer has a trap in it.

In NestJS, an `@Injectable()` service is a **singleton by default** — **one instance
shared by every user and every request**, for the whole life of the app. Not per-user.

```
   user A ──┐
   user B ──┼──►  ONE TournamentService instance   <-- SHARED by everyone
   user C ──┘
```

It does not usually explode because a typical service **holds no data**:

```ts
@Injectable()
export class TournamentService {
  // no fields holding state
  async getStandings(id: string) {
    const rows = await this.db.query(...)   // fresh every call
    return buildTable(rows)                  // fresh objects, local
  }
}
```

The service object is shared. The data it creates inside each call is **local to that
call**.

It breaks the moment you put data on it:

```ts
@Injectable()
export class TournamentService {
  private currentTeams = [];    // <-- DANGER. Shared by all users.
}
```

C# has the same three choices, arriving on Day 10:

| | Meaning |
|---|---|
| **Singleton** | one instance, whole app, all users — same as NestJS default |
| **Scoped** | one instance per request |
| **Transient** | a new one every time it is asked for |

**Same rule in both languages: a shared object with no data in it is fine. A shared
object holding data is a bug waiting for two users.**

---

## 7. How to prevent it

> **A shared object may hold other objects. It must not hold data that changes.**

```csharp
// SAFE to share - holds no changing data
class TournamentService
{
    private readonly IDatabase _db;      // set once, never changes

    public Table GetStandings(int id)
    {
        var rows = _db.Load(id);         // local - each caller gets their own
        return BuildTable(rows);         // local
    }
}

// NOT SAFE to share - holds changing data
class TournamentService
{
    private List<Team> _teams = new();   // <-- every user writes into this
    private int _requestCount = 0;       // <-- so does this
}
```

**Locals are always safe.** Every call gets its own copy of every local. Two threads in
the same method never see each other's locals.

### The test

```
   Does this field ever change after construction?
            │
      ┌─────┴─────┐
     NO           YES
      │             │
      ▼             ▼
   fine        do NOT put it here.
               Make it a local, a parameter,
               or a return value.
```

`readonly` on a field says "set once, never again", and the compiler enforces it.

---

## 8. When you genuinely want sharing

| | Why |
|---|---|
| **Config / settings** | read-only after startup, everyone needs it |
| **A cache** | sharing is the entire point |
| **A connection pool** | opening a DB connection is expensive; reuse them |
| **Counters / metrics** | "requests served" is genuinely global |

The first is free. The other three are shared **mutable** state and must be protected.

### `lock`

```csharp
int unprotected = 0;
int protectedCount = 0;
object gate = new object();     // the thing threads queue on

Parallel.For(0, 100_000, i =>
{
    unprotected++;                        // no protection

    lock (gate)                           // one thread at a time in here
    {
        protectedCount++;
    }
});
```

```
expected     : 100000
unprotected  : 99365
with lock    : 100000
```

**635 increments vanished.** No crash, no error — a wrong number.

`count++` is not one step. It is three:

```
   Thread A              Thread B
   ────────              ────────
   read count  (41)
                         read count  (41)      <-- same value!
   add 1       (42)
                         add 1       (42)
   write       (42)
                         write       (42)      <-- one increment lost
```

Two increments happened. The number rose by one.

**`lock` is not free.** Threads wait. Lock a block that takes 40 ms and you have
serialised the whole API — 12 threads, 11 standing in line.

### Order of preference

```
  1. Don't share it                        <-- 95% of cases. Make it local.
  2. Share something that never changes    <-- free, safe
  3. Share changing data, with a lock      <-- works, costs waiting
  4. Share changing data, no lock          <-- silently wrong numbers
```

**Always try 1 first.** Sharing is not the efficient choice. It is the choice you make
when there is no alternative.

---

## 9. `await` — how a thread lets go

The claim in section 3 was that at `await` the thread walks away. That sounds
impossible. Proof:

```csharp
Console.WriteLine($"before await : thread #{Environment.CurrentManagedThreadId}");

// Task.Delay stands in for "ask the database and wait 40ms".
await Task.Delay(40);

Console.WriteLine($"after await  : thread #{Environment.CurrentManagedThreadId}");
```

```
before await : thread #2
after await  : thread #5
```

**Different thread.** Same method, two lines apart.

### What the compiler does

It does not pause the method. It **chops it in half**:

```
   what you write                  what the compiler builds

   ┌─────────────────────┐         ┌─────────────────────┐
   │ line 1              │         │ PART 1: line 1      │
   │ await something     │   ──►   │   start the wait    │
   │ line 3              │         │   RETURN            │  <-- thread free
   └─────────────────────┘         ├─────────────────────┤
                                   │ PART 2: line 3      │  <-- run later
                                   └─────────────────────┘
```

At the `await`, part 1 **returns**. The thread is genuinely finished and free.

Part 2 is registered as "run this when the wait completes". When it does, part 2 is
scheduled onto whatever thread is available.

Locals survive because the compiler moved them off the stack into an object on the heap.
That is how thread #5 continues with values thread #2 created.

### What a `Task` is

```
   ┌──────────────────────────┐
   │  Task                    │
   │    done yet?   no        │
   │    result      (empty)   │
   │    when done, run:  ───────►  part 2 of your method
   └──────────────────────────┘
```

**A `Task` is not a thread.** Nobody runs anything during those 40 ms. There is an
object, and a note saying what to run when the answer arrives.

| JavaScript | C# |
|---|---|
| `Promise` | `Task` |
| `const x = await fetch(...)` | `var x = await http.GetAsync(...)` |

Near-identical syntax, same idea. The difference underneath: **JS resumes on the one
thread it always had. C# resumes on whichever thread is free** — which is why the number
changed.

---

## 10. The two rules

**1. Never block a thread waiting for something.** Database, file, another API — always
`await`. A blocked thread is a thread nobody else can use.

**2. Objects created per request are cheap and safe. Objects shared between requests are
neither.**

---

## 11. The whole flow, one picture

```
  user clicks "dashboard"
        │
        ▼
  socket opens, bytes arrive                          ── OS
        │
        ▼
  Kestrel reads them, builds a request object          ── your app
        │
        ▼
  a thread is taken from the pool  ─────────┐          12 available
        │                                   │
        ▼                                   │
  YOUR CODE RUNS (1ms)                      │
        │                                   │
        ▼                                   │
  await database ──────────────────────────►│  thread GOES BACK
        │                                   │  and serves someone else
        │  (40ms, no thread held)           │
        ▼                                   │
  answer arrives ◄──────────────────────────┘
        │
        ▼
  a thread picks it back up, builds JSON (1ms)
        │
        ▼
  bytes written to the socket, request object thrown away
        │
        ▼
  objects it made become garbage; thread returns to pool
```

---

## 12. What this means for the code already written

`Tournament`, `Team`, `Match`, `StandingsTable` — all hold mutable `List<T>` fields.
**Perfectly safe today** (one thread) and **unsafe the moment two requests share one
object**.

Nothing about them needs changing, because they fall under rule 1: each request builds
its own from the database. No locks anywhere.

The hard problem does not disappear, it moves: **two users editing the same database row
at the same time** is still real, and gets solved with database tools rather than C#
locks. That arrives around Day 11.

---

*Introduced: Day 8, before building the API. Related:
[memory-and-references](memory-and-references.md), [collections](collections.md),
[thinking-like-a-backend-dev](thinking-like-a-backend-dev.md),
[THE-BIG-PICTURE](../THE-BIG-PICTURE.md).*
