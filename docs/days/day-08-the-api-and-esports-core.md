# Day 8 — The API, and splitting the code in two

> **Goal:** Serve the league table over HTTP, using the exact same classes the console app uses.
> **Date:** 2026-10-01 · **Tag:** `day-08`

---

## 0. Where we were

[Day 7](day-07-standings-and-linq.md) finished the league table. The console app printed
it and exited.

```
  #  Team   P   W   D   L   Games    GD  Pts
  ------------------------------------------
  1  DK     3   3   0   0    5-0      5    9
  2  HLE    3   1   1   1    4-3      1    4
  3  T1     3   1   0   2    2-3     -1    3
  4  GEN    3   0   1   2    2-7     -5    1
```

**What was missing:** only one person can see that — whoever is standing at the
terminal. `Esports.Api` had been sitting untouched since Day 0, still holding the
sample files `dotnet new` created.

*Previous: [Day 7 — A league table, and LINQ](day-07-standings-and-linq.md)*

---

## 1. What we built today

- Deleted the template scaffolding from [Esports.Api](../../src/Esports.Api/).
- **A new project, `Esports.Core`**, holding the domain — because two programs now
  need the same classes.
- Three endpoints: `/health`, `/standings`, `/matches/{format}`.
- Namespaces, which had been missing all along.

---

## 2. A console `Program.cs` and an API `Program.cs` are different shapes

Same filename. Opposite behaviour.

```csharp
// CONSOLE - runs top to bottom, then the program ends
Team t1 = MakeTeam(...);
league.GenerateMatches();
Console.WriteLine(...);
// <- reaches the bottom, exits
```

```csharp
// API - sets things up, then waits forever
var builder = WebApplication.CreateBuilder(args);
...
app.Run();     // <- never reaches the bottom
```

### The file has exactly two halves

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();        ─┐
builder.Services.AddOpenApi();             │  HALF 1: setting up.
                                           │  Nothing is running yet.
var app = builder.Build();                ─┘  <-- the dividing line

app.UseHttpsRedirection();                ─┐
app.UseAuthorization();                    │  HALF 2: the pipeline.
app.MapControllers();                      │  What happens to each request.
                                          ─┘
app.Run();                                    <-- start listening. Blocks forever.
```

**`builder.Build()` is the line that splits it.**

- **Before it** — `builder.Services.Add...` writes a *shopping list* of things the app
  will need. Nothing is created.
- **After it** — `app.Use...` describes what happens to every arriving request.
- **`app.Run()`** opens the port and waits. That line never finishes while the app lives.

### Proof it waits

```
Now listening on: http://localhost:5270
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
```

```
GET /weatherforecast  -> HTTP 200
GET /weatherforecast  -> HTTP 200  (asked again)
```

**"Press Ctrl+C to shut down."** The console app never says that, because it ends on its
own. This one will not. And it answered twice — still alive between the calls.

```
  CONSOLE                          API
  ───────                          ───
  start                            start
  do the work                      set up
  print                            LISTEN ────┐
  exit                                        │  answer
                                              │  answer
                                              │  ...forever
```

`Now listening on: http://localhost:5270` means your program asked the OS for port 5270
and got it. The web server (**Kestrel**) lives *inside* your app — there is no separate
server program to install. **Your app is the web server.**

---

## 3. The first endpoint

```csharp
app.MapGet("/health", () => new { status = "ok" });
```

```
{"status":"ok"}
HTTP 200
```

And the deleted sample endpoint:

```
GET /weatherforecast -> HTTP 404
```

404 confirms the template code is genuinely gone, not just hidden.

### What that one line did

```
  C#                                     over the wire
  ────────────────────────────           ─────────────
  new { status = "ok" }         ──►      {"status":"ok"}
```

**Nobody wrote a `{` or a `}`.** You return objects; the framework turns them into JSON.
Same in reverse on the way in.

`new { status = "ok" }` with no type name is an **anonymous object** — a shape used once,
no class needed. Like a JS object literal.

`MapGet` **registers** the endpoint; it runs nothing. Then `app.Run()` opens the port,
and every arriving request is matched against the registered list. No match → 404.

### Why a minimal API and not a controller

