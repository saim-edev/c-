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
