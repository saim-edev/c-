# Day 0 — Setup: repo, SDK pin, and the two scaffolds

> **Goal:** Have a git repo pushed to GitHub containing a building .NET solution and a running React app, with every piece of the setup understood rather than copy-pasted.
> **Date:** 2026-09-22 · **Tag:** `day-00` · **Time spent:** ~2h

---

## 1. What we built today

An empty folder became a working repository. There is now a solution with a console
project and a Web API project (both building on .NET 10), a React 18 client (building
on Vite 6), a documentation system, five Claude Code skills that encode how this
project is taught, and a first commit pushed to GitHub. Nothing does anything useful
yet — that starts on Day 1. Today was about the ground being solid.

---

## 2. The flow

What happened, in order, and **why the order matters**:

1. **`.gitignore` first.** Created *before* any `dotnet new` or `git init`. If you
   scaffold first, `bin/`, `obj/` and later `node_modules/` are already sitting there
   when you first `git add`, and they go into history. Getting them back out is far
   more work than putting the file there first.
2. **`global.json` second.** Pins the SDK to `10.0.100`. `dotnet new` templates differ
   between SDK versions, so pinning makes scaffolding reproducible.
3. **`Directory.Build.props` third.** MSBuild automatically reads this file for every
   project at or below the folder. Putting `TargetFramework`, `Nullable` and
   `ImplicitUsings` here means each `.csproj` inherits them instead of carrying its
   own copy that drifts.
4. **`dotnet new sln`** — creates `EsportsEngine.sln`, a plain text file listing projects.
5. **`dotnet new console`** → `src/Esports.Console`, then `dotnet sln add` to register it.
6. **Trimmed the generated `.csproj`** — deleted the `TargetFramework`/`Nullable`/
   `ImplicitUsings` lines the template wrote, because `Directory.Build.props` already
   supplies them. Then built and confirmed output landed in `bin/Debug/net10.0/` —
   which *proves* the inheritance works rather than assuming it.
7. **`dotnet new webapi --use-controllers`** → `src/Esports.Api`.
8. **Read the build warnings** instead of ignoring them (see section 8).
9. **`npm create vite@latest client -- --template react`**, then pinned React 18 and
   fixed two separate toolchain problems (section 8).
10. **Docs skeleton, then skills, then README.**
11. **`git init` → `git add` → `git commit` → `git remote add` → `git push -u origin main`.**

---

## 3. How it works behind the scenes

### What `dotnet build` actually produced

```
src/Esports.Console/
  obj/          <- compiler scratch space
    project.assets.json          the resolved dependency graph after restore
    *.GlobalUsings.g.cs          the `using` directives ImplicitUsings injects
    *.AssemblyInfo.cs            generated assembly attributes
    Debug/net10.0/*.dll          the intermediate compiled output
  bin/Debug/net10.0/             <- the finished, runnable output
    Esports.Console.dll          YOUR CODE, as IL — this is the real program
    Esports.Console.exe          a tiny native launcher that boots the CLR
    Esports.Console.pdb          debug symbols: IL offsets -> source line numbers
    *.deps.json                  dependency manifest the runtime reads at startup
    *.runtimeconfig.json         which runtime version + GC settings to use
```

Both folders are regenerable, which is exactly why both are gitignored.

### Evidence that `Directory.Build.props` was actually applied

The `Esports.Console.csproj` contains no `<TargetFramework>` at all. Yet:

```
$ ls src/Esports.Console/bin/Debug/
net10.0
```

The output path is derived from `TargetFramework`. It could only be `net10.0` if the
value came from somewhere — and the only place it exists is `Directory.Build.props`.

### The declared version is not the version you get