A controller would have meant four new pieces of syntax at once — the class,
`[ApiController]`, `[Route]`, `[HttpGet]`, `ControllerBase` — to say the same thing.
Controllers arrive when there is enough to organise. Same **no structure before the
pain** rule that keeps the console project folder-free.

---

## 4. The problem that forced a third project

To return the standings, the API needs `Tournament`, `StandingsTable`, `Team`, `Match`.
They lived inside the **console project**.

```
  src/Esports.Console/        src/Esports.Api/
    Team.cs                     Program.cs
    Match.cs                    (needs Team, Match, Tournament...)
    Tournament.cs
    StandingsTable.cs           ...but they live over there
```

### Three ways out, and only one is right

| | |
|---|---|
| **Copy the files into the API** | two copies of `Team.cs`. Fix a bug in one, forget the other. No. |
| **Reference the console project** | it is a *program* with a `Main`, meant to be run. Referencing an app from another app is backwards. |
| **A class library both can use** | this. |

### What a class library is

The only difference is a missing line in the `.csproj`:

```xml
<!-- Esports.Console.csproj -->
<OutputType>Exe</OutputType>      <-- "make something runnable"

<!-- Esports.Core.csproj -->
                                   <-- nothing. Just a .dll.
```

No `Main`, no entry point. You cannot `dotnet run` it. It exists only to be referenced.

Proof — what lands in each output folder after a build:

```
  src/Esports.Console/bin/Debug/net10.0/
      Esports.Console.dll
      Esports.Console.exe     <- a launcher
      Esports.Core.dll        <- a COPY of Core

  src/Esports.Api/bin/Debug/net10.0/
      Esports.Api.dll
      Esports.Api.exe         <- a launcher
      Esports.Core.dll        <- another COPY of Core

  src/Esports.Core/bin/Debug/net10.0/
      Esports.Core.dll        <- no .exe. Nothing to launch.
```

### The arrows point inward, and that is the point

```
   Esports.Console  ──────►  Esports.Core  ◄──────  Esports.Api
   (runs it in a              the rules           (serves it
    terminal)                 of the game          over HTTP)
```

`Esports.Core` does not know either caller exists. So the same rules can be run from a
terminal, served over HTTP, or called from a test, **with nothing in Core changing**.

Something stronger too: when a database library lands in `Esports.Api` later, **Core
physically cannot use it** — the reference is not there. Same instinct as `private set`
on Day 1, scaled to whole projects: *shrink the number of places a rule can be broken*.
A folder cannot enforce that. A missing project reference can.

**Core never starts.** It is loaded into whichever program needed it and sits there until
something calls one of its methods. Same relationship as your code and `List<T>` — you
do not "run" `List<T>`, you use it from a program you did run.

### Twelve files moved, zero lines changed

```
src/{Esports.Console => Esports.Core}/Match.cs           |  0
src/{Esports.Console => Esports.Core}/Team.cs            |  0
src/{Esports.Console => Esports.Core}/Player.cs          |  0
```

The `0` is the line count. Moved with `git mv`, so history followed them —
`git log src/Esports.Core/Match.cs` still shows Day 3.

Console output afterwards: byte-identical.

### Why now and not on Day 1

There was nothing to share. One project, one program. The library appeared the moment a
**second** program needed the same code.

**Third time the same rule has applied** — the interface on Day 5, the abstract class on
Day 6, the library today: *write the concrete thing, extract when a second case shows up.*

---

## 5. The standings endpoint

```csharp
app.MapGet("/standings", () =>
{
    Tournament league = BuildTournament(new RoundRobinFormat());
    List<TeamStanding> table = StandingsTable.Build(league.Teams, league.Matches);

    return table.Select((row, index) => new
    {
        position = index + 1,
        team = row.Team.Tag,
        played = row.Played,
        won = row.Won,
        points = row.Points
        // ...
    });
});
```

```json
{"position": 1, "team": "DK",  "played": 3, "won": 3, "points": 9}
{"position": 2, "team": "HLE", "played": 3, "won": 1, "points": 4}
{"position": 3, "team": "T1",  "played": 3, "won": 1, "points": 3}
{"position": 4, "team": "GEN", "played": 3, "won": 0, "points": 1}
```

**Same table, same order, same numbers as the console app.**

