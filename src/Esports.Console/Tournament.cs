// A tournament holds teams and fixtures. It does NOT know how fixtures are
// worked out - it hands that job to whatever format it was handed.

public class Tournament
{
    // ---- DATA ----

    public string Name { get; private set; }

    // "Some format." Not RoundRobinFormat, not SingleEliminationFormat.
    // The variable's declared type is the CONTRACT; the object it points at
    // is one of the classes that fills the contract.
    //
    // `readonly` = can only be assigned in the constructor. The format a
    // tournament runs under should not change halfway through.
    private readonly ITournamentFormat _format;

    private List<Team> _teams = new List<Team>();
    private List<Match> _matches = new List<Match>();

    // The format is handed in from outside. Tournament does not choose it,
    // does not build it, and never finds out which one it got.
    public Tournament(string name, ITournamentFormat format)
    {
        Name = name;
        _format = format;
    }

    // Reading the format's name is fine - that is part of the contract.
    // Asking "are you a RoundRobinFormat?" would not be.
    public string FormatName => _format.Name;

    // ---- SIGNING UP ----

    public void Register(Team team)
    {
        // Once fixtures exist, a new team would have no matches.
        if (_matches.Count > 0)
        {
            throw new InvalidOperationException(
                $"{Name} has already started - cannot register {team.Tag}");
        }

        // The SAME object twice would end up playing itself. Two different
        // Team objects that happen to share a tag are different teams, so
        // ReferenceEquals is the right check, not ==.
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

    // ---- FIXTURES ----

    // THE WHOLE POINT OF TODAY IS THIS METHOD.
    //
    // There is no `if`. No mention of round robin or knockout. It asks the
    // format to do the work and stores the answer. Adding a third format
    // means writing one new class and changing NOTHING in this file.
    public void GenerateMatches()
    {
        if (_matches.Count > 0)
        {
            throw new InvalidOperationException($"{Name} fixtures already generated");
        }

        _matches = _format.GenerateMatches(_teams);
    }

    // ---- READING ----

    // IReadOnlyList<T> is a VIEW of the same list, so no copy is made and it
    // costs nothing. There is no Add on it. It stops accidents, not
    // sabotage - a caller can cast it back to List<T>.
    public IReadOnlyList<Match> Matches => _matches;

    public IReadOnlyList<Team> Teams => _teams;
}
