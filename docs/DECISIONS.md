# Decisions log

Every "why X over Y", one entry each. **This file is the interview prep** — being
able to defend a choice, and name what it cost, is the difference between having
used a thing and having chosen it.

Format: the decision, the alternatives, why, and what it cost.

---

## D1 — Target `net10.0`, not `net9.0`

**Alternatives:** .NET 9 (also installed on this machine).
**Why:** .NET 9 is STS and went out of support in **May 2026**. .NET 10 is LTS,
supported through November 2028, and has been GA ~10 months so search results are
plentiful.
**Cost:** a small amount of older tutorial material targets .NET 8/9 and will need
mental translation.
**Date:** 2026-09-22

## D2 — One repo, `F:\saim` as the git root

**Alternatives:** a nested project folder; separate repos for API and client.
**Why:** one clone, one history, one README. The curriculum is the repo.
**Cost:** the client and server version together, which is wrong for a real
product but right for a learning project.
**Date:** 2026-09-22

## D3 — `Directory.Build.props` for shared MSBuild properties

**Alternatives:** repeat `TargetFramework`/`Nullable`/`ImplicitUsings` in every `.csproj`.
**Why:** MSBuild picks it up automatically for every project at or below the folder.
One place to change, no drift between projects.
**Cost:** a property appearing "from nowhere" if you don't know the file exists —
which is why each `.csproj` carries a comment pointing at it.
**Date:** 2026-09-22

## D4 — Pin the SDK with `global.json`

**Alternatives:** float on whatever SDK is installed.
**Why:** `dotnet new` templates and build behaviour change between SDK versions.
Pinning makes scaffolding reproducible.
**Cost:** must be bumped deliberately when upgrading.
**Date:** 2026-09-22

## D5 — Vite 6 + React 18, not the Vite default

**Alternatives:** the template default (React 19 + Vite 8).
**Why:** React 18 was specified. Vite 8 requires Node `^20.19.0 || >=22.12.0` and
this machine runs **Node 20.18.1**, so its native binding fails to load. Vite 5
works but ships an esbuild dev-server vulnerability (GHSA-67mh-4wv8-2f99).
Vite 6.4.3 fixes it and supports Node 20.
**Cost:** not on the newest Vite. Revisit after a Node upgrade.
**Date:** 2026-09-22

## D6 — Upgrade `Microsoft.AspNetCore.OpenApi` to 10.0.12

**Alternatives:** keep the template's 10.0.0.
**Why:** the template version pulls in `Microsoft.OpenApi 2.0.0`, which has a
**known high-severity vulnerability**. 10.0.12 resolves `Microsoft.OpenApi 2.12.0`
and the build goes to zero warnings.
**Cost:** none.
**Date:** 2026-09-22

## D7 — `Player`/`Team`/`Match` are classes, `MatchScore` is a record

**Alternatives:** make everything a record (immutability feels right), or everything a
class (fewer concepts).
**Why:** they answer the identity-vs-value question differently. Two people called
"Faker" at 1847 are two different people, so `Player` needs identity-based equality.
Any 3-1 is any other 3-1, so `MatchScore` needs content-based equality. Using one
choice for both would be wrong for one of them, and wrong *quietly*.
**Cost:** you must make this call per type, and a mistake fails silently rather than
loudly.
**Date:** 2026-09-22

## D8 — Hand-written `ToString()` on `MatchScore`

**Alternatives:** keep the record's generated one.
**Why:** a record's auto-generated `ToString()` lists **every public property including
computed ones**. Once `IsDraw`, `HomeWon`, `AwayWon`, `Margin` and `GamesPlayed` were
added, a score printed as a seven-field wall instead of `3-1`.
**Cost:** one more line to maintain if the shape changes.
**Date:** 2026-09-22

## D9 — Validate in constructors and factory methods, not at call sites

**Alternatives:** check at each place a value is used.
**Why:** guarding at construction means an invalid object can never exist anywhere in
the program — not even briefly. Three rules now work this way: a team cannot play
itself, a result cannot be recorded twice, a score cannot be negative. Same discipline
as a smart constructor.
**Cost:** exceptions rather than a `Result` type, so failure is not visible in the
signature. C# has no idiomatic `Result`, so this is the convention.
**Date:** 2026-09-22

## D10 — Teaching approach recorded in `CLAUDE.md`