```
  CONSOLE                              API
  1  DK   3  3  0  0   9               {"position":1,"team":"DK",...,"points":9}
  2  HLE  3  1  1  1   4               {"position":2,"team":"HLE",...,"points":4}

       └──────────────┬──────────────┘
                      │
               Esports.Core
          ONE set of code, called twice
```

### Why the answer is reshaped rather than returned directly

`TeamStanding` holds a whole `Team` → which holds its `List<Player>` → which hold
ratings. Returning it would push that entire tree down the wire when a table needs none
of it.

Worse, it would **couple callers to our internals**. Rename a property in `Team` and
every client breaks silently. **The shape sent over the wire should be chosen
deliberately, not be whatever the domain happens to look like today.**

That has a name — a **DTO** — which arrives properly on Day 14. For now it is `Select`.

### And the concurrency rule, applied for real

Everything inside the endpoint is built **fresh, per request**. Three users at once get
three separate `Tournament` objects. Nothing shared, nothing another request can corrupt.
See [concurrency-and-threads](../concepts/concurrency-and-threads.md).

---

## 6. Route parameters — getting input from the URL

```csharp
app.MapGet("/matches/{format}", (string format) => ...)
//                     ^^^^^^^^   ^^^^^^^^^^^^^
//                     in the URL  arrives as an argument
```

One endpoint, many answers:

```
GET /matches/league     ->  format == "league"
GET /matches/knockout   ->  format == "knockout"
```

```json
// /matches/league  - everyone plays everyone
{"home": "T1",  "away": "GEN", "state": "Completed", "score": "2-0", "winner": "T1"}
{"home": "T1",  "away": "HLE", "state": "Completed", "score": "0-2", "winner": "HLE"}
{"home": "GEN", "away": "HLE", "state": "Completed", "score": "2-2", "winner": null}
// ...6 total

// /matches/knockout - seeded pairs
{"home": "T1",  "away": "DK",  "state": "Completed", "score": "2-0", "winner": "T1"}
{"home": "GEN", "away": "HLE", "state": "Completed", "score": "0-2", "winner": "HLE"}
```

**The Day 5 interface, now driven from outside.** The caller picks the format; the code
that handles it never names a concrete format class.

Note `"winner": null` on the 2-2 draw — the nullable `Team?` from Day 3, showing up
correctly in JSON.

### 400 vs 404 — a real distinction

```
GET /matches/banana    ->  HTTP 400
{"error":"Unknown format 'banana'","allowed":["league","knockout"]}

GET /nonsense          ->  HTTP 404
```

| | Means |
|---|---|
| **404** | no such endpoint — the URL matched nothing |
| **400** | right endpoint, **wrong input** |

`/matches/banana` *did* match an endpoint. The code ran and judged the input bad. The
body says what is allowed — same instinct as putting real values in exception messages.

---

## 7. Namespaces — the thing that had been missing

A question that came up: *how does the API use `StandingsTable` with no import?*

The answer: **none of the Core files declared a namespace.** They were all in the global
namespace, visible to everything, which is why no `using` was needed. It worked, and it
was wrong.

### How C# importing actually works — two steps, not one

```
  JAVASCRIPT                          C#
  ──────────                          ──
  one step:                           two steps:

  import { StandingsTable }           1. Esports.Api.csproj
    from './core/StandingsTable'         <ProjectReference
         ^^^^^^^^^^^^^^^^^^^^^^             Include="..\Esports.Core\..." />
         a FILE PATH                       "I may use code from there"

                                      2. Program.cs
                                         using Esports.Core;
                                               ^^^^^^^^^^^^
                                               a NAMESPACE, not a file
```

**C# has no way to import a file.** `using Esports.Core;` means *"when I write `Team`,
also look inside the group labelled `Esports.Core`"*. Rename `StandingsTable.cs` to
`banana.cs` and nothing breaks.

### What a `.csproj` is

It is `package.json`.

```
  package.json                    Esports.Api.csproj
  ────────────                    ──────────────────
  "dependencies": {               <PackageReference Include=
    "express": "^4.0"               "Microsoft.AspNetCore.OpenApi" />
  }                                 ^^^^^ from the internet (NuGet)

  (no equivalent)                 <ProjectReference Include=
                                    "..\Esports.Core\Esports.Core.csproj" />
                                    ^^^^^ another project in this repo
```

