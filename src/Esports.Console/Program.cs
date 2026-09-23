// ---- four teams, in seed order (strongest first) ----

Team t1 = MakeTeam("T1", "T1", 1847, 1791, 1823);
Team gen = MakeTeam("Gen.G", "GEN", 1792, 1760, 1744);
Team hle = MakeTeam("Hanwha", "HLE", 1755, 1730, 1718);
Team dk = MakeTeam("Dplus", "DK", 1740, 1712, 1699);

Team[] teams = [t1, gen, hle, dk];


// ---- a league: everyone plays everyone ----

Tournament league = new Tournament("LCK Spring", new RoundRobinFormat());

foreach (Team team in teams)
{
    league.Register(team);
}

league.GenerateMatches();

Console.WriteLine($"=== {league.Name} ({league.FormatName}) ===");
Console.WriteLine($"{league.TeamCount} teams, {league.MatchCount} matches");


// ---- play every match ----

Random rng = new Random(42);   // fixed seed, so every run is identical

foreach (Match m in league.Matches)
{
    m.Start();
    m.RecordResult(rng.Next(0, 4), rng.Next(0, 4));
}

Console.WriteLine();
Console.WriteLine("--- results ---");

foreach (Match m in league.Matches)
{
    Console.WriteLine($"  {m}");
}


// ---- the table ----

Console.WriteLine();
Console.WriteLine("--- standings ---");

List<TeamStanding> table = StandingsTable.Build(league.Teams, league.Matches);
StandingsTable.Print(table);

Console.WriteLine();
Console.WriteLine($"Champion: {table[0].Team.Name} on {table[0].Points} points");


// ---- helpers ----

static Team MakeTeam(string name, string tag, params int[] ratings)
{
    Team team = new Team(name, tag);

    for (int i = 0; i < ratings.Length; i++)
    {
        team.AddPlayer(new Player($"{tag}-p{i + 1}", ratings[i]));
    }

    return team;
}
