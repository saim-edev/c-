# Thinking like a backend developer

**What this file is for:** the goal here is not "learn C#". It is to become a backend
developer whose foundations are strong enough to pick up anything later. C# is the
vehicle.

This file is the part that isn't syntax. It gets appended to as more of it shows up in
real work.

---

## 1. The instinct underneath everything

Every design decision made in this repo so far is the same move:

> **Make it impossible to get wrong, instead of remembering not to.**

A junior asks *"will I remember to check this?"*
A senior asks **"where do I put this so nobody can skip it?"**

Look at what that produced, day by day:

| Day | Possible before | What closed it |
|---|---|---|
| 1 | `faker.Rating = 999999` from anywhere in the program | `private set` |
| 2 | Adding a 6th player to a 5-player team | the limit inside `AddPlayer()` |
| 3 | A negative score; a team playing itself | guards in the constructor |
| 4 | Recording a result on a match that never started | the state machine |

**The usual answer is: at the boundary where the thing is created.** Validate once, at
construction, and every line downstream can trust it. That is why
[MatchScore.Create()](../../src/Esports.Console/MatchScore.cs#L30) throws on a negative
score instead of every caller checking.

The alternative — checking at each place a value is used — fails for one boring reason:
**there will eventually be a place you forgot.** Not maybe. Eventually.

---

## 2. What order to write things in

Build **from the inside out**. The thing with the fewest dependencies first.

That is not a style preference; it is forced. If file A mentions file B, B must exist
or nothing compiles.

```
  Player         needs nothing
     │
     ▼
  Team           needs Player   (it holds a List<Player>)
     │
     ▼
  MatchScore     needs nothing, but Match needs it
     │
     ▼
  Match          needs Team AND MatchScore AND MatchState
```

**The same rule scales up.** On Day 10, the order will be:

```
  entities  →  database context  →  migrations  →  services  →  controllers
  (the      (how to save them)   (the actual   (the rules   (the HTTP
   things)                        tables)       applied)     front door)
```

Never controllers first. A controller with nothing to call is a form with no building
behind it.

**How to know you got it backwards:** you find yourself writing a file full of
`TODO: this needs X which doesn't exist yet`. Stop and go build X.

---

## 3. Don't add structure before the pain

There are no folders inside the console project. Six files is not a filing problem.

This is deliberate and it is a real principle:

> **Structure invented before the pain arrives is usually the wrong structure.**

You do not yet know how this code wants to be organised, because it has not grown
enough to show you. Guess now and you will spend later effort moving things out of
folders that turned out to be wrong.

**When to add it:** when you find yourself scrolling to find a file, or when two things
in the same folder clearly belong to different jobs. Usually around 15–20 files.

The same applies to every "best practice" that shows up as ceremony:

| Thing | Add it when |
|---|---|
| Folders | you cannot find a file |
| A service layer | the rules stop fitting on the entity |
| An interface | you have a **second** implementation, or you need to fake it in a test |
| A repository | you can name what it buys you that `DbContext` doesn't |
| A new project | you want the **compiler** to enforce a boundary, not just a convention |

Notice the last one: splitting into projects on Day 10 is not tidiness. It is that
`Esports.Core` will have no reference to the database library, making it *physically
impossible* to write database code in the domain. A folder cannot enforce that. A
missing project reference can.

---

## 4. How to read a repo you didn't write

This is the single most transferable skill on this list, and it is the same in any
language.

**Find the entry point and follow what it calls.**

For this repo, the order:

```
  1. README.md              what is this, how do I run it
  2. docs/INDEX.md          what has been built
  3. docs/THE-BIG-PICTURE.md how the pieces fit
  4. src/.../Program.cs     THE ENTRY POINT - always start here
  5. follow the code        Program.cs uses Match, Match uses Team, ...
```

For a .NET web application specifically, `Program.cs` is the most information-dense
file in the whole project. Every feature, every setting, every middleware step is
registered there, **in order**. Fifteen minutes reading it beats three hours wandering.

**What you are looking for, in order:**

1. **How many projects, and what are they called?** The names tell you the architecture
   before you read a line of code.
2. **What packages does it use?** The packages *are* the stack. See a Postgres driver,
   you know the database. See a logging library, you know how to find the logs.
3. **Which way do the references point?** Which project depends on which. That is the
   dependency direction, and it tells you what the authors considered "the core".
4. **The entry point**, read top to bottom.
5. **The data model.** The entities or the database schema. Understand the data and you
   understand most of the app.
6. **One feature, end to end.** Pick the simplest and trace it all the way through.

Day 24 does this properly, on a repo with a bug planted in it.

---

## 5. Naming errors so they help the person reading them at 2am

C# has different exception types and picking the right one is not pedantry.

| Type | Means | Example here |
|---|---|---|
| `ArgumentException` | **that input was wrong** | a negative score; a winner who isn't playing |
| `InvalidOperationException` | **you can't do that right now** | starting a match that already finished |

The difference matters because they point at different culprits. `ArgumentException`
says *the caller passed nonsense*. `InvalidOperationException` says *the caller asked
at the wrong time*. One is a bad value, the other is bad sequencing — and you look in
different places for each.

**Also: put the actual values in the message.**

```csharp
throw new ArgumentException($"Scores cannot be negative: {home}-{away}");
```

```
refused: Scores cannot be negative: -1-3
```

Not `"Invalid score"`. When this shows up in a log at 2am, `-1-3` tells you what
happened. `"Invalid score"` starts a thirty-minute investigation.

---

## 6. Comments: what is worth writing down

Comment the things the code **cannot say for itself**:

- **Why** this approach and not the obvious one
- A rule or constraint that isn't visible locally
- A trap the next reader will otherwise fall into

Do **not** comment what the code already says:

```csharp
// BAD - restates the code
// Add the player to the list
_players.Add(player);

// GOOD - says something the code cannot
// `private`, NOT `private set`. A readable List<T> hands out the arrow to
// the real roster, so any caller could .Add() past the five-player limit.
private List<Player> _players = new List<Player>();
```

If a comment would run longer than about six lines, it is not a comment — it belongs in
a doc, with a one-line pointer from the code.

---

## 7. Leave the trail behind you

Three habits that pay off later, all of which this repo does:

**A decisions log.** [DECISIONS.md](../DECISIONS.md) records every "why X and not Y",
with what it cost. Six months on, *"why is `Player` a class but `MatchScore` a record?"*
has a written answer instead of an argument. This is also the file that makes you
useful in a code review and in an interview — being able to defend a choice *and name
its cost* is the difference between having used something and having chosen it.

**A bug log.** [debugging.md](debugging.md) records every real bug: the symptom, the
root cause, how it was found, and what would have prevented it. Over a year this becomes
more valuable than any course, because it is a record of *how you specifically* get
things wrong.

**Commits that explain why.** Not `fix stuff`. The message body says what was wrong and
why this fix rather than another. A year later, `git log` is the only documentation
that cannot go stale, because it is attached to the change itself.

---

## 8. Small commits that always build

Every commit in this repo compiles and runs. That is not tidiness either — it is what
makes `git bisect` possible.

`git bisect` finds the commit that introduced a bug by binary search: it checks out a
midpoint, you say "broken" or "fine", it halves the range. **About seven tests to find
the culprit among a hundred and twenty commits.**

It is useless if half your commits don't build, because those steps become "skip". So
the discipline pays for itself the first time something breaks mysteriously.

Day 24 does this for real, on a planted regression.

---

## 9. Things a senior notices that a junior doesn't

A running list. Each one has already come up in this project.

**Read the build warnings.** On Day 0, the freshly generated Web API built with two
warnings, not zero. One of them was a **known high-severity vulnerability** in a package
the template pinned. A template is a starting point, not a vetted artifact.

**The declared version is not the version you get.** `Esports.Api.csproj` declares one
package. The one with the vulnerability was never mentioned in it — it arrived
transitively. The truth about what you actually build against lives in
`obj/project.assets.json`, after dependency resolution.

**Engine/runtime mismatches hide behind confusing errors.** A Vite build failed with
"Cannot find native binding", which blames a package manager bug. The real cause was
Node 20.18 against a tool requiring 20.19+. A warning had said so and was skimmed past.

**Fixing one constraint can violate another.** Downgrading that tool fixed the Node
problem and reintroduced a security advisory. Always re-check after a pin.

**Integer division is silent.** `9031 / 5` gives `1806`, not `1806.2`. No error, no
warning, just a quietly wrong number in a report someone trusts.

**Ask what happens when two people do this at once.** Not relevant yet in a console app.
It becomes the defining question the moment there is a database and more than one user.

---

## 10. The question to ask about any piece of code

When you cannot tell whether something is good:

> **How many places can break this rule?**

That single question produced `private set`, the roster limit inside `AddPlayer`, the
guards in the constructors, the state machine, and it will produce the project split on
Day 10.

Before: *everywhere*. After: *one file you can actually read*.

---

*Appended to as more of this shows up in real work. Related:
[THE-BIG-PICTURE.md](../THE-BIG-PICTURE.md), [DECISIONS.md](../DECISIONS.md),
[debugging.md](debugging.md).*
