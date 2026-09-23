// ---- four teams, in seed order (strongest first) ----

Team t1 = MakeTeam("T1", "T1", 1847, 1791, 1823);
Team gen = MakeTeam("Gen.G", "GEN", 1792, 1760, 1744);
Team hle = MakeTeam("Hanwha", "HLE", 1755, 1730, 1718);
Team dk = MakeTeam("Dplus", "DK", 1740, 1712, 1699);

Team[] teams = [t1, gen, hle, dk];


// ---- the same four teams, run two different ways ----

// The ONLY difference between these two lines is the object handed in.
Tournament league = new Tournament("LCK Spring", new RoundRobinFormat());
Tournament cup = new Tournament("LCK Cup", new SingleEliminationFormat());

RunTournament(league, teams);
RunTournament(cup, teams);


// ---- one loop, two formats ----
//
// `ITournamentFormat[]` holds objects of two different classes. The loop
// does not know or care which is which - it only uses what the contract
// promises. Adding a third format means adding it to this array.

Console.WriteLine();
Console.WriteLine("--- every format, same teams ---");

ITournamentFormat[] formats = [new RoundRobinFormat(), new SingleEliminationFormat()];

foreach (ITournamentFormat format in formats)
{
    List<Match> fixtures = format.GenerateMatches(teams);
    Console.WriteLine($"  {format.Name,-20} {fixtures.Count} matches");
}


// ---- what each format refuses ----

Console.WriteLine();
Console.WriteLine("--- limits ---");

Team[] three = [t1, gen, hle];

TryThis("knockout with 3 teams", () => new SingleEliminationFormat().GenerateMatches(three));
TryThis("round robin with 3 teams", () => new RoundRobinFormat().GenerateMatches(three));


// ---- helpers ----

static void RunTournament(Tournament tournament, Team[] teams)
{
    foreach (Team team in teams)
    {
        tournament.Register(team);
    }

    tournament.GenerateMatches();

    Console.WriteLine();
    Console.WriteLine($"=== {tournament.Name} ({tournament.FormatName}) ===");
    Console.WriteLine($"{tournament.TeamCount} teams, {tournament.MatchCount} matches");

    foreach (Match m in tournament.Matches)
    {
        Console.WriteLine($"  {m}");
    }
}

static Team MakeTeam(string name, string tag, params int[] ratings)
{
    Team team = new Team(name, tag);

    for (int i = 0; i < ratings.Length; i++)
    {
        team.AddPlayer(new Player($"{tag}-p{i + 1}", ratings[i]));
    }

    return team;
}

static void TryThis(string what, Action attempt)
{
    try
    {
        attempt();
        Console.WriteLine($"  allowed : {what}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  refused : {what} -> {ex.Message}");
    }
}
