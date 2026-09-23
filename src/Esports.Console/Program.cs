// ---- build four teams ----

Team t1 = MakeTeam("T1", "T1", 1847, 1791, 1823);
Team gen = MakeTeam("Gen.G", "GEN", 1792, 1760, 1744);
Team hle = MakeTeam("Hanwha", "HLE", 1755, 1730, 1718);
Team dk = MakeTeam("Dplus", "DK", 1740, 1712, 1699);


// ---- register them ----

Tournament lck = new Tournament("LCK Spring");

lck.Register(t1);
lck.Register(gen);
lck.Register(hle);
lck.Register(dk);

Console.WriteLine($"{lck.TeamCount} teams registered");


// ---- work out who plays who ----

lck.GenerateMatches();

Console.WriteLine($"{lck.MatchCount} matches generated");
Console.WriteLine();

foreach (Match m in lck.Matches)
{
    Console.WriteLine($"  {m}");
}


// ---- play them all ----

Console.WriteLine();
Console.WriteLine("--- playing every match ---");

Random rng = new Random(42);   // fixed seed, so every run is the same

foreach (Match m in lck.Matches)
{
    m.Start();
    m.RecordResult(rng.Next(0, 4), rng.Next(0, 4));
    Console.WriteLine($"  {m}");
}


// ---- the rules hold ----

Console.WriteLine();
Console.WriteLine("--- what the tournament refuses ---");

TryThis("register a team after fixtures exist", () => lck.Register(t1));
TryThis("generate fixtures twice", () => lck.GenerateMatches());
TryThis("register the same team twice", () =>
{
    Tournament t = new Tournament("Test");
    t.Register(t1);
    t.Register(t1);
});
TryThis("generate fixtures with one team", () =>
{
    Tournament t = new Tournament("Test");
    t.Register(t1);
    t.GenerateMatches();
});


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
