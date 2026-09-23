# Day 1 — Types, classes, and encapsulation

> **Goal:** Go from loose variables to a `Player` type that carries its own rules and cannot be put into an invalid state.
> **Date:** 2026-09-22 · **Tag:** `day-01`

---

## 0. Where we were

Nothing existed. [Day 0](day-00-setup.md) built the repo, pinned the SDK and scaffolded
two empty projects, but no C# had been written. The console project contained the
template's two-line "Hello, World!" and nothing else.

*Previous: [Day 0 — Setup](day-00-setup.md)*

---

## 1. What we built today

[Player.cs](../../src/Esports.Console/Player.cs) — the first C# class. It holds four
pieces of data about an esports player, supplies them through a constructor, and
exposes three methods that are the *only* way the rating can change.
[Program.cs](../../src/Esports.Console/Program.cs) creates two players, plays a match
between them, and prints the result.

**Note on sequencing:** this day originally opened with runtime internals (IL, JIT,
the apphost). That was abandoned mid-session — inspecting a compiled assembly is
meaningless before you can read the language. The machinery gets explained alongside
the code that needs it instead.

---

## 2. The flow

How we got here, and why each step was forced by the one before it:

1. **Four loose variables** for one player: `gamerTag`, `rating`, `winRate`, `isActive`.
2. **A second player doubled it to eight.** Nothing connected `player1Tag` to
   `player1Rating` except a naming habit. You could not pass "a player" to a function.
3. **A `class` grouped them into one thing.** Two variables instead of eight, and the
   compiler now knows those four values belong together.
4. **The rule "a win is worth 25 points" lived nowhere** — it was in my head and in
   every place I remembered to type `+ 25`.
5. **A method put the rule inside the class.** `faker.RecordWin()` — one place knows
   the number.
6. **But `faker.Rating = 999999;` still compiled.** The method was a suggestion, not
   a rule, because `{ get; set; }` left the door open.
7. **`private set` closed it** — only code inside `Player` may write.
8. **Which broke object-initializer syntax**, because that writes from outside. So the
   values had to be supplied at creation instead: a **constructor**.

### The shape of the change

```
BEFORE - loose variables                AFTER - one class
========================                =================

  player1Tag     "Faker"                  faker  ──► ┌──────────────────┐
  player1Rating  1847                                │ GamerTag  Faker  │
  player1WinRate 0.672                               │ Rating    1847   │
  player1IsActive true                               │ WinRate   0.672  │
                                                     │ IsActive  true   │
  player2Tag     "Chovy"                             ├──────────────────┤
  player2Rating  1792                                │ RecordWin()      │
  player2WinRate 0.641                               │ RecordLoss()     │
  player2IsActive true                               │ Deactivate()     │
                                                     └──────────────────┘
  8 variables.
  Related only by a naming habit.           chovy  ──► ┌──────────────────┐
  Cannot pass "a player" anywhere.                     │ GamerTag  Chovy  │
                                                       │ ...              │
                                                       └──────────────────┘

                                            2 variables. The compiler knows
                                            those values belong together.
```

---

## 3. How it works behind the scenes

### The code, in full

[Player.cs](../../src/Esports.Console/Player.cs) — this is the whole file, minus
comments:

```csharp
public class Player
{
    // `private set` = readable anywhere, writable ONLY inside this class
    public string GamerTag { get; private set; }
    public int Rating { get; private set; }
    public double WinRate { get; private set; }
    public bool IsActive { get; private set; }

    // Runs on `new Player(...)`. Because the setters are private, values
    // cannot be assigned from outside - they must come through here.
    public Player(string gamerTag, int rating)
    {
        GamerTag = gamerTag;
        Rating = rating;
        WinRate = 0.0;
        IsActive = true;
    }

    public void RecordWin()  { Rating += 25; }
    public void RecordLoss() { Rating -= 25; }
    public void Deactivate() { IsActive = false; }
}
```

And how it is used, from [Program.cs](../../src/Esports.Console/Program.cs):

```csharp
Player faker = new Player("Faker", 1847);
Player chovy = new Player("Chovy", 1792);

faker.RecordWin();
chovy.RecordLoss();

Console.WriteLine($"{faker.GamerTag} - {faker.Rating}");
Console.WriteLine($"{chovy.GamerTag} - {chovy.Rating}");
```

```
Faker - 1872
Chovy - 1767
```

### A method receives the object as a hidden first argument