`Esports.Api.csproj` declares one package:

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.12" />
```

But NuGet resolves a whole *graph*. The truth about what you actually build against
lives in `obj/project.assets.json`:

```
$ grep -o '"Microsoft.OpenApi/[^"]*"' src/Esports.Api/obj/project.assets.json
"Microsoft.OpenApi/2.12.0"
```

`Microsoft.OpenApi` is never mentioned in the `.csproj`. It arrived transitively.
This distinction is the whole reason the vulnerability in section 8 existed.

---

## 4. Why it's needed

**Without `.gitignore` first:** `bin/`, `obj/` and `node_modules/` (tens of thousands
of files) end up in git history. Clones become slow, diffs become unreadable, and
removing them later means rewriting history.

**Without `global.json`:** the machine picks the newest installed SDK. Six months from
now, `dotnet new` produces a different template and the project scaffolds differently.

**Without `Directory.Build.props`:** four projects each declare `net10.0`. Upgrading
means editing four files, and forgetting one produces a confusing runtime mismatch.

**Without reading build warnings:** you ship a known high-severity vulnerability on
day zero, because the template gave it to you and you assumed a template was vetted.

---

## 5. Where it fits

This is the foundation every other day stands on:

- `src/Esports.Console` is where Days 1–8 live — C# language and runtime
  fundamentals, with the real domain classes in memory.
- `src/Esports.Core` (Day 5) will be extracted from that console project, and is the
  same project the API references on Day 10. **Nothing gets moved between projects
  later** — that was the point of fixing the names now.
- `src/Esports.Api` gets dissected line by line on Day 9. It is deliberately left
  with its `WeatherForecastController` intact so there is something to take apart.
- `client/` sits idle until Day 17.

---

## 6. Coming from functional programming

**The analogy:**
`.csproj` is `package.json` / `.cabal` / `dune-project` — a build manifest.
NuGet is npm / Hackage / opam.
The `.sln` is a workspace file listing the packages in the monorepo.

**Where the analogy breaks:**
- **NuGet has ONE global cache** at `C:\Users\<you>\.nuget\packages\`, shared by every
  project on the machine. There is no per-project `node_modules`. Projects reference
  the cached DLL and copy it to `bin/` at build time.
- **`Directory.Build.props` has no npm equivalent.** MSBuild walks *up* the directory
  tree and applies any props file it finds. Settings can therefore arrive from a file
  you are not looking at — which is powerful and occasionally baffling.
- **A `.dll` carries full type metadata**, not just compiled code. That is why C# needs
  no header files, why IntelliSense works against compiled libraries you have no
  source for, and why ILSpy can reconstruct readable C#.

---

## 7. New C# syntax I met today

None — no C# was written today beyond what the templates generated. Day 1 starts the
language properly.

What *was* new is MSBuild's XML vocabulary:

| Element | Means |
|---|---|
| `<TargetFramework>net10.0</TargetFramework>` | which framework to build **for** (separate from the SDK you build **with**) |
| `<Nullable>enable</Nullable>` | turns on nullable reference types — a compile-time-only feature |
| `<ImplicitUsings>enable</ImplicitUsings>` | auto-injects common `using` directives via a generated file in `obj/` |
| `<OutputType>Exe</OutputType>` | produce an executable rather than a library |
| `<PackageReference Include=... Version=... />` | a NuGet dependency |

---

## 8. Traps and gotchas

Three real ones, all hit today.

- **Trap: the project template shipped a known vulnerability.**
  `dotnet new webapi` pinned `Microsoft.AspNetCore.OpenApi 10.0.0`, which pulls
  `Microsoft.OpenApi 2.0.0` — published advisory GHSA-v5pm-xwqc-g5wc, high severity.
  **Why it happens:** templates are versioned with the SDK and don't track advisories.
  **How to avoid it:** read the build output. Two warnings is not zero warnings.
  Fixed by upgrading to `10.0.12` → resolves `Microsoft.OpenApi 2.12.0` → 0 warnings.

- **Trap: `npm run build` died with "Cannot find native binding".**
  **Why it happens:** the Vite template installed Vite 8, which requires Node
  `^20.19.0 || >=22.12.0`. This machine has **Node 20.18.1**. npm warned about it
  (`EBADENGINE`) and the warning got skimmed past. The error message blames npm's
  optional-dependency bug, which is a red herring.
  **How to avoid it:** when a native module "can't be found", check
  `node -p "require('./node_modules/vite/package.json').engines"` against
  `node --version` *before* deleting `node_modules`.

- **Trap: the obvious downgrade introduced a different vulnerability.**
  Pinning Vite 5 fixed the Node problem but `npm audit` then reported the esbuild
  dev-server advisory (GHSA-67mh-4wv8-2f99). Vite 6.4.3 satisfies both constraints.
  **Lesson:** fixing one constraint can violate another. Re-audit after every pin.

- **Trap: Vite no longer defaults to React 18.** The template installs React 19.
  Pinned explicitly to `18.3.1` and verified with `npm ls react`.

---

## 9. Checkpoint questions

1. **Q:** You delete `bin/` and `obj/` and run `dotnet build`. What gets regenerated,
   and which step produces each?
   **Answer:** `restore` writes `obj/project.assets.json` and the nuget `.props`/
   `.targets` files. Compilation writes the generated `.cs` files (`GlobalUsings`,
   `AssemblyInfo`) and the intermediate DLL into `obj/`. The copy step produces
   `bin/Debug/net10.0/` with the DLL, the apphost `.exe`, the `.pdb`, `.deps.json`
   and `.runtimeconfig.json`.

2. **Q:** `Esports.Console.csproj` contains no `<TargetFramework>`. How does the
   compiler know to target `net10.0`, and how did you *prove* it rather than assume it?
   **Answer:** MSBuild automatically imports `Directory.Build.props` from an ancestor
   directory. Proof: the build output landed in `bin/Debug/net10.0/`, and the output
   path is derived from `TargetFramework` — it could not be that value by accident.

3. **Q:** `Microsoft.OpenApi` appears nowhere in `Esports.Api.csproj`, yet a
   vulnerability in it broke the build. How, and where would you look to confirm?
   **Answer:** it's a transitive dependency of `Microsoft.AspNetCore.OpenApi`. NuGet
   resolves the full graph, and the resolved result is recorded in
   `obj/project.assets.json` — that file, not the `.csproj`, is the truth.

4. **Q:** Why must `.gitignore` exist before the first `dotnet new` rather than before
   the first `git add`?
   **Answer:** it technically only needs to exist before `git add`. But scaffolding
   immediately creates `bin/`/`obj/`, and the realistic failure mode is scaffolding,
   then `git add .` without thinking. Creating it first removes the window entirely.

5. **Q:** What is in `Esports.Console.exe`, given the code is about 5 lines?
   **Answer:** not your code. It's the **apphost** — a small native launcher that
   boots the CLR and hands it `Esports.Console.dll`. Your code is the IL inside the
   DLL. Proof: `dotnet bin/Debug/net10.0/Esports.Console.dll` runs the program
   without the `.exe` at all.

---

## 10. Commands I ran

```powershell
# Order matters: ignore rules and SDK pin BEFORE any scaffolding
dotnet new gitignore
# ...appended React / Visual Studio / secrets sections by hand...

