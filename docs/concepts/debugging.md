# Debugging — bug log and technique

Every real bug hunt gets an entry. Over 25 days this becomes more valuable than
any course, because it is a record of how *I* get things wrong.

Technique lives in the `debug-session` skill. This file is the evidence.

---

## Template

```markdown
## YYYY-MM-DD — <one-line symptom>
**Symptom:** what was observed, phrased the way a user would say it
**Root cause:** the actual defect
**How I found it:** which step of the five-step method got there
**Fix:** what changed
**Lesson:** what would have prevented it
```

---

## 2026-09-22 — `dotnet build` reported a high-severity package vulnerability

**Symptom:** the freshly scaffolded Web API built with 2 warnings, not 0.
**Root cause:** the `webapi` template pins `Microsoft.AspNetCore.OpenApi 10.0.0`,
which transitively pulls `Microsoft.OpenApi 2.0.0` — a version with a published
high-severity advisory (GHSA-v5pm-xwqc-g5wc).
**How I found it:** read the warning instead of ignoring it, then
`grep Microsoft.OpenApi obj/project.assets.json` to see the *resolved* version
rather than the declared one.
**Fix:** upgraded to `Microsoft.AspNetCore.OpenApi 10.0.12`, which resolves
`Microsoft.OpenApi 2.12.0`. Build went to 0 warnings.
**Lesson:** a project template is a starting point, not a vetted artifact. The
declared version in `.csproj` is not the version you get — `project.assets.json`
holds the truth after transitive resolution.

## 2026-09-22 — `npm run build` crashed with "Cannot find native binding"

**Symptom:** Vite build died inside `rolldown` with a missing native module.
**Root cause:** the Vite template installed Vite 8, whose `engines` field requires
Node `^20.19.0 || >=22.12.0`. This machine runs Node 20.18.1, so the native
binding for the platform never resolved. npm had warned `EBADENGINE` and it was
skimmed past.
**How I found it:** `node -p "require('./node_modules/vite/package.json').engines"`
compared against `node --version`.
**Fix:** pinned Vite 6.4.3, which supports Node 20 and also carries the fix for
the esbuild dev-server advisory (GHSA-67mh-4wv8-2f99) that Vite 5 still has.
**Lesson:** `EBADENGINE` warnings are not noise. When a native module "can't be
found", check the engine requirement before deleting `node_modules`.

## 2026-09-23 — `MatchScore.Create()` could be bypassed entirely

**Symptom:** none. Nothing failed, no test went red, no exception was thrown. The
defect was found only because a doc was being written and the claim "an invalid score
cannot exist" got tested rather than repeated.

**Root cause:** `MatchScore` was a positional record:

```csharp
public record MatchScore(int Home, int Away);
```

A positional record's constructor is **public**. `Create()` validated, but `new`
sat right beside it, unguarded:

```
via Create():  refused: Scores cannot be negative: -1-3
via new:       built: -1-3  Margin=4  GamesPlayed=2
```

A margin of 4 and 2 games played, from a score of minus one. Every computed property
happily calculated on impossible input.

**How I found it:** by running the bypass instead of assuming the guard held.

**Fix:** made the constructor `private` so `Create()` is the only way in. Verified the
hole is closed: `error CS0122: 'MatchScore.MatchScore(int, int)' is inaccessible due
to its protection level`.

**Lesson — and it is the big one:** *a guard only works if it is the only way in.*
This code failed the exact principle it was written to demonstrate. `Create()` closed
the front door and left `new` standing open beside it.

Ask of every validation: **what else can construct this?** A public constructor, an
object initializer, `with`, deserialisation from JSON or a database — each is another
door, and each needs closing or the guard is decoration.
