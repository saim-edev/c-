// A team is a name, a tag, and the players on its roster.

public class Team
{
    // ---- DATA ----

    public string Name { get; private set; }
    public string Tag { get; private set; }

    // A List<Player> is ONE variable holding MANY players.
    // <Player> is the element type - the compiler will reject anything else.
    //
    // `private` here, not `private set`. The difference matters:
    //   private set  -> outsiders can READ the list, and could then .Add() to it
    //   private      -> outsiders cannot see this field at all
    // The read-only view further down is what outsiders get instead.
    private List<Player> _players = new List<Player>();

    // ---- CONSTRUCTOR ----

    public Team(string name, string tag)
    {
        Name = name;
        Tag = tag;
    }

    // ---- BEHAVIOUR ----

    // The roster limit lives HERE, so it cannot be bypassed.
    public void AddPlayer(Player player)
    {
        if (_players.Count >= 5)
        {
            Console.WriteLine($"  [rejected] {Tag} roster is full - cannot add {player.GamerTag}");
            return;
        }

        _players.Add(player);
    }

    // How many players are on the roster right now.
    public int PlayerCount
    {
        get { return _players.Count; }
    }

    // Add up every player's rating and divide by how many there are.
    public double AverageRating
    {
        get
        {
            if (_players.Count == 0)
            {
                return 0;
            }

            int total = 0;

            // foreach walks every item in the list, one at a time.
            // `player` is the current item on each pass.
            foreach (Player player in _players)
            {
                total = total + player.Rating;
            }

            return (double)total / _players.Count;
        }
    }
}
