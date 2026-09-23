// A tournament holds teams, then works out who plays who.

public class Tournament
{
    // ---- DATA ----

    public string Name { get; private set; }

    // Same reasoning as Team._players: `private`, not `private set`.
    // Handing out the real List<T> would let any caller Add() straight past
    // the rules below.
    private List<Team> _teams = new List<Team>();
    private List<Match> _matches = new List<Match>();

    public Tournament(string name)
    {
        Name = name;
    }

    // ---- SIGNING UP ----

    public void Register(Team team)
    {
        // Once the fixtures exist, adding a team would leave it with no
        // matches - a team in the tournament that never plays. Refuse.
        if (_matches.Count > 0)
        {
            throw new InvalidOperationException(
                $"{Name} has already started - cannot register {team.Tag}");
        }

        // A team registering twice would play itself. ReferenceEquals is
        // doing the work here: two Team objects with the same tag are
        // different teams, but the SAME object registered twice is not.
        foreach (Team existing in _teams)
        {
            if (ReferenceEquals(existing, team))
            {
                throw new ArgumentException($"{team.Tag} is already registered");
            }
        }

        _teams.Add(team);
    }

    public int TeamCount => _teams.Count;

    public int MatchCount => _matches.Count;

    // ---- WORKING OUT WHO PLAYS WHO ----

    // Every team plays every other team once.
    //
    // The loop shape is the whole trick. `j` starts at `i + 1`, never 0:
    //
    //        j=0  j=1  j=2  j=3
    //   i=0    -   X    X    X       X = a match
    //   i=1    -   -    X    X       - = skipped
    //   i=2    -   -    -    X
    //   i=3    -   -    -    -
    //
    // Starting j at 0 would give both "A plays B" and "B plays A", plus
    // "A plays A" down the diagonal. Starting at i+1 takes each pair once.
    public void GenerateMatches()
    {
        if (_matches.Count > 0)
        {
            throw new InvalidOperationException($"{Name} fixtures already generated");
        }

        if (_teams.Count < 2)
        {
            throw new InvalidOperationException(
                $"{Name} needs at least 2 teams, has {_teams.Count}");
        }

        for (int i = 0; i < _teams.Count; i++)
        {
            for (int j = i + 1; j < _teams.Count; j++)
            {
                _matches.Add(new Match(_teams[i], _teams[j]));
            }
        }
    }

    // ---- READING THE FIXTURES ----

    // Callers need to see the matches. Returning the real List<Match> would
    // hand out the arrow to it, so anyone could .Add() a fixture that the
    // rules above never approved.
    //
    // IReadOnlyList<T> is a VIEW of the same list - no copy is made, so it
    // costs nothing. It is a strong hint, not a hard guarantee: a determined
    // caller can cast it back. It stops accidents, not sabotage.
    public IReadOnlyList<Match> Matches => _matches;

    public IReadOnlyList<Team> Teams => _teams;
}
