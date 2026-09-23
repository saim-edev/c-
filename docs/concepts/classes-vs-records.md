# Classes vs records — identity or value?

The one question to ask before declaring any type.

> **Does this thing have an identity that survives its values changing?**
> Yes → `class`. No, it *is* its contents → `record`.

---

## The test, applied

| Thing | Choice | Why |
|---|---|---|
| `Player` | `class` | Faker is still Faker after a rating change. Two people can share stats and still be two people. |
| `Team` | `class` | A roster change doesn't make it a different team. |
| `Match` | `class` | The same fixture, before and after it's played. |
| `MatchScore` | `record` | A 3-1 is any other 3-1. Nothing else to it. |
| An ID, a date range, a money amount | `record` | Pure values. |

---

## What actually differs

### Equality

```csharp
Player p1 = new Player("Faker", 1847);
Player p2 = new Player("Faker", 1847);
Console.WriteLine(p1 == p2);          // False - compares ADDRESSES

MatchScore a = new MatchScore(3, 1);
MatchScore b = new MatchScore(3, 1);
Console.WriteLine(a == b);            // True  - compares CONTENTS
Console.WriteLine(ReferenceEquals(a, b));  // False - still two objects
```

A `class`'s `==` compares addresses and never looks inside. A `record`'s `==` compares
every member.

`string` is the odd one out — a reference type whose `==` C# overrides to compare
characters. A special case in the language, not the rule.

### What one line of `record` generates

```csharp
public record MatchScore(int Home, int Away);
```

- constructor
- `==`, `!=`, `Equals`, `GetHashCode` — all content-based
- `ToString()` listing every property
- properties read-only after construction
- `with`

A `class` gives you none of it. `Player` is 48 hand-written lines and still prints as
just `Player`.

### `with`

```csharp
MatchScore flipped = a with { Home = 1, Away = 3 };
```

Copies, then overrides what you name. The original is untouched.

**Shallow.** A record holding a `List<T>` shares that list with its copy.

---

## Gotchas

- **A record's `ToString()` includes computed properties too.** Adding `IsDraw`,
  `Margin` and `GamesPlayed` turned a clean `MatchScore { Home = 3, Away = 1 }` into a
  seven-field wall. Fix by overriding it:
  ```csharp
  public override string ToString() => $"{Home}-{Away}";
  ```

- **Record equality covers every member.** Add a property a month later and equality
  silently changes meaning everywhere it's used.

- **A record is still a reference type** (unless declared `record struct`). Aliasing
  applies; it's just that immutability makes it harmless.

- **Don't make everything a record because immutability feels right.** An entity that
  genuinely changes over time — a `Match` moving from scheduled to finished — is
  clearer as a class with controlled mutators.

---

## Coming from functional programming

`record` is home: structural equality, `with` is record-update syntax, positional
declaration is a product type.

**The leaks:**
1. `with` is shallow — no structural sharing, no persistence.
2. Equality includes every member, silently, forever.
3. **You have to choose per type.** In a world of immutable values the identity-vs-value
   question rarely arises. Here it's a design decision, and getting it wrong fails
   quietly rather than loudly.

---

*Introduced: [Day 3](../days/day-03-equality-records-and-nothing.md)*