Written **once per project**, not per file.

### Why no namespace is a problem

1. **Name collisions.** Add a NuGet package containing a class called `Match` or
   `Player` — plausible names — and the compiler cannot tell which you mean.
2. **Nothing says where a class came from.** The `using` lines at the top of a file are
   a map of its dependencies.
3. **Every C# codebase you will ever see uses them.** A project without them looks
   broken to anyone else.

Fixed: `namespace Esports.Core;` added to all 12 Core files, `using Esports.Core;` to
both callers. Proof it is load-bearing — deleting that one line:

```
error CS0103: The name 'StandingsTable' does not exist in the current context
error CS0246: The type or namespace name 'ITournamentFormat' could not be found
              (are you missing a using directive or an assembly reference?)
error CS0246: The type or namespace name 'Match' could not be found
```

Note the wording: *"a using directive **or an assembly reference**"* — C# telling you
there are two separate steps and either could be the one you missed.

### How to find where something came from

The information JS puts in the import path, C# puts in the editor:

| You want | JavaScript | C# |
|---|---|---|
| Jump to the definition | Ctrl+click | **`F12`** |
| Peek without leaving | Alt+F12 | **`Alt+F12`** |
| Find everything using it | Shift+F12 | **`Shift+F12`** |
| Search all types by name | Ctrl+P | **`Ctrl+T`** |
| **Auto-add the import** | auto-import | **`Ctrl + .`** |

**`Ctrl + .` is the one to memorise.** Type a class name, it is underlined red, press
`Ctrl+.`, pick the `using`. Exactly the JS auto-import flow.

### The actual steps to use code from another project

```
  ONCE PER PROJECT
  ────────────────
  1. dotnet add src/Esports.Api reference src/Esports.Core
     ...which writes into the .csproj:
     <ProjectReference Include="..\Esports.Core\Esports.Core.csproj" />

  ONCE PER FILE
  ─────────────
  2. Start typing the class name
  3. Ctrl + .  ->  "using Esports.Core;"  ->  added for you
```

### Why C# chose this

**You can move files freely.** Rename a file, move it to a subfolder, split it in two —
nothing else changes. In JS every import holding that path breaks.

The cost is the one you noticed: reading the source alone, you cannot see where
something came from.

---

## 8. Where it all sits now

```
                     Esports.Core
                 (Tournament, Match, StandingsTable...)
                    /                      \
                   /                        \
          Esports.Console              Esports.Api
          prints the table             GET /health
          to a terminal                GET /standings
                                       GET /matches/{format}
```

```
Esports.Core       12 files    the rules of the game
Esports.Console     1 file     runs them in a terminal
Esports.Api         1 file     serves them over HTTP
```

See [THE-BIG-PICTURE](../THE-BIG-PICTURE.md).

---

## 9. New syntax met today

| Syntax | Means |
|---|---|
| `WebApplication.CreateBuilder(args)` | start configuring a web app |
| `builder.Services.Add...` | add to the shopping list. Creates nothing. |
| `builder.Build()` | the dividing line — turn the list into a real app |
| `app.Use...` | a step every request passes through |
| `app.MapGet("/path", () => ...)` | register an endpoint |
| `app.Run()` | open the port and wait. Never finishes. |
| `new { status = "ok" }` | an anonymous object — a shape with no class |
| `"/matches/{format}"` + `(string format)` | a route parameter, pulled out of the URL |
| `Results.Ok(...)` / `Results.BadRequest(...)` | choose the HTTP status deliberately |
| `namespace Esports.Core;` | a label on a group of classes |
| `using Esports.Core;` | "also look in that group for names I write" |
| `<ProjectReference Include="..." />` | "I may use code from that project" |

---

## 10. Traps and gotchas

- **A reference is not an import.** Two separate steps: the project reference in the
  `.csproj` (once per project) and the `using` in each file.

- **No namespace means the global namespace.** It works until a package you install has
  a class with the same name.

- **`<OutputType>Exe</OutputType>` is what makes a project runnable.** Its absence is
  the whole difference between a library and a program.