**Alternatives:** leave it implicit, or only in Claude's private memory.
**Why:** the approach was arrived at by correction during Day 1 (too much jargon,
runtime theory before syntax, narrating edit timelines the learner cannot see).
Checking it into the repo means it survives any session and is visible/editable.
**Cost:** none.
**Date:** 2026-09-22

## D11 — `MatchScore` is a non-positional record with a private constructor

**Alternatives:** the original one-liner `public record MatchScore(int Home, int Away);`.
**Why:** a positional record's constructor is **public**, so `new MatchScore(-1, 3)`
bypassed `Create()` entirely and produced a score of `-1-3` with `Margin = 4` and
`GamesPlayed = 2` — nonsense values flowing through every computed property. A guard
only works if it is the **only** way in. The constructor is now private and `Create()`
is the single door, verified: `error CS0122: ... is inaccessible due to its protection
level`.
**Cost:** real. `with` no longer works from outside the type, because that would be a
second route to an invalid value. `WithHome()` / `WithAway()` replace it and re-validate.
The declaration is also ~10 lines instead of 1.
**Found by:** writing the concept file for nullability and guards, which tested the
claim instead of repeating it.
**Date:** 2026-09-23

## D12 — Tournament formats are an interface, not an enum plus `if`

**Alternatives:** a `TournamentFormat` enum with an `if`/`else` inside
`Tournament.GenerateMatches()`.
**Why:** the `if` version makes `Tournament` responsible for knowing every format that
will ever exist, and the branch spreads — formats also differ in standings and
reporting, so "how knockout works" ends up smeared across several files. With the
interface, adding a format is one new class and zero edits to existing code. The test:
`Tournament.cs` does not contain the word `RoundRobin`.
**Cost:** four files instead of one, and dispatch is decided at runtime so the compiler
cannot tell you which implementation will run.
**Date:** 2026-09-23

## D13 — The interface was extracted, not designed up front

**Alternatives:** define `ITournamentFormat` first, then write round robin against it.
**Why:** round robin was written as a plain loop **inside** `Tournament` and only pulled
out once a second format appeared. You cannot see the right shape for an abstraction
with one example, and an interface with one implementation is usually ceremony.
**Cost:** one refactor. Cheap, and it produced a better-shaped contract than guessing
would have.
**Date:** 2026-09-23

## D14 — An abstract base class under the interface, not shared code duplicated

**Alternatives:** leave the `teams.Count < 2` check duplicated in each format; or put
it in `Tournament` before calling the format.
**Why:** duplication was the small problem. The real one is that **nothing forced** a
new format to include the check — a third format could ship without it. Making
`BuildFixtures` abstract means a format cannot exist without going through
`GenerateMatches`, which validates first. Verified: `error CS0534` if a subclass tries
to skip it. Putting the check in `Tournament` was rejected because the rule belongs to
formats, and a format called directly (as the tests do) would bypass it.
**Cost:** spends the single inheritance slot those classes have. A format can implement
many interfaces but inherit from exactly one class.
**Date:** 2026-09-23

## D15 — `BuildFixtures` is `protected`, not `public`

**Alternatives:** `public`, which is the reflex.
**Why:** a public `BuildFixtures` could be called directly, skipping the validation in
`GenerateMatches`. That is exactly the bug `MatchScore` had, where `Create()` validated
and `new` sat open beside it. Verified closed: `error CS0122`.
**Cost:** none.
**Date:** 2026-09-23

## D16 — Keep `ITournamentFormat` even though the abstract class now implements it

**Alternatives:** delete the interface and have `Tournament` depend on
`TournamentFormat` directly.
**Why:** `Tournament` should depend on the narrowest thing that works. A future format
could implement the interface without inheriting the base.
**Cost:** honest — with only two formats, both inheriting the base, the interface is
barely earning its place. It is one file, and it keeps an option open. Deleting can be
decided later with better information; un-deleting is harder.
**Date:** 2026-09-23

---

## Decisions already made in the plan, to be recorded here as they land

- Controllers over Minimal APIs (Day 9)
- No repository pattern — `DbSet<T>` *is* the repository (Day 14)
- Fluent API only, no data annotations on entities (Day 11)
- Hand-written DTO mapping, not AutoMapper (Day 14)
- `xmin` as the concurrency token rather than a manual `RowVersion` (Day 15)
- UUIDv7 for public keys, `bigint` identity for `rating_history` (Day 12)
- `localStorage` for the JWT, with the XSS tradeoff written down (Day 17)
