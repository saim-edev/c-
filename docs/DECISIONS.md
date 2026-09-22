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

---

## Decisions already made in the plan, to be recorded here as they land

- Controllers over Minimal APIs (Day 9)
- No repository pattern — `DbSet<T>` *is* the repository (Day 14)
- Fluent API only, no data annotations on entities (Day 11)
- Hand-written DTO mapping, not AutoMapper (Day 14)
- `xmin` as the concurrency token rather than a manual `RowVersion` (Day 15)
- UUIDv7 for public keys, `bigint` identity for `rating_history` (Day 12)
- `localStorage` for the JWT, with the XSS tradeoff written down (Day 17)
