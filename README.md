# Esports Tournament Engine

A 25-day, build-it-to-learn-it C# / .NET backend curriculum. Teams, players,
tournaments, brackets, matches and Elo ratings — built incrementally, with every
day documented and committed.

**Status:** Day 0 — scaffolding complete. Days 1–25 in progress.

---

## Why this project

The domain was chosen for one reason: tournament formats (Single-Elimination vs
Round-Robin) are a *genuine* reason to reach for inheritance and interfaces. Same
input, different algorithm, one abstraction — not a textbook `Animal`/`Dog`
example. Everything else about the schema is deliberately small (9 tables) so the
concepts get the attention rather than the business rules.

## Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 10 (LTS) / C# 14 |
| API | ASP.NET Core Web API, controllers |
| Data | EF Core + Npgsql → Neon Postgres |
| Client | React 18 + Vite (deliberately thin) |
| Real-time | SignalR (Day 21) |
| Background work | `BackgroundService` + `Channel<T>` (Day 22) |

## Layout

```
EsportsEngine.sln
global.json                 SDK pin, so scaffolding is reproducible
Directory.Build.props       TargetFramework / Nullable / ImplicitUsings in ONE place
docs/                       the curriculum's real output
  days/                     one file per day
  concepts/                 living reference, appended to over time
  INDEX.md GLOSSARY.md CHEATSHEET.md DECISIONS.md
.claude/skills/             Claude Code skills encoding how this project is taught
src/
  Esports.Console/          Day 1.  Console playground for language fundamentals
  Esports.Core/             Day 5.  Domain. References NOTHING — no EF, no ASP.NET
  Esports.Api/              Day 9.  HTTP surface
  Esports.Infrastructure/   Day 10. EF Core, security, file storage
client/                     Day 17. React 18 + Vite
```

**The dependency rule is enforced by the compiler, not by convention.**
`Esports.Core` has no reference to EF Core, so it is physically impossible to use
a `DbContext` in the domain. That is the entire reason these are separate projects
rather than folders.

---

## Running it

### Prerequisites

| Tool | Version used here |
|---|---|
| .NET SDK | 10.0.100 (pinned in `global.json`) |
| Node.js | 20.18.1 — note Vite 6 is pinned for this reason, see `docs/DECISIONS.md` |
| Git | 2.47 |

### API

```powershell
dotnet run --project src/Esports.Api --launch-profile http
# -> http://localhost:5270
```

### Client

```powershell
cd client
npm install
npm run dev
# -> http://localhost:5173
```

### Database

Not wired up yet — Neon Postgres arrives on Day 10. When it does, the connection
string lives in `dotnet user-secrets`, **never** in `appsettings.json`:

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "<pooled connection string>" --project src/Esports.Api
dotnet user-secrets set "ConnectionStrings:Migrations" "<direct connection string>" --project src/Esports.Api
```

The pooled and direct endpoints differ, and migrations need the direct one —
explained on Day 10.

---

## The documentation

The code is the artifact; the docs are the point.

- **[docs/INDEX.md](docs/INDEX.md)** — all 25 days at a glance
- **[docs/CHEATSHEET.md](docs/CHEATSHEET.md)** — C# beside its functional-programming
  equivalent, and **where each analogy breaks**
- **[docs/DECISIONS.md](docs/DECISIONS.md)** — every "why X over Y", with what it cost
- **[docs/GLOSSARY.md](docs/GLOSSARY.md)** — plain-language definitions, no jargon
  defined using other jargon
- **[docs/concepts/](docs/concepts/)** — living reference files, appended to over time

Each day produces a doc following
[the template](docs/days/_TEMPLATE-day.md): the flow, how it works behind the
scenes, why it's needed, where it fits, and the checkpoint questions answered from
memory before looking anything up.
