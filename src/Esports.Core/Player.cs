// A blueprint. This says "a Player is these four things bundled together".
// Writing this creates a new TYPE, the same way `string` and `int` are types.
// It does not create any actual player yet - it's the shape, not the thing.

public class Player
{
    // ---- DATA ----

    // `private set` = anyone can READ this, but only code inside Player
    // can CHANGE it. That one word is what turns the methods below from
    // a polite suggestion into the only way in.
    public string GamerTag { get; private set; }
    public int Rating { get; private set; }
    public double WinRate { get; private set; }
    public bool IsActive { get; private set; }

    // ---- CONSTRUCTOR: runs when you say `new Player(...)` ----

    // Because the setters are private, values can no longer be assigned
    // from outside. They must be supplied here, at the moment of creation.
    // Result: a Player with no tag or rating is now impossible to build.
    public Player(string gamerTag, int rating)
    {
        GamerTag = gamerTag;
        Rating = rating;
        WinRate = 0.0;
        IsActive = true;
    }

    // ---- BEHAVIOUR ----

    // `void` means this hands nothing back - it just changes the player.
    // Notice there is no `player.` prefix inside: a method already knows
    // WHICH player it was called on. faker.RecordWin() changes faker.
    public void RecordWin()
    {
        // `+=` means "add to what's already there".
        Rating += 25;
    }

    public void RecordLoss()
    {
        Rating -= 25;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
