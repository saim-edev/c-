# Index — 25 Days

**Start here if you are lost:** [THE-BIG-PICTURE.md](THE-BIG-PICTURE.md) — how every
piece built so far fits together, as one flow.

## Reference files (read these, not the day notes, when you need to look something up)

| File | What it covers |
|---|---|
| [THE-BIG-PICTURE](THE-BIG-PICTURE.md) | the map — how it all joins up, and where it is going |
| [thinking-like-a-backend-dev](concepts/thinking-like-a-backend-dev.md) | the craft: file order, reading a repo, what seniors notice |
| [memory-and-references](concepts/memory-and-references.md) | what a variable holds; aliasing |
| [collections](concepts/collections.md) | `List<T>`, `foreach`, choosing a collection |
| [classes-vs-records](concepts/classes-vs-records.md) | identity or value? |
| [nullability-and-guards](concepts/nullability-and-guards.md) | saying "nothing"; refusing invalid objects |
| [enums-and-state-machines](concepts/enums-and-state-machines.md) | modelling a lifecycle |
| [interfaces-and-polymorphism](concepts/interfaces-and-polymorphism.md) | one call, many answers; when NOT to use one |
| [debugging](concepts/debugging.md) | the bug log and the method |
| [GLOSSARY](GLOSSARY.md) · [CHEATSHEET](CHEATSHEET.md) · [DECISIONS](DECISIONS.md) | terms · C# vs functional · why X not Y |

## The days


One row per day. Filled in as each day completes.

| Day | Title | Doc | Tag | Concepts touched |
|---|---|---|---|---|
| 0 | Setup: repo, SDK pin, scaffolds | [day-00](days/day-00-setup.md) | `day-00` | git, .gitignore, global.json, MSBuild, npm |
| 1 | Types, classes, and encapsulation | [day-01](days/day-01-types-classes-encapsulation.md) | `day-01` | types, classes, constructors, methods, `private set` |
| 2 | Collections, and what a variable actually holds | [day-02](days/day-02-collections-and-references.md) | `day-02` | `List<T>`, `foreach`, computed properties, value vs reference, aliasing |
| 3 | Equality, records, and how to say "nothing" | [day-03](days/day-03-equality-records-and-nothing.md) | `day-03` | `==` vs contents, `record`, `with`, nullable `?`, `?.`, `??`, guards |
| 4 | Enums, and a match that can't cheat | [day-04](days/day-04-enums-and-state-machines.md) | `day-04` | `enum`, state machine, guarded transitions, `switch` expression |
| 5 | Interfaces: one call, two different answers | [day-05](days/day-05-interfaces-and-polymorphism.md) | `day-05` | `Tournament`, `interface`, polymorphism, `IReadOnlyList<T>` |
| 6 | Inheritance, interfaces, polymorphism | — | — | `callvirt`, v-tables, abstract classes, strategies |
| 7 | Absence, failure, cleanup | — | — | nullability, exceptions, GC, `IDisposable` |
| 8 | Functions as values | — | — | delegates, lambdas, LINQ, events, async intro |
| 9 | Kestrel and the life of a request | — | — | HTTP, middleware, routing, the 23-step journey |
| 10 | DI, configuration, and Neon | — | — | DI lifetimes, options, user-secrets, Npgsql |
| 11 | EF Core and your first migration | — | — | `DbContext`, change tracker, migrations |
| 12 | Relationships, TPH, Postgres types | — | — | FKs, TPH, owned types, indexes, UUIDv7 |
| 13 | How LINQ becomes SQL | — | — | `IQueryable`, expression trees, N+1, tracking |
| 14 | DTOs, binding, validation, services | — | — | model binding, FluentValidation, filters |
| 15 | Errors, logging, transactions, concurrency | — | — | `IExceptionHandler`, Serilog, `xmin` |
| 16 | JWT auth, authorization, CORS | — | — | PBKDF2, JWT, policies, preflight |
| 17 | File upload + a JS/React primer | — | — | multipart, `IFormFile`, JS basics, Vite |
| 18 | The React UI and end-to-end delivery | — | — | components, fetch, ProblemDetails |
| 19 | Visual Studio: the map | — | — | `.sln`, F12/Ctrl+F12/Shift+F12 navigation |
| 20 | The debugger, properly | — | — | breakpoints, tracepoints, call stack, watch |
| 21 | SignalR: live scores | — | — | WebSockets, hubs, groups |
| 22 | Background jobs | — | — | `BackgroundService`, `Channel<T>`, scopes |
| 23 | Observability across async boundaries | — | — | stack traces, correlation IDs |
| 24 | How backend developers actually work | — | — | reading a repo, bisect, blame, code review |
| 25 | The whole system, end to end | — | — | full narration, self-assessment, roadmap |