# global.json  -> pins SDK 10.0.100
# Directory.Build.props -> net10.0, Nullable, ImplicitUsings for ALL projects

dotnet new sln -n EsportsEngine
dotnet new console -o src/Esports.Console
dotnet sln add src/Esports.Console/Esports.Console.csproj
dotnet build src/Esports.Console -v:m

dotnet new webapi -o src/Esports.Api --use-controllers
dotnet sln add src/Esports.Api/Esports.Api.csproj
dotnet add src/Esports.Api package Microsoft.AspNetCore.OpenApi --version 10.0.12
dotnet build src/Esports.Api -v:m

npm create vite@latest client -- --template react
cd client
# pinned react/react-dom to 18.3.1 and vite to ^6.4.3 in package.json
npm install
npm ls react react-dom vite
npm run build

git init
git add -A
git commit -m "chore(day-00): scaffold repo, docs system and skills"
git branch -M main
git remote add origin https://github.com/saim-edev/c-.git
git push -u origin main
```

---

## 11. What tomorrow depends on

Day 1 needs, and has:

- `src/Esports.Console` building and runnable via `dotnet run --project src/Esports.Console`
- `docs/days/_TEMPLATE-day.md` to copy
- A working `git push` so the day can be committed and tagged

**Carried forward, not done today:**

- `Esports.Core`, `Esports.Infrastructure` — created on Days 5 and 10 respectively
- Neon Postgres account — Day 10
- The `WeatherForecastController` is deliberately left in `Esports.Api` so Day 9 has
  something real to dissect before deleting it
- The React client is static and does not call the API. Wiring needs CORS, which is
  Day 16, and the client work is Day 17
