# Glossary

Plain language only. **No term in here is allowed to be defined using another
undefined term.** If a definition needs jargon, the jargon gets its own entry first.

Appended to as terms come up. Alphabetical.

---

**Aliasing** — two variables holding the address of the *same* object, so a change
made through one is visible through the other. The default for any `class` you write.

**Assembly** — a compiled `.dll` or `.exe`. Contains IL (the half-compiled code),
metadata (a full description of every type and method inside), and a manifest
(name, version, what it depends on). The metadata is why C# needs no header files
and why tools like ILSpy can reconstruct readable source.

**BCL (Base Class Library)** — the set of types that ship with .NET itself:
`string`, `List<T>`, `DateTime`, and so on. The standard library.

**CLR (Common Language Runtime)** — the engine that runs your code. It loads
assemblies, JIT-compiles IL to machine code, manages the heap, and runs the
garbage collector.

**IL (Intermediate Language)** — what the C# compiler actually produces. A
CPU-independent instruction set. Not machine code yet; the JIT does that step.

**JIT (Just-In-Time compiler)** — converts IL into real machine code the first
time each method is called, then caches it. This is why the first call to a
method is slower than the second.

**MSBuild** — the build engine. Reads `.csproj` files (which are XML build
scripts) and works out what to compile, in what order.

**NuGet** — the package manager. A `.nupkg` is a ZIP containing a DLL plus a
manifest. Packages live in one global cache at
`C:\Users\<you>\.nuget\packages\`, shared across every project on the machine —
unlike `node_modules`, which is per-project.

**SDK vs Runtime** — the SDK is the toolbox (compiler, CLI, MSBuild) and is what
you need to *build*. The runtime is what you need to *run*. Installing "the SDK"
gives you both.

**TFM (Target Framework Moniker)** — the `net10.0` string in a `.csproj`. It says
which framework version you are building *for*. This is a different knob from the
SDK version you are building *with*.

**Guard** — a check at the top of a constructor or method that throws if the input is
invalid. Its value is that an invalid object then cannot exist *at all* — not even
briefly in a variable someone forgot about.

**Identity vs value** — the question that decides `class` vs `record`. A thing with
identity survives its values changing (a player is still that player after a rating
change). A value simply *is* its contents (any 3-1 score is any other 3-1 score).

**Nullable reference type (`string?`, `MatchScore?`)** — an annotation meaning "this
might be nothing". **Erased at runtime** — `MatchScore?` and `MatchScore` compile to
identical code. It drives compiler warnings only; it is a linter, not a guarantee.

**Override** — replace a method inherited from a base type. Every type in C# inherits
`ToString()`, `Equals()` and `GetHashCode()` from `object`, and `override` swaps in
your own version.

**Property** — looks like a field to callers but is really a pair of methods. That is
why `PlayerCount` can run code on every read while reading like stored data.

**Record** — a type whose equality is based on contents rather than address. One line
generates the constructor, `==`, `Equals`, `GetHashCode`, `ToString` and `with`.

**Reference type / value type** — a value type variable holds the value itself; a
reference type variable holds the *address* of an object elsewhere. Copying a value
type copies the data; copying a reference type copies the address.

**Static member** — belongs to the type, not to any one instance. Called as
`MatchScore.Create(...)`, never `someScore.Create(...)`.

**Enum** — a fixed, named set of options that becomes a new type. Exactly as many
values as you listed, all known to the compiler. **Not a sum type**: carries no
payload, is not exhaustiveness-checked, and is an `int` underneath, so `(MyEnum)99`
compiles and runs.

**State machine** — a set of states plus rules about which moves between them are
allowed. `Match` is one: `Scheduled → InProgress → Completed`, with forfeit and cancel
as alternative endings. Each method enforces one arrow and throws on anything else.

**Switch expression** — pattern matching that *produces a value*, so it can be assigned
directly. `_` is the catch-all arm. One of the few places C# is expression-oriented
rather than statement-oriented.

**Rich vs anemic domain model** — rich: the rules live on the entity itself
(`player.RecordWin()`). Anemic: entities are data bags and the rules live in separate
service classes. This project is rich; much NestJS code is anemic. Both are valid.