[Player.cs:35](../../src/Esports.Console/Player.cs#L35):

```csharp
public void RecordWin()
{
    Rating += 25;
}
```

`Rating` has no prefix. No `player.`, no `this.`. So which player does it change?
Whichever one it was called on — because these two are the same thing:

```csharp
faker.RecordWin();        // what you write
Player.RecordWin(faker);  // what it actually means
```

**This is the single mechanic the whole of OOP is built on.** A class is a record,
plus functions that take that record as an implicit first argument. C# just moves
the first parameter to the left of the dot and hides it.

```
  What you write            What it means

  faker.RecordWin()   ──►   Player.RecordWin(faker)
  ^^^^^                                    ^^^^^
  moved to the left                  a normal first argument
  of the dot, and hidden

  There is ONE copy of the RecordWin code, shared by every
  Player. The object is what tells it which data to act on.

       ┌──────────────┐   ┌──────────────┐
       │ faker        │   │ chovy        │      the DATA differs
       │ Rating 1847  │   │ Rating 1792  │      per object
       └──────┬───────┘   └──────┬───────┘
              │                  │
              └────────┬─────────┘
                       ▼
              ┌──────────────────┐
              │ RecordWin()      │             the CODE exists once
              │   Rating += 25   │
              └──────────────────┘
```

### `private set` verified, not assumed

Feeding these two lines to the compiler against the real `Player.cs`:

```csharp
p.Rating = 999999;
p.GamerTag = "hacked";
```

```
error CS0272: The property or indexer 'Player.Rating' cannot be used
              in this context because the set accessor is inaccessible
error CS0272: The property or indexer 'Player.GamerTag' cannot be used
              in this context because the set accessor is inaccessible
```

### Types are checked before the program exists

```csharp
int rating = 1847;
rating = "not a number";
```

```
error CS0029: Cannot implicitly convert type 'string' to 'int'
```

No output, no partial run — the compiler refuses to produce a program at all. That
is what writing types out buys you, and it is the trade for the verbosity.

---

## 4. Why it's needed

**Without the class:** five players means twenty loose variables, related only by a
naming convention the compiler doesn't check. You can't pass a player anywhere.

**Without the method:** the rule "+25 per win" is duplicated at every call site.
Typo `+52` once and nothing tells you.

**Without `private set`:** the method is decoration. Any line anywhere can write
`faker.Rating = 999999`. The rule exists but is unenforceable.

**The real measure of encapsulation:** how many places in the program can break this
rule? Before: everywhere. After: 48 lines in one file.

---

## 5. Where it fits

`Player` is the first entity of the Esports Tournament Engine. `Team` will hold
several of them, `Match` will pair two teams, `Tournament` will organise matches.

The same class survives to the end of the course — on Day 10 it becomes a database
table. **The storage changes; the class does not.** Nothing written today is throwaway.

---

## 6. Coming from functional programming

**The analogy:**
A class is a **record plus the module of functions that operate on it**, with the
record passed implicitly as the first argument. `private set` is the same instinct as
not exporting a raw constructor from a module and exposing a smart constructor plus a
few operations instead. Both exist to **make illegal states unrepresentable**.

**Where the analogy breaks:**
- **Your version is usually enforced by immutability** — nobody can change the value
  at all. C# only *routes* change through methods you control. `Rating` is still
  mutable; it's just that only `Player.cs` may mutate it.
- **Granularity is per-member, not per-module.** `private` applies to one property,
  not to an export list.
- **The type is written out everywhere.** C# never infers across a method signature,
  so every parameter and return type is spelled. That is the cost of the compile-time
  checking above.

---

## 7. New C# syntax I met today

| Syntax | Means |
|---|---|
| `string gamerTag = "Faker";` | type first, then name. Declaration and assignment. |
| `$"Tag: {gamerTag}"` | string interpolation — `{ }` evaluates and pastes the value |
| `public class Player { }` | declares a new **type** (a blueprint), not an object |
| `{ get; set; }` | a property: readable and writable from anywhere |
| `{ get; private set; }` | readable anywhere, **writable only inside this class** |
| `public Player(string tag, int rating)` | constructor — runs on `new Player(...)` |
| `new Player("Faker", 1847)` | builds one actual object from the blueprint |
| `public void RecordWin()` | a method. `void` = hands nothing back |
| `faker.Rating` | the dot reaches inside an object |
| `Rating += 25;` | compound assignment — same as `Rating = Rating + 25;` |

The four types used: `string` (text), `int` (whole numbers), `double` (decimals),
`bool` (true/false).

---

## 8. Traps and gotchas

- **`{ get; set; }` is the default and it is wide open.** Writing a method that
  changes a value does nothing to stop anyone changing it directly. The method only
  becomes the *only* way in once the setter is `private`.

- **`private set` breaks object-initializer syntax.** This stops compiling:
  ```csharp
  new Player { GamerTag = "Faker", Rating = 1847 }
  ```
  That's not a bug — the initializer writes from outside. It forces a constructor,
  which is the point.

- **`true` prints as `True`.** You wrote lowercase, C# printed capital. Printing a
  value is not the same as the value itself.

- **Editor hints are not errors.** Two appeared today: *"use compound assignment"*
  (`+=`) and *"use primary constructor"*. Both are style suggestions. The primary
  constructor form was deliberately declined — it's shorter but hides where values
  land, which is worse while learning.

---

## 9. Checkpoint questions

1. **Q:** `RecordWin()` contains `Rating += 25` with no prefix. How does it know
   which player to change?
   **A:** The object is passed as a hidden first argument. `faker.RecordWin()` means
   `Player.RecordWin(faker)`.

2. **Q:** You have `RecordWin()`. Why does it not stop someone writing
   `faker.Rating = 999999`?
   **A:** It doesn't, on its own. `{ get; set; }` allows writing from anywhere. Only
   `private set` closes that.

3. **Q:** Why did adding `private set` break `new Player { Rating = 1847 }`?
   **A:** Object-initializer syntax assigns from outside the class, which is exactly
   what `private set` forbids. Values must go through the constructor instead.

4. **Q:** Encapsulation in one sentence, without using the word "hide".
   **A:** Shrinking the number of places in a program that can break a rule.

---

## 10. Commands I ran

```powershell
dotnet run --project src/Esports.Console
dotnet build src/Esports.Console --nologo
```

---

## 11. What's next

Working and committed:
- [Player.cs](../../src/Esports.Console/Player.cs) — data, constructor, three methods
- [Program.cs](../../src/Esports.Console/Program.cs) — two players, one match

**Next:** a `Team` holding several players. That needs a way to store many things at
once — collections — which is where C# differs from a functional language in a way
that is genuinely surprising.

**Deferred from today:** what the compiled `.dll` actually contains (IL, JIT). Revisit
once there is enough C# to make reading it meaningful.