- **`dotnet new classlib --nologo` fails.** `--nologo` is not a valid flag for
  `dotnet new` and makes it bail with exit code 127 and a confusing message. Drop it.

- **404 and 400 are different failures.** 404 = no such endpoint. 400 = right endpoint,
  bad input. Returning 404 for bad input hides the real problem from the caller.

- **Committing immediately empties VS Code's Source Control panel**, so changes appear
  to vanish. Pending work and history are two different views.

---

## 11. How a senior would think here

**They would read `Program.cs` before touching anything.** It is the most
information-dense file in a .NET web app — every feature and every pipeline step is
registered there, in order. That is why it is step one when opening an unfamiliar repo.
See [thinking-like-a-backend-dev](../concepts/thinking-like-a-backend-dev.md).

**They would not have created `Esports.Core` on Day 1.** With one program there is
nothing to share, and the right shape for a shared library is invisible until a second
caller exists.

**They would use `git mv`, not delete-and-recreate.** History following a file is worth
the extra care — `git log` and `git blame` stay useful.

**They would reshape the response rather than return the entity**, without thinking
about it. The wire format is a contract with the outside world; your classes are not.

**They would notice `BuildTournament()` is fake and leave a comment saying so**, rather
than let it quietly look like real code.

---

## 12. Checkpoint questions

1. **Q:** What does `builder.Build()` separate?
   **A:** Before it, `Services.Add...` registers what the app will need and creates
   nothing. After it, `app.Use...` describes what happens to each request.

2. **Q:** Why does `app.Run()` never finish?
   **A:** It opens the port and waits for requests. A console app reaches the bottom and
   exits; this one does not.

3. **Q:** Why could the API not just reference the console project?
   **A:** The console project is a program with an entry point, meant to be run.
   Referencing an app from another app is backwards. A class library has no `Main` and
   exists only to be used.

4. **Q:** Which way do the project references point, and why does that matter?
   **A:** Inward. Both `Console` and `Api` point at `Core`; `Core` points at nothing. So
   `Core` cannot use anything from them — including, later, a database library.

5. **Q:** `using Esports.Core;` — is that importing a file?
   **A:** No. C# cannot import files. It names a *namespace*, a label on a group of
   classes. Which file they live in is irrelevant to the compiler.

6. **Q:** What are the two steps to use a class from another project?
   **A:** A `ProjectReference` in the `.csproj` (once per project), and a `using` for
   the namespace (once per file).

7. **Q:** `/matches/banana` returns 400, `/nonsense` returns 404. Why different?
   **A:** `/matches/banana` matched an endpoint, which then judged the input invalid —
   that is 400. `/nonsense` matched no endpoint at all — that is 404.

---

## 13. Commands I ran

```powershell
dotnet new classlib -o src/Esports.Core
dotnet sln add src/Esports.Core/Esports.Core.csproj
dotnet add src/Esports.Console reference src/Esports.Core
dotnet add src/Esports.Api reference src/Esports.Core

dotnet build
dotnet run --project src/Esports.Api --launch-profile http
```

---

## 14. What's next

**Working and uncommitted:**
- Three endpoints: `/health`, `/standings`, `/matches/{format}`
- `Esports.Core` holding the domain, referenced by both programs
- Namespaces throughout

**Next, and why it follows:** roughly half of
[Esports.Api/Program.cs](../../src/Esports.Api/Program.cs) is `BuildTournament()` —
**fake data, rebuilt from scratch on every request.** Which means you cannot add a team,
cannot record a real result, and restarting loses nothing because nothing was ever
there.

That is not a missing feature. **It is the reason databases exist.**

```
   TODAY                        WITH A DATABASE
   ─────                        ───────────────
   request -> build fake data   request -> read REAL data
           -> return it                 -> return it
           -> throw it away

   restart?                     restart?
   same fake data               your data is still there
```

Three things arrive together: **Neon** (Postgres in the cloud), **EF Core** (turns
objects into rows and back), and **migrations** (turns C# classes into real tables).

**Loose ends carried forward:**
- The `!` in `StandingsTable.BuildRow` is still a `TODO`.
- Single elimination generates round one only.
- `Program.cs` is 208 lines. Controllers would tidy it, but most of the bulk is the fake
  data, which disappears with the database.
