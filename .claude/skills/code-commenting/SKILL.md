---
name: code-commenting
description: The line-by-line comment style for the C#/.NET curriculum — theory plus implementation above meaningful lines, and an explicit list of what NOT to comment. Use when writing or annotating any code in this repo.
---

# Code commenting style

Saim asked for comments that "dive deep into it, both theory and implementation".
The risk is noise. A file where every line is commented is a file where **no**
line is commented, because the eye stops reading.

## The rule

Comment **meaningful** lines — the ones where something non-obvious happens at the
language, runtime, or design level. For those, answer as many of these as apply:

1. **What** this line does.
2. **What the runtime/compiler does with it** — the part that isn't visible.
3. **Why it's written this way** — and what the alternative would have cost.

## What NOT to comment

Never write these. They add length and subtract attention:

```csharp
// BAD — restates the code
// Set the name to the value of name
this.Name = name;

// BAD — narrates the obvious
// Loop through the teams
foreach (var team in teams)

// BAD — comments the language, not the intent
// Declare a string variable
string tag;
```

Also skip: getters/setters that do nothing, `using` directives, closing braces,
and anything a competent reader gets for free from the identifier name.

## Worked example

**Before — noise:**
```csharp
// Create a list of players
private readonly List<Player> _players = new();

// Add a player
public void AddPlayer(Player player)
{
    // Add the player to the list
    _players.Add(player);
}
```

**After — signal:**
```csharp
// `readonly` stops the FIELD being reassigned. It does NOT freeze the list's
// contents — callers who get a reference to this list could still mutate it,
// which is exactly why the public surface below exposes IReadOnlyList instead.
private readonly List<Player> _players = new();

// Exposing IReadOnlyList rather than List is the whole point of encapsulation
// here: if callers could .Add() directly, Team could never guarantee the roster
// limit. Shrinking the set of places that can break an invariant IS the feature.
public IReadOnlyList<Player> Players => _players;

public void AddPlayer(Player player)
{
    // Validate BEFORE mutating. If this threw halfway through a multi-step
    // change we'd leave the object in a state its own rules say is impossible.
    if (_players.Count >= MaxPlayers)
        throw new RosterFullException(Id, MaxPlayers);

    _players.Add(player);
}
```

Three comments instead of three. But now each one teaches something the code
cannot say for itself.

## Density guide

- **New concept, first time he meets it:** comment generously. This is a lesson.
- **A pattern already established:** comment only the deviation.
- **Third time onward:** stop commenting it. It's vocabulary now.

When a comment would run longer than about six lines, it isn't a comment — it
belongs in that day's doc under "How it works behind the scenes", with a one-line
pointer from the code.
