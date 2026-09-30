// ============================================================================
//  WHAT THIS PRINTS
//
//  Copied verbatim from a real run, so you can see the shape of the output
//  before reading the code that makes it. The Random is seeded with a fixed
//  number, so this is the same every single run.
//
//    === LCK Spring (Round robin) ===
//    4 teams, 6 matches
//
//    --- results ---
//      T1 vs GEN [Completed] 2-0
//      T1 vs HLE [Completed] 0-2
//      T1 vs DK [Completed] 0-1
//      GEN vs HLE [Completed] 2-2
//      GEN vs DK [Completed] 0-3
//      HLE vs DK [Completed] 0-1
//
//    --- standings ---
//      #  Team   P   W   D   L   Games    GD  Pts
//      ------------------------------------------
//      1  DK     3   3   0   0    5-0      5    9
//      2  HLE    3   1   1   1    4-3      1    4
//      3  T1     3   1   0   2    2-3     -1    3
//      4  GEN    3   0   1   2    2-7     -5    1
//
//    Champion: Dplus on 9 points
//
//  HOW TO READ THE TABLE
//    P     played          W/D/L  won / drawn / lost
//    Games games won-lost  GD     game difference (won minus lost)
//    Pts   3 per win, 1 per draw
//
//  WHY DK IS TOP: three wins out of three. 3 x 3 = 9 points.
//  WHY HLE IS SECOND: 1 win + 1 draw = 4 points, ahead of T1's single win.
// ============================================================================


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

// A fixed seed means the same "random" numbers every run. Without it the
// output changes each time and you cannot tell a real change from noise.
Random rng = new Random(42);

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
//
// Nothing here counts anything. StandingsTable works it out from the
// matches, so the table can never disagree with the results.

Console.WriteLine();
Console.WriteLine("--- standings ---");

List<TeamStanding> table = StandingsTable.Build(league.Teams, league.Matches);
StandingsTable.Print(table);

// table is sorted, so index 0 is the winner.
Console.WriteLine();
Console.WriteLine($"Champion: {table[0].Team.Name} on {table[0].Points} points");


// ---- helpers ----

// `params int[] ratings` lets the caller pass any number of ratings:
//   MakeTeam("T1", "T1", 1847, 1791, 1823)
static Team MakeTeam(string name, string tag, params int[] ratings)
{
    Team team = new Team(name, tag);

    for (int i = 0; i < ratings.Length; i++)
    {
        team.AddPlayer(new Player($"{tag}-p{i + 1}", ratings[i]));
    }

    return team;
}
