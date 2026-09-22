# Glossary

Plain language only. **No term in here is allowed to be defined using another
undefined term.** If a definition needs jargon, the jargon gets its own entry first.

Appended to as terms come up. Alphabetical.

---

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
